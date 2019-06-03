using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using SiriExtension.Models;
using Toggl.iOS.ExtensionKit;
using Toggl.iOS.ExtensionKit.Analytics;
using Toggl.iOS.ExtensionKit.Extensions;
using Toggl.iOS.Intents;
using Toggl.Networking;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace SiriExtension
{
    public class StartTimerFromClipboardIntentHandler: StartTimerFromClipboardIntentHandling
    {
        private ITogglApi togglAPI;
        private static string clipboardText;
        private const string activityType = "StartTimerFromClipboard";

        public StartTimerFromClipboardIntentHandler(ITogglApi togglApi)
        {
            togglAPI = togglApi;
            InvokeOnMainThread(() => {
                 clipboardText = UIPasteboard.General.String;
            });
        }

        public override void ConfirmStartTimerFromClipboard(StartTimerFromClipboardIntent intent, Action<StartTimerFromClipboardIntentResponse> completion)
        {
            var userActivity = new NSUserActivity(activityType);
            if (togglAPI == null)
            {
                userActivity.SetResponseText(Resources.SiriShortcutLoginToUseShortcut);
                completion(new StartTimerFromClipboardIntentResponse(StartTimerFromClipboardIntentResponseCode.FailureNoApiToken, userActivity));
                return;
            }

            userActivity.SetResponseText(clipboardText);
            completion(new StartTimerFromClipboardIntentResponse(StartTimerFromClipboardIntentResponseCode.Ready, userActivity));
        }

        public override void HandleStartTimerFromClipboard(StartTimerFromClipboardIntent intent, Action<StartTimerFromClipboardIntentResponse> completion)
        {
            var userActivity = new NSUserActivity(activityType);
            var timeEntry = createTimeEntry(intent);
            togglAPI.TimeEntries.Create(timeEntry).Subscribe(te =>
            {
                SharedStorage.instance.SetNeedsSync(true);
                SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.StartTimer(te));
                userActivity.SetResponseText(clipboardText);
                var response = new StartTimerFromClipboardIntentResponse(StartTimerFromClipboardIntentResponseCode.Success, userActivity);
                completion(response);
            }, exception =>
            {
                SharedStorage.instance.AddSiriTrackingEvent(SiriTrackingEvent.Error(exception.Message));
                userActivity.SetResponseText(Resources.SomethingWentWrongTryAgain);
                completion(new StartTimerFromClipboardIntentResponse(StartTimerFromClipboardIntentResponseCode.Failure, userActivity));
            });
        }

        private TimeEntry createTimeEntry(StartTimerFromClipboardIntent intent)
        {
            var workspaceId = intent.Workspace == null ? SharedStorage.instance.GetDefaultWorkspaceId() : (long)Convert.ToDouble(intent.Workspace.Identifier);

            return new TimeEntry(
                workspaceId,
                stringToLong(intent.ProjectId?.Identifier),
                null,
                intent.Billable != null && intent.Billable.Identifier == "True",
                DateTimeOffset.Now,
                null,
                clipboardText ?? string.Empty,
                intent.Tags == null ? new long[0] : stringToLongCollection(intent.Tags.Select(tag => tag.Identifier)),
                (long)SharedStorage.instance.GetUserId(),
                0,
                null,
                DateTimeOffset.Now
            );
        }

        private long? stringToLong(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            return (long)Convert.ToDouble(str);
        }

        private IEnumerable<long> stringToLongCollection(IEnumerable<string> strings)
        {
            if (strings.Count() == 0)
                return new long[0];

            return strings.Select(stringToLong).Cast<long>();
        }
    }
}
