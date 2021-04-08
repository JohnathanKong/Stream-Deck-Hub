using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace StreamDeckHub.NotificationListener
{
    /// <summary>
    /// Contains methose for interfacing with the windows 10 notification system.
    /// </summary>
    class Notifications
    {
        private UserNotificationListener _listener = null;
        private List<uint> _allNotifications = new List<uint>();

        public Notifications()
        {
            // Get the listener
            this._listener = UserNotificationListener.Current;
        }

        public async Task<bool> RequestAccess()
        {
            // And request access to the user's notifications (must be called from UI thread)
            UserNotificationListenerAccessStatus accessStatus = await this._listener.RequestAccessAsync();

            return accessStatus == UserNotificationListenerAccessStatus.Allowed;
        }

        public async Task<List<Notification>> GetNotifications()
        {
            IReadOnlyList<UserNotification> notifs = await this._listener.GetNotificationsAsync(NotificationKinds.Toast);
            List<Notification> notifications = new List<Notification>();

            this._allNotifications = new List<uint>();

            foreach (UserNotification notif in notifs)
            {
                NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                string displayName = notif.AppInfo.DisplayInfo.DisplayName;
                string titleText = string.Empty;
                string bodyText = string.Empty;

                // get the body text
                if (toastBinding != null)
                {

                    // And then get the text elements from the toast binding
                    IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();

                    // Treat the first text element as the title text
                    titleText = textElements.FirstOrDefault()?.Text;

                    // We'll treat all subsequent text elements as body text,
                    // joining them together via newlines.
                    bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));

                    // if there are generics, like google chrome, we try to split it up
                    if (displayName.ToLower() == "google chrome")
                    {
                        // check if it's in a time format only.
                        if (System.Text.RegularExpressions.Regex.IsMatch(bodyText, @"^[0-9]{1,2}:[0-9]{1,2}(am|pm){1}\x20{1}–\x20{1}[0-9]{1,2}:[0-9]{1,2}(am|pm){1}$"))
                        {
                            displayName = "calendar";
                        }
                        else
                        {
                            displayName = "gmail";
                        }
                    }
                }

                notifications.Add(new Notification()
                {
                    NotificationId = notif.Id,
                    DisplayName = displayName,
                    Title = titleText,
                    Body = bodyText,
                    CreationTimestamp = notif.CreationTime.Ticks

                }); ;
                this._allNotifications.Add(notif.Id);
            }

            return notifications;
        }

        public void ClearNotifications()
        {
            this.ClearNotifications(this._allNotifications);
        }

        public void ClearNotifications(List<uint> notificationIds)
        {
            foreach (uint notificationId in notificationIds)
            {
                this._listener.RemoveNotification(notificationId);
            }
        }
    }
}
