using Android.Content;
using Android.Runtime;
using Android.Support.Design.Animation;
using Android.Support.Design.Transformation;
using Android.Util;

namespace Toggl.Droid.Presentation
{
    [Preserve]
    [Register("toggl.droid.presentation.RunningTimeEntrySheetBehavior")]
    public sealed class RunningTimeEntrySheetBehavior : FabTransformationSheetBehavior
    {
        public RunningTimeEntrySheetBehavior()
        {
        }

        public RunningTimeEntrySheetBehavior(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
        }

        protected override FabTransformationSpec OnCreateMotionSpec(Context context, bool expanded)
        {
            var motionSpecRes = expanded ? Resource.Animator.Expand : Resource.Animator.Collapse;
            var motionSpec = base.OnCreateMotionSpec(context, expanded);
            motionSpec.Timings = MotionSpec.CreateFromResource(context, motionSpecRes);
            return motionSpec;
        }
    }
}
