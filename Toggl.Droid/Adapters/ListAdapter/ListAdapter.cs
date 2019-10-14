using System;
using System.Collections.Immutable;
using Android.Support.V7.RecyclerView.Extensions;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Toggl.Droid.Adapters.DiffingStrategies;

namespace Toggl.Droid.Adapters.ListAdapter
{
    public abstract class ListAdapter<T> : RecyclerView.Adapter
        where T : IEquatable<T>
    {
        private AsyncListDiffer<T> listDiffer;

        public ListAdapter(IDiffingStrategy<T> diffingStrategy, AsyncDifferConfig config)
        {
            listDiffer = new AsyncListDiffer<T>(new AdapterListUpdateCallback(this), diffingStrategy, new AsyncDifferConfig.Builder(diffCallback).build());
        }

        public void SubmitList(IImmutableList<T> newItems)
        {
            listDiffer.SubmitList(newItems);
        }

        public T getItem(int position)
        {
            return listDiffer.GetCurrentList()[position];
        }

        public int getItemCount()
        {
            return listDiffer.GetCurrentList().Count;
        }
    }
}
