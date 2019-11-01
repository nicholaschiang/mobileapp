using System;
using System.Linq;
using Toggl.Shared;

namespace Toggl.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsAnonymized(this Exception exception)
            => Attribute.IsDefined(exception.GetType(), typeof(IsAnonymizedAttribute));

        public static Exception UnwrapSingle(this Exception exception)
            => exception is AggregateException aggregate
                ? aggregate.Flatten().InnerExceptions.Single()
                : exception;
    }
}
