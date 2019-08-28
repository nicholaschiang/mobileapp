// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.iOS
{
	[Register ("SuggestionView")]
	partial class SuggestionView
	{
		[Outlet]
		UIKit.UILabel ClientLabel { get; set; }

		[Outlet]
		UIKit.UIImageView ContinueIcon { get; set; }

		[Outlet]
		Toggl.iOS.Views.FadeView DescriptionFadeView { get; set; }

		[Outlet]
		UIKit.UILabel DescriptionLabel { get; set; }

		[Outlet]
		UIKit.UIImageView ProjectDot { get; set; }

		[Outlet]
		Toggl.iOS.Views.FadeView ProjectFadeView { get; set; }

		[Outlet]
		UIKit.UILabel ProjectLabel { get; set; }

		[Outlet]
		UIKit.UIView ProjectView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ClientLabel != null) {
				ClientLabel.Dispose ();
				ClientLabel = null;
			}

			if (DescriptionFadeView != null) {
				DescriptionFadeView.Dispose ();
				DescriptionFadeView = null;
			}

			if (DescriptionLabel != null) {
				DescriptionLabel.Dispose ();
				DescriptionLabel = null;
			}

			if (ProjectDot != null) {
				ProjectDot.Dispose ();
				ProjectDot = null;
			}

			if (ProjectFadeView != null) {
				ProjectFadeView.Dispose ();
				ProjectFadeView = null;
			}

			if (ProjectLabel != null) {
				ProjectLabel.Dispose ();
				ProjectLabel = null;
			}

			if (ProjectView != null) {
				ProjectView.Dispose ();
				ProjectView = null;
			}

			if (ContinueIcon != null) {
				ContinueIcon.Dispose ();
				ContinueIcon = null;
			}
		}
	}
}
