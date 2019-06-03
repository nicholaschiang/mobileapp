using System;
using Intents;
using Toggl.iOS.Intents;

namespace Toggl.iOS.Models
{
    public enum SiriShortcutType
    {
        Start,
        StartFromClipboard,
        Continue,
        Stop,
        CustomStart,
        ShowReport,
        CustomReport
    }

    static class SiriShortcutTypeExtensions
    {
        public static string Title(this SiriShortcutType shortcutType)
        {
            switch (shortcutType)
            {
                case SiriShortcutType.Start:
                    return "Start timer";
                case SiriShortcutType.StartFromClipboard:
                    return "Start from clipboard";
                case SiriShortcutType.Continue:
                    return "Continue tracking";
                case SiriShortcutType.Stop:
                    return "Stop running entry";
                case SiriShortcutType.CustomStart:
                    return "Start timer with custom details";
                case SiriShortcutType.ShowReport:
                    return "Show report";
                case SiriShortcutType.CustomReport:
                    return "Show custom report";
                default:
                    throw new ArgumentOutOfRangeException(nameof(shortcutType), shortcutType, null);
            }
        }
    }
    static class INIntentExtensions
    {
        public static SiriShortcutType ShortcutType(this INIntent intent)
        {
            if (intent is StartTimerIntent startTimerIntent)
            {
                if (startTimerIntent.EntryDescription != null ||
                    startTimerIntent.Billable != null ||
                    startTimerIntent.Tags != null ||
                    startTimerIntent.ProjectId != null)
                    return SiriShortcutType.CustomStart;

                return SiriShortcutType.Start;
            }

            if (intent is StartTimerFromClipboardIntent startFromClipboardTimerIntent)
            {
                if (startFromClipboardTimerIntent.Billable != null ||
                    startFromClipboardTimerIntent.Tags != null ||
                    startFromClipboardTimerIntent.ProjectId != null)
                    return SiriShortcutType.CustomStart;

                return SiriShortcutType.StartFromClipboard;
            }

            if (intent is StopTimerIntent)
                return SiriShortcutType.Stop;

            if (intent is ContinueTimerIntent)
                return SiriShortcutType.Continue;

            if (intent is ShowReportIntent)
                return SiriShortcutType.ShowReport;

            if (intent is ShowReportPeriodIntent)
                return SiriShortcutType.CustomReport;

            throw new ArgumentOutOfRangeException(nameof(intent));
        }
    }
}
