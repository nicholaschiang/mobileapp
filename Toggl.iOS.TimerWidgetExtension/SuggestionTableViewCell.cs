﻿using System;
using Foundation;
using Toggl.iOS.AppExtensions.Extensions;
using Toggl.iOS.AppExtensions.Models;
using Toggl.Shared;
using UIKit;

namespace Toggl.iOS.TimerWidgetExtension
{
    public partial class SuggestionTableViewCell : UITableViewCell
    {
        public static string Identifier = "SuggestionCell";
        public static readonly UINib Nib;

        static SuggestionTableViewCell()
        {
            Nib = UINib.FromName(nameof(SuggestionTableViewCell), NSBundle.MainBundle);
        }

        protected SuggestionTableViewCell(IntPtr handle) : base(handle)
        {
        }

        public void PopulateCell(Suggestion suggestion)
        {
            DescriptionLabel.Text = suggestion.Description;
            if (suggestion.ProjectName != string.Empty)
            {
                ProjectLabel.Text = suggestion.ProjectName;
                DotView.BackgroundColor = new Color(suggestion.ProjectColor).ToNativeColor();
                ProjectLabel.Hidden = false;
                DotView.Hidden = false;
            }
            else
            {
                ProjectLabel.Hidden = true;
                DotView.Hidden = true;
            }
        }
    }
}