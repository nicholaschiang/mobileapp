using System;
using System.Collections.Immutable;
using Android.OS;
using Android.Support.V7.Util;
using System.Threading.Tasks;
using Toggl.Droid.Adapters.DiffingStrategies;

namespace Toggl.Droid.Adapters.ListAdapter
{
    public class AsyncListDiffer<T>
        where T : IEquatable<T>
    {
        private readonly IListUpdateCallback updateCallback;
        private readonly IDiffingStrategy<T> diffingStrategy;


        private IImmutableList<T> list;
        private IImmutableList<T> readonlyList = ImmutableList.Create<T>();
        private int maxScheduledGeneration;

        public AsyncListDiffer(IListUpdateCallback updateCallback, IDiffingStrategy<T> diffingStrategy)
        {
            this.updateCallback = updateCallback;
            this.diffingStrategy = diffingStrategy;
        }

        public IImmutableList<T> GetCurrentList()
        {
            return readonlyList;
        }

        public void SubmitList(IImmutableList<T> newList)
        {
            // incrementing generation means any currently-running diffs are discarded when they finish
            var runGeneration = ++maxScheduledGeneration;

            if (newList == list)
            {
                // nothing to do (Note - still had to inc generation, since may have ongoing work)
                return;
            }

            // fast simple remove all
            if (newList == null)
            {
                //noinspection ConstantConditions
                var countRemoved = list.Count;
                list = null;
                readonlyList = ImmutableList.Create<T>();
                // notify last, after list is updated

                updateCallback.OnRemoved(0, countRemoved);
                return;
            }

            if (list == null)
            {
                list = newList;
                readonlyList = newList;
                // notify last, after list is updated
                updateCallback.OnInserted(0, newList.Count);
                return;
            }

            var oldList = list;

            var handler = new Handler(Looper.MainLooper);
            Task.Run(() =>
            {
                var diffResult = DiffUtil.CalculateDiff(new BaseDiffCallBack(oldList, newList, diffingStrategy));
                handler.Post(() =>
                {
                    if (maxScheduledGeneration == runGeneration)
                    {
                        LatchList(newList, diffResult);
                    }
                });
            });
        }

        public void LatchList(IImmutableList<T> newList, DiffUtil.DiffResult diffResult)
        {
            list = newList;
            readonlyList = newList;
            diffResult.DispatchUpdatesTo(updateCallback);
        }

        public long GetItemId(T item)
        {
            return diffingStrategy.GetItemId(item);
        }

        private sealed class BaseDiffCallBack : DiffUtil.Callback
        {
            private IImmutableList<T> oldItems;
            private IImmutableList<T> newItems;
            private IDiffingStrategy<T> diffingStrategy;

            public BaseDiffCallBack(IImmutableList<T> oldItems, IImmutableList<T> newItems, IDiffingStrategy<T> diffingStrategy)
            {
                this.oldItems = oldItems;
                this.newItems = newItems;
                this.diffingStrategy = diffingStrategy;
            }

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                var oldItem = oldItems[oldItemPosition];
                var newItem = newItems[newItemPosition];

                return diffingStrategy.AreContentsTheSame(oldItem, newItem);
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                var oldItem = oldItems[oldItemPosition];
                var newItem = newItems[newItemPosition];

                return diffingStrategy.AreItemsTheSame(oldItem, newItem);
            }

            public override int NewListSize => newItems.Count;
            public override int OldListSize => oldItems.Count;
        }
    }
}
