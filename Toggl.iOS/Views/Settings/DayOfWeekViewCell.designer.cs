// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.iOS.Views.Settings
{
	[Register ("DayOfWeekViewCell")]
	partial class DayOfWeekViewCell
	{
		[Outlet]
		UIKit.UILabel DayOfWeekLabel { get; set; }

		[Outlet]
		UIKit.UIImageView SelectedImageView { get; set; }

		[Outlet]
		UIKit.UIView Separator { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (DayOfWeekLabel != null) {
				DayOfWeekLabel.Dispose ();
				DayOfWeekLabel = null;
			}

			if (SelectedImageView != null) {
				SelectedImageView.Dispose ();
				SelectedImageView = null;
			}

			if (Separator != null) {
				Separator.Dispose ();
				Separator = null;
			}
		}
	}
}
