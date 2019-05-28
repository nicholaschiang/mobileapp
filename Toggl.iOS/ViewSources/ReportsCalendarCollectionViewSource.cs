using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.ReportsCalendar;
using Toggl.iOS.Views;
using UIKit;

namespace Toggl.iOS.ViewSources
{
    public sealed class ReportsCalendarCollectionViewSource : UICollectionViewSource
    {
        private const string cellIdentifier = nameof(ReportsCalendarViewCell);

        private readonly UICollectionView collectionView;
        private readonly ISubject<int> currentPageNotScrollingSubject = new Subject<int>();
        private readonly ISubject<int> currentPageWhileScrollingSubject = new Subject<int>();
        private readonly ISubject<ReportsCalendarDayViewModel> dayTaps = new Subject<ReportsCalendarDayViewModel>();

        private ReportsDateRange currentSelectedDateRange;
        private List<ReportsCalendarPageViewModel> months = new List<ReportsCalendarPageViewModel>();

        public IObservable<ReportsCalendarDayViewModel> DayTaps { get; }
        public IObservable<int> CurrentPageNotScrollingObservable { get; }
        public IObservable<int> CurrentPageWhileScrollingObservable { get; }

        public ReportsCalendarCollectionViewSource(UICollectionView collectionView)
        {
            this.collectionView = collectionView;

            collectionView.RegisterNibForCell(ReportsCalendarViewCell.Nib, cellIdentifier);

            DayTaps = dayTaps.AsObservable();
            CurrentPageWhileScrollingObservable = currentPageWhileScrollingSubject.AsObservable().DistinctUntilChanged();
            CurrentPageNotScrollingObservable = currentPageNotScrollingSubject.AsObservable().DistinctUntilChanged();
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = months[indexPath.Section].Days[indexPath.Row];
            var cell = collectionView.DequeueReusableCell(cellIdentifier, indexPath) as ReportsCalendarViewCell;

            cell.UpdateDateRange(currentSelectedDateRange);
            cell.Item = months[indexPath.Section].Days[indexPath.Row];
            return cell;
        }

        public void UpdateMonths(List<ReportsCalendarPageViewModel> newMonths)
        {
            months = newMonths;
            if (currentSelectedDateRange != null)
            {
                UpdateSelection(currentSelectedDateRange);
            }
            else
            {
                collectionView.ReloadData();
            }
        }

        public void UpdateSelection(ReportsDateRange selectedDateRange)
        {
            if (selectedDateRange == null) return;

            currentSelectedDateRange = selectedDateRange;
            collectionView.CollectionViewLayout.InvalidateLayout();
            collectionView.ReloadData();
        }

        public void RefreshUIAtPage(int page)
        {
            currentPageNotScrollingSubject.OnNext(page);
            UpdateSelection(currentSelectedDateRange);
        }

        public override nint NumberOfSections(UICollectionView collectionView)
            => months.Count;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => months[(int) section].Days.Count;

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            dayTaps.OnNext(months[indexPath.Section].Days[indexPath.Row]);
        }

        public override void DecelerationEnded(UIScrollView scrollView)
        {
            var page = (int) ((collectionView.ContentOffset.X + collectionView.Frame.Width/2) / collectionView.Frame.Width);
            currentPageNotScrollingSubject.OnNext(page);
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            var page = (int) ((collectionView.ContentOffset.X + collectionView.Frame.Width/2) / collectionView.Frame.Width);
            currentPageWhileScrollingSubject.OnNext(page);
        }
    }
}
