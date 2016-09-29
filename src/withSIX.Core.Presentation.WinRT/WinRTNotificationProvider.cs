// <copyright company="SIX Networks GmbH" file="WinRTNotificationProvider.cs">
//     Copyright (c) SIX Networks GmbH. All rights reserved. Do not remove this notice.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using SN.withSIX.Core.Applications.Services;

namespace SN.withSIX.Core.Presentation.WinRT
{
    public class WinRTNotificationProvider : INotificationProvider
    {
        public Task<bool?> Notify(string subject, string text, string icon = null, TimeSpan? expirationTime = null,
            params TrayAction[] actions) {
            const ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText04;
            var toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            var stringElements = toastXml.GetElementsByTagName("text");
            if (subject != null)
                stringElements[0].AppendChild(toastXml.CreateTextNode(subject));
            if (text != null)
                stringElements[1].AppendChild(toastXml.CreateTextNode(text));
            /*
            var textElements = toastXml.GetElementsByTagName("text");
            var titleEl = textElements[0];
            var textEl = textElements[1];
            foreach (var c in titleEl.ChildNodes.ToArray())
                titleEl.RemoveChild(c);
            foreach (var c in textEl.ChildNodes.ToArray())
                titleEl.RemoveChild(c);
            titleEl.AppendChild(toastXml.CreateTextNode(subject));
            textEl.AppendChild(toastXml.CreateTextNode(text));
            */

            //TODO: Convert ?
            if (icon != null) {
                var imgEl = (XmlElement) toastXml.GetElementsByTagName("image")[0];
                imgEl.SetAttribute("src", icon);
            }

            if (actions.Any())
                stringElements[2].AppendChild(toastXml.CreateTextNode("Click to " + actions[0].DisplayName));

            var notification = new ToastNotification(toastXml) {
                ExpirationTime = expirationTime == null ? null : (DateTime?) DateTime.UtcNow.Add(expirationTime.Value)
            };
            if (actions.Any())
                notification.Activated += (sender, args) => actions[0].Command.Execute(null);
            var tcs = GenerateTcs(notification);
            var packageId = "Sync" + Common.ReleaseTitle?.ToLower();
            var notifier =
                ToastNotificationManager.CreateToastNotifier($"com.squirrel.{packageId}.Sync");
                //  TODO: Configure per app?
            notifier.Show(notification);
            return tcs.Task;
        }

        static TaskCompletionSource<bool?> GenerateTcs(ToastNotification notification) {
            var tcs = new TaskCompletionSource<bool?>();
            notification.Dismissed += (sender, args) => tcs.SetResult(false);
            notification.Activated += (sender, args) => tcs.SetResult(true);
            notification.Failed += (sender, args) => tcs.SetException(args.ErrorCode);
            return tcs;
        }
    }
}