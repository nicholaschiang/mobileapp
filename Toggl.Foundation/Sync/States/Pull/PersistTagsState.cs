﻿using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Sync.States
{
    class PersistTagsState : BasePersistState<ITag, IDatabaseTag>
    {
        public PersistTagsState(ITogglDatabase database)
            : base(database)
        {
        }

        protected override IObservable<IEnumerable<ITag>> FetchObservable(FetchObservables fetch)
            => fetch.Tags;

        protected override IDatabaseTag ConvertToDatabaseEntity(ITag entity)
            => Tag.Clean(entity);

        protected override IObservable<IEnumerable<(ConflictResolutionMode, IDatabaseTag)>> BatchUpdate(ITogglDatabase database, IEnumerable<(long, IDatabaseTag)> entities)
            => database.Tags.BatchUpdate(entities, Resolver.ForTags().Resolve);

        protected override DateTimeOffset? LastUpdated(ISinceParameters old, IEnumerable<IDatabaseTag> entities)
            => entities.Select(p => p?.At).Where(d => d.HasValue).DefaultIfEmpty(old.Tags).Max();

        protected override ISinceParameters UpdateSinceParameters(ISinceParameters old, DateTimeOffset? lastUpdated)
            => new SinceParameters(old, tags: lastUpdated);
    }
}
