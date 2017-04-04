﻿using System;
using System.Collections.Generic;

namespace GTRevo.Infrastructure.Domain
{
    public abstract class AggregateRoot : IAggregateRoot
    {
        public AggregateRoot(Guid id, Guid classId) : this()
        {
            Id = id;
            ClassId = classId;
            Version = 0;
        }

        /// <summary>
        /// Only to be used by by ORMs like EF and the second chained constructor.
        /// </summary>
        protected AggregateRoot()
        {
            EventRouter = new AggregateEventRouter(this);
        }

        public virtual Guid Id { get; private set; }
        public virtual Guid ClassId { get; private set; }
        public virtual int Version { get; protected set; }

        public virtual IEnumerable<DomainAggregateEvent> UncommitedEvents
        {
            get
            {
                return EventRouter.UncommitedEvents;
            }
        }

        protected IAggregateEventRouter EventRouter { get; }

        public void Commit()
        {
            Version++;
            EventRouter.CommitEvents();
        }

        protected virtual void ApplyEvent<T>(T evt) where T : DomainAggregateEvent
        {
        }
    }
}
