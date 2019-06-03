using System;
namespace Toggl.Core.Exceptions
{
    public sealed class OutOfBoundsDateTimeCreatedException : Exception
    {
        public override string StackTrace { get; }

        public OutOfBoundsDateTimeCreatedException(string message, string stackTrace)
            : base(message)
        {
            StackTrace = stackTrace;
        }
    }
}
