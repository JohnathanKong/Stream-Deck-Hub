using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using StreamDeckHub.NotificationListener;
using StreamDeckHub.Hardware;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace StreamDeckHub
{
    public partial class frmMain : Form
    {
        // notification object for checking windows action center notifications
        private Notifications _notification = null;

        // Check notification timer
        private Timer _notificationCheckTimer = null;

        // these are all the buttons, from the root screen
        private StreamDeckButton[] _streamDeckButtons = null;

        // stream deck object to interface with the stream deck
        private StreamDeck _streamDeck = null;

        // the last hash that was generated from the last check
        private string _lastCheckHash = string.Empty;

        // media player for notification sounds
        private WMPLib.WindowsMediaPlayer _wplayer = null;

        public frmMain()
        {
            InitializeComponent();
        }

        private async void frmMain_Load(object sender, EventArgs e)
        {
            // setup notification sounds
            this._wplayer = new WMPLib.WindowsMediaPlayer();
            this._wplayer.URL = @"Assets\Sounds\notification.mp3";
            this._wplayer.controls.stop();

            // load up the buttons
            this._streamDeckButtons = Settings.LoadButtons();

            // initialize the stream deck
            _streamDeck = new StreamDeck(this._streamDeckButtons);

            // create the notification object
            this._notification = new Notifications();

            // check to see if we have permission to access the notifications
            if (!(await this._notification.RequestAccess()))
            {
                throw new Exception("Unable to get notification access from user");
            }


            // setup the timer
            this._notificationCheckTimer = new Timer();
            this._notificationCheckTimer.Interval = (int)TimeSpan.FromSeconds(Constants.CHECK_TIME_IN_SECONDS).TotalMilliseconds;
            this._notificationCheckTimer.Tick += new EventHandler(CheckNotification);
            this._notificationCheckTimer.Start();

            // timer will execute every minute, so let's check notifications right on start up
            this.CheckNotification(null, null);

        }

        private void ClearNotification(Object myObject, EventArgs myEventArgs)
        {
            this._notification.ClearNotifications();
        }

        private async void CheckNotification(Object myObject, EventArgs myEventArgs)
        {
            // get the pending notifications
            List<Notification> notifications = await this._notification.GetNotifications();
            Dictionary<string, List<uint>> notificationCounts = new Dictionary<string, List<uint>>();
            StringBuilder hashText = new StringBuilder();

            // loop through and count the notifications.
            foreach (Notification notification in notifications)
            {
                hashText.Append(notification.NotificationId);
                hashText.Append(notification.CreationTimestamp);
                hashText.Append(notification.DisplayName);

                string key = notification.DisplayName.ToLower();
                if (!notificationCounts.ContainsKey(key))
                {
                    notificationCounts.Add(key, new List<uint>());
                }

                notificationCounts[key].Add(notification.NotificationId);
            }

            // generate the hash
            string hash = this.CreateMD5Hash(JsonSerializer.Serialize(hashText.ToString()));

            // check if the hash has changed
            if (this._lastCheckHash != hash)
            {
                // easier for me to visualize. If the class hash is empty, that means we just started up.
                // So if we just started and there are no notifications, don't do anything
                if (!string.IsNullOrWhiteSpace(this._lastCheckHash) || (string.IsNullOrWhiteSpace(this._lastCheckHash) && notificationCounts.Count > 0))
                {
                    // push the new notifications to the stream deck
                    this._streamDeck.ApplyNotification(notificationCounts);

                    if(notificationCounts.Count > 0)
                    {
                        this._wplayer.controls.currentPosition = 0;
                        this._wplayer.controls.play();
                    }

                    // reset the hash
                    this._lastCheckHash = hash;
                }
            }

        }

        private void niMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // run cleanup
            this._notificationCheckTimer.Start();
            this._notificationCheckTimer = null;
            this._streamDeck.Dispose();
            this._streamDeck = null;
        }

        private void cmsMain_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "tsmiExit")
            {
                this.Close();
            }
        }

        private string CreateMD5Hash(string input)
        {
            // Step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
