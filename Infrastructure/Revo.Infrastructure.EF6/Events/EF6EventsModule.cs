﻿using Ninject.Modules;
using Revo.Core.Core;
using Revo.DataAccess.EF6.Entities;
using Revo.DataAccess.Entities;
using Revo.Infrastructure.EF6.Events.Async;
using Revo.Infrastructure.Events.Async;

namespace Revo.Infrastructure.EF6.Events
{
    public class EF6AsyncEventsModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IAsyncEventQueueManager>()
                .To<AsyncEventQueueManager>()
                .InRequestOrJobScope();

            Bind<ICrudRepository>()
                .To<EF6CrudRepository>()
                .WhenInjectedInto<AsyncEventQueueManager>()
                .InTransientScope();

            Bind<IEventSerializer>()
                .To<EventSerializer>()
                .InSingletonScope();
        }
    }
}