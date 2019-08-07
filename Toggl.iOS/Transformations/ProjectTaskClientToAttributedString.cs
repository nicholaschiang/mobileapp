using Foundation;
using System;
using System.Text;
using Toggl.iOS.Extensions;
using Toggl.Shared;
using UIKit;

namespace Toggl.iOS.Transformations
{
    public class ProjectTaskClientToAttributedString
    {
        private const string projectDotImageResource = "icProjectDot";
        private readonly UIColor clientColor;
        private readonly nfloat fontHeight;

        public ProjectTaskClientToAttributedString(nfloat fontHeight, UIColor clientColor)
        {
            Ensure.Argument.IsNotNull(clientColor, nameof(clientColor));

            this.clientColor = clientColor;
            this.fontHeight = fontHeight;
        }

        public NSAttributedString Convert(string project, string task, string client, UIColor color)
        {
            var dotString = projectDotImageResource.GetAttachmentString(fontHeight);
            var clone = new NSMutableAttributedString(dotString);
            var dottedString = tryAddColorToDot(clone, color);
            var projectInfo = buildString(project, task, client, color);
            dottedString.Append(projectInfo);

            return dottedString;
        }

        private static NSMutableAttributedString tryAddColorToDot(NSMutableAttributedString dotString, UIColor color)
        {
            if (color == null) return dotString;

            var range = new NSRange(0, 1);
            var attributes = new UIStringAttributes { ForegroundColor = color };
            dotString.AddAttributes(attributes, range);

            return dotString;
        }

        private NSAttributedString buildString(string project, string task, string client, UIColor color)
        {
            var builder = new StringBuilder();
            var hasClient = !string.IsNullOrEmpty(client);

            if (!string.IsNullOrEmpty(project))
                builder.Append($" {project}");

            if (!string.IsNullOrEmpty(task))
                builder.Append($": {task}");

            if (hasClient)
                builder.Append($" {client}");

            var text = builder.ToString();

            var result = new NSMutableAttributedString(text);
            var clientIndex = text.Length - (client?.Length ?? 0);

            var projectNameRange = new NSRange(0, clientIndex);
            var projectNameAttributes = new UIStringAttributes { ForegroundColor = color };
            result.AddAttributes(projectNameAttributes, projectNameRange);

            if (!hasClient) return result;

            var clientNameRange = new NSRange(clientIndex, client.Length);
            var clientNameAttributes = new UIStringAttributes { ForegroundColor = clientColor };
            result.AddAttributes(clientNameAttributes, clientNameRange);

            return result;
        }
    }
}
