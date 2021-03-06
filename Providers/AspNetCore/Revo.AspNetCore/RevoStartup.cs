﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.ContextPreservation;
using Ninject.Extensions.Factory;
using Ninject.Infrastructure.Disposal;
using Revo.AspNetCore.Core;
using Revo.AspNetCore.Ninject;
using Revo.Core.Configuration;
using Revo.Core.Core;
using Revo.Core.Types;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Revo.AspNetCore
{
    public abstract class RevoStartup
    {
        private static readonly AsyncLocal<Scope> scopeProvider = new AsyncLocal<Scope>();
        public static object RequestScope(IContext context) => scopeProvider.Value;
        
        private KernelBootstrapper kernelBootstrapper;

        public RevoStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public IKernel Kernel { get; private set; }
        private object Resolve(Type type) => Kernel.Get(type);

        public virtual void ConfigureServices(IServiceCollection services)
        {
            CreateKernel();
            var revoConfiguration = CreateRevoConfiguration();

            /** NOTE: these assemblies containing these modules are usually not directly referenced
             * and thus would other not get loaded into the app domain */
            Type[] ninjectExtModules = new[] {typeof(FuncModule), typeof(ContextPreservationModule)};
            foreach (Type ninjectExtModule in ninjectExtModules)
            {
                if (!revoConfiguration
                    .GetSection<KernelConfigurationSection>()
                    .LoadedModuleOverrides.ContainsKey(ninjectExtModule))
                {
                    revoConfiguration.OverrideModuleLoading(ninjectExtModule, true);
                }
            }
            
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton(sp => Kernel);

            services.AddRequestScopingMiddleware(() => scopeProvider.Value = new Scope());
            services.AddCustomControllerActivation();
            services.AddCustomHubActivation();
            services.AddCustomViewComponentActivation();

            kernelBootstrapper = new KernelBootstrapper(Kernel, revoConfiguration);
            
            var typeExplorer = new TypeExplorer();

            kernelBootstrapper.Configure();

            var assemblies = typeExplorer
                .GetAllReferencedAssemblies()
                .Where(a => !a.GetName().Name.StartsWith("System."))
                .Where(a => !a.IsDynamic).ToList();

            kernelBootstrapper.LoadAssemblies(assemblies);
            
            var aspNetCoreConfigurers = Kernel.GetAll<IAspNetCoreStartupConfigurer>();
            foreach (var aspNetCoreConfigurer in aspNetCoreConfigurers)
            {
                aspNetCoreConfigurer.ConfigureServices(services);
            }
        }
        
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            RegisterAspNetCoreService(app, loggerFactory);
            kernelBootstrapper.RunAppConfigurers();

            var aspNetCoreConfigurers = Kernel.GetAll<IAspNetCoreStartupConfigurer>();
            foreach (var aspNetCoreConfigurer in aspNetCoreConfigurers)
            {
                aspNetCoreConfigurer.Configure(app, env, loggerFactory);
            }
            
            kernelBootstrapper.RunAppStartListeners();
        }

        protected abstract IRevoConfiguration CreateRevoConfiguration();

        private void CreateKernel()
        {
            Kernel = new StandardKernel();

            Kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => Kernel);
            Kernel.Bind<StandardKernel>().ToMethod(ctx => Kernel as StandardKernel);

            //Hangfire.GlobalConfiguration.Configuration.UseNLogLogProvider();
            //Hangfire.GlobalConfiguration.Configuration.UseNinjectActivator();

            NinjectBindingExtensions.Current = new AspNetCoreNinjectBindingExtension();
        }

        private void RegisterAspNetCoreService(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            foreach (var ctrlType in app.GetControllerTypes())
            {
                Kernel.Bind(ctrlType).ToSelf().InScope(RequestScope);
            }

            foreach (var hubType in app.GetHubTypes())
            {
                Kernel.Bind(hubType).ToSelf().InScope(RequestScope);
            }

            Kernel.Bind<IViewBufferScope>().ToMethod(ctx => app.GetRequestService<IViewBufferScope>());
            Kernel.Bind(typeof(IHubContext<>), typeof(IHubContext<,>)).ToMethod(ctx => app.GetRequestService(ctx.Request.Service));
            Kernel.Bind<ILoggerFactory>().ToConstant(loggerFactory);
            Kernel.Bind<IServiceProvider>().ToMethod(ctx => app.ApplicationServices);
            Kernel.Bind<IWebHostEnvironment>().ToMethod(ctx => app.ApplicationServices.GetRequiredService<IWebHostEnvironment>());
            Kernel.Bind<IHttpContextAccessor>()
                .ToMethod(ctx => app.ApplicationServices.GetRequiredService<IHttpContextAccessor>())
                .InTransientScope();
            Kernel.Bind<HttpContext>()
                .ToMethod(ctx => app.ApplicationServices.GetRequiredService<IHttpContextAccessor>().HttpContext)
                .InTransientScope();

            Kernel.Bind<IConfiguration>().ToConstant(Configuration);
        }
        
        private sealed class Scope : DisposableObject { }
    }
}
