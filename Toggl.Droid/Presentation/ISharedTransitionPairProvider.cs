using Android.App;
using Android.Util;
using System;

namespace Toggl.Droid.Presentation
{
    internal interface ISharedTransitionPairProvider
    {
        Activity Activity { get; }

        Pair[] GetAnimationsPairsFor(Type viewModelType);
    }
}
