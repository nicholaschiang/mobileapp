using Android.App;
using Android.Appwidget;
using Android.Content;
using Toggl.Droid.Extensions;

namespace Toggl.Droid.Helper
{
    public static class AppWidgetProviderUtils
    {
        public static void UpdateAllInstances<TWidgetProvider>() where TWidgetProvider : AppWidgetProvider
        {
            var context = Application.Context;
            var widgetClass = JavaUtils.ToClass<TWidgetProvider>();

            var widgetIds = AppWidgetManager
                .GetInstance(context)
                .GetAppWidgetIds(new ComponentName(context, widgetClass));

            var intent = new Intent(Application.Context, widgetClass);
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, widgetIds);

            context.SendBroadcast(intent);
        }

        public static PendingIntent ToBroadcastIntent<TWidgetProvider>(this string action, Context context) where TWidgetProvider : AppWidgetProvider
        {
            var intent = new Intent(context, typeof(TWidgetProvider));
            intent.SetAction(action);
            return PendingIntent.GetBroadcast(context, 0, intent, PendingIntentFlags.UpdateCurrent);
        }
    }
}