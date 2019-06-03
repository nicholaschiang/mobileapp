using System;
using SystemMath = System.Math;
using static Toggl.Shared.Math;

namespace Toggl.Shared.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        private const int minAllowedYear = 2006;
        private const int maxAllowedYear = 2030;

        public static long ToUnixTimeSeconds(this DateTimeOffset dateTime)
        {
            // constant and implementation taken from .NET reference source:
            // https://referencesource.microsoft.com/#mscorlib/system/datetimeoffset.cs
            const long unixEpochSeconds = 62_135_596_800L;

            var seconds = dateTime.UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            return seconds - unixEpochSeconds;
        }
        public static DateTimeOffset RoundDownToLocalDate(this DateTimeOffset time)
        {
            var localTime = time.ToOffset(DateTimeOffset.Now.Offset);
            return new DateTimeOffset(localTime.Date, localTime.Offset);
        }

        public static DateTimeOffset RoundDownToLocalMonth(this DateTimeOffset time)
        {
            var localTime = time.ToOffset(DateTimeOffset.Now.Offset);
            return new DateTimeOffset(localTime.Year, localTime.Month, 1, 0, 0, 0, localTime.Offset);
        }

        public static DateTimeOffset RoundDownToLocalYear(this DateTimeOffset time)
        {
            var localTime = time.ToOffset(DateTimeOffset.Now.Offset);
            return new DateTimeOffset(localTime.Year, 1, 1, 0, 0, 0, localTime.Offset);
        }

        public static DateTimeOffset RoundToClosestMinute(this DateTimeOffset time)
            => time.Second >= (SecondsInAMinute / 2)
                ? time + TimeSpan.FromSeconds(SecondsInAMinute - time.Second)
                : time - TimeSpan.FromSeconds(time.Second);

        public static DateTimeOffset RoundDownToMinute(this DateTimeOffset time)
            => new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Offset);

        public static DateTimeOffset WithDate(this DateTimeOffset original, DateTimeOffset date)
            => new DateTimeOffset(date.Year, date.Month, date.Day,
                                  original.Hour, original.Minute, original.Second, original.Offset);

        public static DateTimeOffset WithTime(this DateTimeOffset original, DateTimeOffset time)
            => new DateTimeOffset(original.Year, original.Month, original.Day,
                                  time.Hour, time.Minute, time.Second, original.Offset);

        public static DateTimeOffset RoundToClosestQuarter(this DateTimeOffset time)
        {
            var roundDown = RoundDownToClosestQuarter(time);
            var roundUp = RoundUpToClosestQuarter(time);

            if (SystemMath.Abs((roundDown.RoundDownToClosestQuarter() - time).TotalSeconds) > SystemMath.Abs((roundUp.RoundDownToClosestQuarter() - time).TotalSeconds))
            {
                return roundUp;
            }
            else
            {
                return roundDown;
            }
        }

        public static DateTimeOffset RoundDownToClosestQuarter(this DateTimeOffset time)
        {
            var offset = time.Minute % 15;
            var minute = time.Minute - offset;
            return new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, minute, 0, time.Offset);
        }

        public static DateTimeOffset RoundUpToClosestQuarter(this DateTimeOffset time)
        {
            if (time.Minute >= 45)
            {
                var nextHour = time.AddHours(1);
                return new DateTimeOffset(nextHour.Year, nextHour.Month, nextHour.Day, nextHour.Hour, 0, 0, nextHour.Offset);
            }
            else
            {
                var offset = 15 - time.Minute % 15;
                var minute = time.Minute + offset;
                return new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, minute, 0, time.Offset);
            }
        }

        public static DateTimeOffset BeginningOfWeek(this DateTimeOffset time, BeginningOfWeek beginningOfWeek)
        {
            int diff = (7 + ((BeginningOfWeek)time.DayOfWeek - beginningOfWeek)) % 7;
            return time.AddDays(-1 * diff).Date;
        }

        public static bool IsWithinTogglLimits(this DateTimeOffset time)
            => time.Year >= minAllowedYear && time.Year <= maxAllowedYear;
    }
}
