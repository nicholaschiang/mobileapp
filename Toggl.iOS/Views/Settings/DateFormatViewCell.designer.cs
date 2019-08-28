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
	[Register ("DateFormatViewCell")]
	partial class DateFormatViewCell
	{
		[Outlet]
		UIKit.UILabel DateFormatLabel { get; set; }

		[Outlet]
		UIKit.UIImageView SelectedImageView { get; set; }

		[Outlet]
		UIKit.UIView Separator { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (DateFormatLabel != null) {
				DateFormatLabel.Dispose ();
				DateFormatLabel = null;
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
