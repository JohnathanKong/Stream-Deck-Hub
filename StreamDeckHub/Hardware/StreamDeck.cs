using OpenMacroBoard.SDK;
using StreamDeckHub.NotificationListener;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace StreamDeckHub.Hardware
{
    class StreamDeck
    {
        private StreamDeckSharp.IStreamDeckBoard _streamDeck = null;
        private StreamDeckButton[] _streamDeckButtons = null;
        private List<int> _levels = new List<int>();
        private StreamDeckButton[] _currentButtons = null;
        private int _currentLevel = 0;
        private Dictionary<string, List<uint>> _currentNotifications = new Dictionary<string, List<uint>>();
        private System.Timers.Timer _backToMain = null;
        private WMPLib.WindowsMediaPlayer _buttonClickSound = null;

        public StreamDeck(StreamDeckButton[] buttons)
        {
            if (Constants.ENABLE_BUTTON_CLICK)
            {
                this._buttonClickSound = new WMPLib.WindowsMediaPlayer();
                this._buttonClickSound.URL = @"Assets\Sounds\button-click.mp3";
                this._buttonClickSound.controls.stop();
            }

            // only activate the timer if it's greater than zero
            if (Constants.BACK_TO_MAIN_TIMER_IN_SECONDS > 0)
            {
                this._backToMain = new System.Timers.Timer();
                this._backToMain.Interval = (int)TimeSpan.FromSeconds(Constants.BACK_TO_MAIN_TIMER_IN_SECONDS).TotalMilliseconds;
                this._backToMain.Enabled = true;
                this._backToMain.Elapsed += new System.Timers.ElapsedEventHandler(BackToMain);
            }

            this._streamDeck = StreamDeckSharp.StreamDeck.OpenDevice();

            this._streamDeckButtons = buttons;

            this._streamDeck.KeyStateChanged += this.ButtonClicked;

            this.DrawButtons();
        }

        public void Dispose()
        {
            this._streamDeck.Dispose();
            this._streamDeck = null;
        }

        public void ApplyNotification(Dictionary<string, List<uint>> notifications)
        {
            if (notifications != null && notifications.Count > 0)
            {
                this._currentNotifications = notifications;
            }
            else
            {
                this._currentNotifications.Clear();
            }
            this.DrawButtons();
        }

        private void DrawButtons()
        {
            StreamDeckButton[] buttons = this._streamDeckButtons;
            int currentLevel = 0;

            // drill down to get the current level. This helps with going forward and back 
            // the tree.
            foreach (int level in this._levels)
            {
                // add the back button only if we are in a sub directory.
                if (buttons[level].ChildElements != null && buttons[level].ChildElements.Count > 0)
                {
                    List<StreamDeckButton> newButtons = new List<StreamDeckButton>(buttons[level].ChildElements);
                    newButtons.Insert(0, new StreamDeckButton
                    {
                        Name = "back",
                        IconPath = "Assets/Images/Flashback.png",
                        Action = StreamDeckButtonAction.BackButton
                    });
                    buttons = newButtons.ToArray();
                    currentLevel += 1;
                }
            }

            // if our level has changed, we need to clear the screen
            if (currentLevel != this._currentLevel)
            {
                this._streamDeck.ClearKeys();
                this._currentLevel = currentLevel;
            }

            this._currentButtons = buttons;

            for (int count = 0; count < buttons.Length; count++)
            {
                this.AddButton(count, buttons[count]);
            }

            // I want a timer to reset to main
            if (this._currentLevel > 0)
            {
                this.StartBackTimer();
            }
        }


        private void AddButton(int index, StreamDeckButton button)
        {
            if (button.Action == StreamDeckButtonAction.Blank)
            {
                return;
            }

            bool notification = false;
            if (this._currentNotifications.Count > 0)
            {
                notification = this._currentNotifications.ContainsKey(button.Name.ToLower());
            }
            this._streamDeck.SetKeyBitmap(index, this.CreateBitmap(button.IconPath, button.NotificationIconPath, notification));
        }

        private void ButtonClicked(object sender, OpenMacroBoard.SDK.KeyEventArgs e)
        {
            if (e.IsDown) return;
            if (this._currentButtons.Length < e.Key) return;

            // there is some action, so stop the timer
            this.StopBackTimer();

            if (this._buttonClickSound != null)
            {
                this._buttonClickSound.controls.currentPosition = 0;
                this._buttonClickSound.controls.play();
            }

            StreamDeckButton button = null;

            button = this._currentButtons[e.Key];

            // on button click I want to remove the notification
            if (this._currentNotifications.ContainsKey(button.Name.ToLower()))
            {
                // clean up the notifications
                Notifications notification = new Notifications();
                notification.ClearNotifications(this._currentNotifications[button.Name.ToLower()]);

                // remove it from our notification list so we don't recount it.
                this._currentNotifications.Remove(button.Name.ToLower());

                // redraw the button, hopefully without the notification
                this.AddButton(e.Key, button);
            }

            // button actions! Let's do some cool things
            switch (button.Action)
            {
                case StreamDeckButtonAction.OpenProgram:
                    this.OpenTarget(button.Target);
                    break;
                case StreamDeckButtonAction.KeyPress:
                    this.PressKeys(button.KeyPresses);
                    break;
                case StreamDeckButtonAction.Folder:
                    this.OpenFolder(e.Key);
                    break;
                case StreamDeckButtonAction.BackButton:
                    this.BackFolder();
                    break;
                case StreamDeckButtonAction.ClearNotifications:
                    this.ClearNotifications();
                    break;
            }

            // I want a timer to reset after a button has been clicked.
            if (this._currentLevel > 0)
            {
                this.StartBackTimer();
            }
        }

        private void OpenTarget(string target)
        {
            string directory = System.IO.Path.GetDirectoryName(target);

            ProcessStartInfo process = new ProcessStartInfo(target);
            process.UseShellExecute = true;
            if (System.IO.Directory.Exists(directory))
            {
                process.WorkingDirectory = directory;
            }

            System.Diagnostics.Process.Start(process);
        }

        private void PressKeys(Keys[] keys)
        {
            StreamDeckHub.Hardware.Keyboard.Press(keys);
        }
        private void OpenFolder(int index)
        {
            this._levels.Add(index);
            this.DrawButtons();
        }

        private void BackFolder()
        {
            if (this._levels.Count > 0)
            {
                this._levels.RemoveAt(this._levels.Count - 1);
                this.DrawButtons();
            }
        }

        private async void ClearNotifications()
        {
            Notifications notification = new Notifications();
            await notification.GetNotifications();
            notification.ClearNotifications();
            this._currentNotifications.Clear();
            this.DrawButtons();
        }

        private void StartBackTimer()
        {
            if (this._backToMain != null)
            {
                this._backToMain.Start();
            }
        }

        private void StopBackTimer()
        {
            if (this._backToMain != null)
            {
                this._backToMain.Stop();
            }
        }

        private void BackToMain(object sender, EventArgs e)
        {
            this._levels.Clear(); ;
            this.DrawButtons();
            this.StopBackTimer();
        }

        private KeyBitmap CreateBitmap(string icon, string notificationIcon, bool AddNotification)
        {

            Image iconImage = null;
            string displayIcon = icon;

            if (AddNotification && !string.IsNullOrWhiteSpace(notificationIcon))
            {
                displayIcon = notificationIcon;
            }

            if (displayIcon.StartsWith("http"))
            {
                WebClient wc = new WebClient();
                byte[] bytes = wc.DownloadData(displayIcon);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    iconImage = System.Drawing.Image.FromStream(ms);
                    ms.Close();
                }

            }
            else
            {
                using (FileStream fs = new FileStream(displayIcon, FileMode.Open))
                {
                    iconImage = System.Drawing.Image.FromStream(fs);
                    fs.Close();
                }

            }
            
            return KeyBitmap.Create.FromGraphics(Constants.BUTTON_SIZE, Constants.BUTTON_SIZE, g =>
           {
               g.SmoothingMode = SmoothingMode.HighQuality;
               g.InterpolationMode = InterpolationMode.HighQualityBicubic;
               g.PixelOffsetMode = PixelOffsetMode.HighQuality;

               // draw the image
               //g.DrawImage(System.Drawing.Image.FromFile(icon, true), Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2), Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2));
               g.DrawImage(iconImage, Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2), Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2));

               if (AddNotification)
               {
                   g.DrawRectangle(new Pen(Brushes.Orange, Constants.ICON_PADDING), Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2), Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2));
                   g.FillEllipse(Brushes.Orange, Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.NOTIFICATION_SIZE, Constants.NOTIFICATION_SIZE);
                   g.DrawEllipse(new Pen(Brushes.Black, 2), Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.NOTIFICATION_SIZE, Constants.NOTIFICATION_SIZE);
               }
           });
        }
    }
}
