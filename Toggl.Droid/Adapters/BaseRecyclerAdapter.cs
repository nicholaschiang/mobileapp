using Android.OS;
using Android.Runtime;
using Android.Support.V7.Util;
using Android.Support.V7.Widget;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.UI.Interfaces;
using Toggl.Droid.Adapters.DiffingStrategies;
using Toggl.Droid.Adapters.ListAdapter;
using Toggl.Droid.ViewHolders;
using Toggl.Shared.Extensions;
using Handler = Android.OS.Handler;

namespace Toggl.Droid.Adapters
{
    public abstract class BaseRecyclerAdapter<T> : BaseListAdapter<T>
        where T : IEquatable<T>
    {
        public IObservable<T> ItemTapObservable => itemTapSubject.AsObservable();

        private readonly Subject<T> itemTapSubject = new Subject<T>();

        public virtual IImmutableList<T> Items
        {
            get => GetItemList();
            set => SetItems(value ?? ImmutableList<T>.Empty);
        }

        protected BaseRecyclerAdapter(IDiffingStrategy<T> diffingStrategy = null) : base(diffingStrategy) {}

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var inflater = LayoutInflater.From(parent.Context);
            var viewHolder = CreateViewHolder(parent, inflater, viewType);
            viewHolder.TappedSubject = itemTapSubject;
            return viewHolder;
        }

        protected abstract BaseRecyclerViewHolder<T> CreateViewHolder(ViewGroup parent, LayoutInflater inflater,
            int viewType);

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = GetItem(position);
            ((BaseRecyclerViewHolder<T>)holder).Item = item;
        }

        public override int ItemCount => GetItemCount();

        public override long GetItemId(int position)
        {
            return GetItemId(position);
        }

        public virtual T GetItem(int viewPosition)
            => GetItem(viewPosition);

        protected virtual void SetItems(IImmutableList<T> newItems)
        {
            SubmitList(newItems);
        }
    }
}
