using System;
using System.Collections.Immutable;
using Android.Support.V7.RecyclerView.Extensions;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Toggl.Core.UI.Interfaces;
using Toggl.Droid.Adapters.DiffingStrategies;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Adapters.ListAdapter
{
    public abstract class BaseListAdapter<T> : RecyclerView.Adapter
        where T : IEquatable<T>
    {
        private readonly AsyncListDiffer<T> listDiffer;

        public BaseListAdapter(IDiffingStrategy<T> diffingStrategy)
        {
            HasStableIds = diffingStrategy.HasStableIds;
            listDiffer = new AsyncListDiffer<T>(new AdapterListUpdateCallback(this), normalizeDiffingStrategy(diffingStrategy));
        }

        private IDiffingStrategy<T> normalizeDiffingStrategy(IDiffingStrategy<T> diffingStrategy)
        {
            if (diffingStrategy != null)
                return diffingStrategy;

            if (typeof(T).ImplementsOrDerivesFrom<IDiffableByIdentifier<T>>())
                return new IdentifierEqualityDiffingStrategy<T>();

            return new EquatableDiffingStrategy<T>();
        }

        public void SubmitList(IImmutableList<T> newItems)
        {
            listDiffer.SubmitList(newItems);
        }

        public IImmutableList<T> GetItemList()
        {
            return listDiffer.GetCurrentList();
        }

        public T GetItem(int position)
        {
            return listDiffer.GetCurrentList()[position];
        }

        public int GetItemCount()
        {
            return listDiffer.GetCurrentList().Count;
        }

        public override long GetItemId(int position)
        {
            return listDiffer.GetItemId(GetItem(position));
        }
    }
}
