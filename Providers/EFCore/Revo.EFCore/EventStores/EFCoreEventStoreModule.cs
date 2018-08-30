﻿using Ninject.Modules;
using Revo.Core.Core;
using Revo.DataAccess.Entities;
using Revo.EFCore.DataAccess.Entities;
using Revo.Infrastructure.Events.Async;
using Revo.Infrastructure.EventStores;
using Revo.Infrastructure.EventStores.Generic;

namespace Revo.EFCore.EventStores
{
    [AutoLoadModule(false)]
    public class EFCoreEventStoreModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IEventStore>()
                .To<EFCoreEventStore>()
                .InRequestOrJobScope();

            Bind<ICrudRepository>()
                .To<EFCoreCrudRepository>()
                .WhenInjectedInto<EFCoreEventStore>()
                .InTransientScope();

            Bind<IEventSourceCatchUp>()
                .To<EventSourceCatchUp>()
                .InTransientScope();

            Bind<IReadRepository>()
                .To<EFCoreCrudRepository>()
                .WhenInjectedInto<EventSourceCatchUp>()
                .InTransientScope();
        }
    }
}
