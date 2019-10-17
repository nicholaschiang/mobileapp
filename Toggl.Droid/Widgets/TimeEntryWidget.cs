using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using Toggl.Droid.Extensions;
using Toggl.Droid.Widgets.Services;
using Toggl.Shared;
using static Toggl.Droid.Services.TimerBackgroundService;
using static Toggl.Droid.Widgets.WidgetsConstants;
using Color = Android.Graphics.Color;

namespace Toggl.Droid.Widgets
{
    [BroadcastReceiver(Label = "Toggl Time Entry Widget", Exported = true)]
    [IntentFilter(new string[] { AppWidgetManager.ActionAppwidgetUpdate })]
    [MetaData("android.appwidget.provider", Resource = "@xml/timeentrywidgetprovider")]
    public class TimeEntryWidget : AppWidgetProvider
    {
        public override void OnReceive(Context context, Intent intent)
        {
            base.OnReceive(context, intent);

            switch (intent.Action)
            {
                case StartTimeEntryAction:
                    EnqueueWork(context, intent);
                    break;

                case StopRunningTimeEntryAction:
                    EnqueueWork(context, intent);
                    break;
            }
        }

        public override void OnDeleted(Context context, int[] appWidgetIds)
        {
            reportInstallationState(context, false);
            base.OnDeleted(context, appWidgetIds);
        }

        public override void OnEnabled(Context context)
        {
            reportInstallationState(context, true);
            base.OnEnabled(context);
        }

        public override void OnUpdate(Context context, AppWidgetManager appWidgetManager, int[] appWidgetIds)
        {
            base.OnUpdate(context, appWidgetManager, appWidgetIds);

            foreach (var widgetId in appWidgetIds)
            {
                var options = appWidgetManager.GetAppWidgetOptions(widgetId);
                var dimensions = WidgetDimensions.FromBundle(options);
                updateWidget(context, appWidgetManager, widgetId, dimensions);
            }
        }

        public override void OnAppWidgetOptionsChanged(Context context, AppWidgetManager appWidgetManager, int appWidgetId, Bundle newOptions)
        {
            base.OnAppWidgetOptionsChanged(context, appWidgetManager, appWidgetId, newOptions);
            var dimensions = WidgetDimensions.FromBundle(newOptions);

            updateWidget(context, appWidgetManager, appWidgetId, dimensions);

            reportSize(context, dimensions.ColumnsCount);
        }

        private void updateWidget(Context context, AppWidgetManager appWidgetManager, int appWidgetId, WidgetDimensions dimensions = null)
        {
            var widgetInfo = TimeEntryWidgetInfo.FromSharedPreferences();
            var remoteViews = TimeEntryWidgetFactory.Create(dimensions).Setup(context, widgetInfo);
            appWidgetManager.UpdateAppWidget(appWidgetId, remoteViews);
        }

        private void reportInstallationState(Context context, bool installed)
        {
            var intent = new Intent(context, typeof(WidgetsAnalyticsService));
            intent.SetAction(WidgetsAnalyticsService.TimerWidgetInstallAction);
            intent.PutExtra(WidgetsAnalyticsService.TimerWidgetInstallStateParameter, installed);
            WidgetsAnalyticsService.EnqueueWork(context, intent);
        }

        private void reportSize(Context context, int columnCount)
        {
            var intent = new Intent(context, typeof(WidgetsAnalyticsService));
            intent.SetAction(WidgetsAnalyticsService.TimerWidgetResizeAction);
            intent.PutExtra(WidgetsAnalyticsService.TimerWidgetSizeParameter, columnCount);
            WidgetsAnalyticsService.EnqueueWork(context, intent);
        }
    }
}
