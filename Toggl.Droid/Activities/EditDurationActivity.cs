using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.ViewHelpers;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using static Toggl.Core.UI.Helper.TemporalInconsistency;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
        WindowSoftInputMode = SoftInput.StateHidden | SoftInput.AdjustResize,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditDurationActivity : ReactiveActivity<EditDurationViewModel>
    {
        private readonly Dictionary<TemporalInconsistency, int> inconsistencyMessages = new Dictionary<TemporalInconsistency, int>
        {
            [StartTimeAfterCurrentTime] = Resource.String.StartTimeAfterCurrentTimeWarning,
            [StartTimeAfterStopTime] = Resource.String.StartTimeAfterStopTimeWarning,
            [StopTimeBeforeStartTime] = Resource.String.StopTimeBeforeStartTimeWarning,
            [DurationTooLong] = Resource.String.DurationTooLong,
        };

        private readonly Subject<DateTimeOffset> activeEditionChangedSubject = new Subject<DateTimeOffset>();
        private readonly Subject<Unit> viewClosedSubject = new Subject<Unit>();
        //private readonly Subject<Unit> saveSubject = new Subject<Unit>();

        private DateTimeOffset minDateTime;
        private DateTimeOffset maxDateTime;
        private EditMode editMode = EditMode.None;
        private bool canDismiss = true;
        private bool is24HoursFormat;

        private Dialog editDialog;
        private Toast toast;

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.EditDurationActivity);
            InitializeViews();
            setupToolbar();

            ViewModel.TimeFormat
                .Subscribe(v => is24HoursFormat = v.IsTwentyFourHoursFormat)
                .DisposedBy(DisposeBag);

            ViewModel.StartTimeString
                .Subscribe(startTimeText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.StartDateString
                .Subscribe(startDateText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.StopTimeString
                .Subscribe(stopTimeText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.StopDateString
                .Subscribe(stopDateText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsRunning
                .Subscribe(updateStopTimeUIVisibility)
                .DisposedBy(DisposeBag);

            ViewModel.MinimumDateTime
                .Subscribe(min => minDateTime = min)
                .DisposedBy(DisposeBag);

            ViewModel.MaximumDateTime
                .Subscribe(max => maxDateTime = max)
                .DisposedBy(DisposeBag);

            stopTimerLabel.Rx()
                .BindAction(ViewModel.StopTimeEntry)
                .DisposedBy(DisposeBag);

            startTimeText.Rx().Tap()
                .Subscribe(_ => { editMode = EditMode.Time; })
                .DisposedBy(DisposeBag);

            startDateText.Rx().Tap()
                .Subscribe(_ => { editMode = EditMode.Date; })
                .DisposedBy(DisposeBag);

            startTimeText.Rx()
                .BindAction(ViewModel.EditStartTime)
                .DisposedBy(DisposeBag);

            startDateText.Rx()
                .BindAction(ViewModel.EditStartTime)
                .DisposedBy(DisposeBag);

            stopTimeText.Rx().Tap()
                .Subscribe(_ => { editMode = EditMode.Time; })
                .DisposedBy(DisposeBag);

            stopDateText.Rx().Tap()
                .Subscribe(_ => { editMode = EditMode.Date; })
                .DisposedBy(DisposeBag);

            stopTimeText.Rx()
                .BindAction(ViewModel.EditStopTime)
                .DisposedBy(DisposeBag);

            stopDateText.Rx()
                .BindAction(ViewModel.EditStopTime)
                .DisposedBy(DisposeBag);

            ViewModel.TemporalInconsistencies
                .Subscribe(onTemporalInconsistency)
                .DisposedBy(DisposeBag);

            ViewModel.IsEditingStartTime
                .Where(CommonFunctions.Identity)
                .SelectMany(_ => ViewModel.StartTime)
                .Subscribe(startEditing)
                .DisposedBy(DisposeBag);

            ViewModel.IsEditingStopTime
                .Where(CommonFunctions.Identity)
                .SelectMany(_ => ViewModel.StopTime)
                .Subscribe(startEditing)
                .DisposedBy(DisposeBag);

            activeEditionChangedSubject
                .Subscribe(ViewModel.ChangeActiveTime.Inputs)
                .DisposedBy(DisposeBag);

            viewClosedSubject
                .Subscribe(ViewModel.StopEditingTime.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsEditingTime
                .Invert()
                .Subscribe(wheelNumericInput.Rx().Enabled())
                .DisposedBy(DisposeBag);

            ViewModel.Duration
                .Where(_ => !wheelNumericInput.HasFocus)
                .Subscribe(wheelNumericInput.SetDuration)
                .DisposedBy(DisposeBag);

            wheelNumericInput.Duration
                .Subscribe(ViewModel.ChangeDuration.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.MinimumStartTime
                .Subscribe(v => wheelForeground.MinimumStartTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.MaximumStartTime
                .Subscribe(v => wheelForeground.MaximumStartTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.MinimumStopTime
                .Subscribe(v => wheelForeground.MinimumEndTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.MaximumStopTime
                .Subscribe(v => wheelForeground.MaximumEndTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .DistinctUntilChanged()
                .Subscribe(v => wheelForeground.StartTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.StopTime
                .Select(endTime => endTime.RoundDownToMinute())
                .DistinctUntilChanged()
                .Subscribe(v => wheelForeground.EndTime = v)
                .DisposedBy(DisposeBag);

            ViewModel.IsRunning
                .Subscribe(v => wheelForeground.IsRunning = v)
                .DisposedBy(DisposeBag);

            wheelForeground.StartTimeObservable
                .Subscribe(ViewModel.ChangeStartTime.Inputs)
                .DisposedBy(DisposeBag);

            wheelForeground.EndTimeObservable
                .Subscribe(ViewModel.ChangeStopTime.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.GenericSaveMenu, menu);
            var item = menu.FindItem(Resource.Id.SaveMenuItem);
            item.ActionView.Rx().Tap()
                .Subscribe(_ =>
                {
                    wheelNumericInput.ApplyDurationIfBeingEdited();
                    ViewModel.Save.Execute();
                })
                .DisposedBy(DisposeBag);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {

                case Android.Resource.Id.Home:
                    ViewModel.CloseWithDefaultResult();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void setupToolbar()
        {
            toolbar.Title = Shared.Resources.StartAndStopTime;

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
        }

        private void updateStopTimeUIVisibility(bool isRunning)
        {
            var stopDateTimeViewsVisibility = (!isRunning).ToVisibility();
            stopTimerLabel.Visibility = isRunning.ToVisibility();
            stopTimeText.Visibility = stopDateTimeViewsVisibility;
            stopDateText.Visibility = stopDateTimeViewsVisibility;
            stopDotSeparator.Visibility = stopDateTimeViewsVisibility;
        }

        protected override void OnStop()
        {
            base.OnStop();
            canDismiss = true;
            editDialog?.Dismiss();
        }

        private void onTemporalInconsistency(TemporalInconsistency temporalInconsistency)
        {
            canDismiss = false;
            toast?.Cancel();
            toast = null;

            var messageResourceId = inconsistencyMessages[temporalInconsistency];
            var message = Resources.GetString(messageResourceId);

            toast = Toast.MakeText(this, message, ToastLength.Short);
            toast.Show();
        }

        private void startEditing(DateTimeOffset initialValue)
        {
            if (editMode == EditMode.None)
                return;

            var localInitialValue = initialValue.ToLocalTime();

            if (editMode == EditMode.Time)
            {
                editTime(localInitialValue);
            }
            else
            {
                editDate(localInitialValue);
            }
        }

        private void editTime(DateTimeOffset currentTime)
        {
            if (editDialog == null)
            {
                var timePickerDialog = new TimePickerDialog(this, Resource.Style.WheelDialogStyle, new TimePickerListener(currentTime, activeEditionChangedSubject.OnNext),
                    currentTime.Hour, currentTime.Minute, is24HoursFormat);

                void resetAction()
                {
                    timePickerDialog.UpdateTime(currentTime.Hour, currentTime.Minute);
                }

                editDialog = timePickerDialog;
                editDialog.DismissEvent += (_, __) => onCurrentEditDialogDismiss(resetAction);
                editDialog.Show();
            }
        }
        private void editDate(DateTimeOffset currentDate)
        {
            if (editDialog == null)
            {
                var datePickerDialog = new DatePickerDialog(this, Resource.Style.WheelDialogStyle, new DatePickerListener(currentDate, activeEditionChangedSubject.OnNext),
                    currentDate.Year, currentDate.Month - 1, currentDate.Day);

                // FirstDayOfWeek days start with sunday at 1 and finish with saturday at 7
                var normalizedBeginningOfWeek = (int)ViewModel.BeginningOfWeek + 1;
                datePickerDialog.DatePicker.FirstDayOfWeek = normalizedBeginningOfWeek;

                void updateDateBounds()
                {
                    datePickerDialog.DatePicker.MinDate = minDateTime.ToUnixTimeMilliseconds();
                    datePickerDialog.DatePicker.MaxDate = maxDateTime.ToUnixTimeMilliseconds();
                }

                updateDateBounds();

                void resetAction()
                {
                    updateDateBounds();
                    datePickerDialog.UpdateDate(currentDate.Year, currentDate.Month - 1, currentDate.Day);
                }

                editDialog = datePickerDialog;
                editDialog.DismissEvent += (_, __) => onCurrentEditDialogDismiss(resetAction);
                editDialog.Show();
            }
        }

        private void onCurrentEditDialogDismiss(Action resetAction)
        {
            if (canDismiss)
            {
                editDialog = null;
                viewClosedSubject.OnNext(Unit.Default);
                editMode = EditMode.None;
            }
            else
            {
                resetAction();
                editDialog.Show();
                canDismiss = true;
            }
        }

        private enum EditMode
        {
            Time,
            Date,
            None
        }
    }
}
