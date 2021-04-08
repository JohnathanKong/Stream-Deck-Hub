﻿using OpenMacroBoard.SDK;
using StreamDeckHub.NotificationListener;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace StreamDeckHub.Hadrware
{
    class StreamDeck
    {
        private StreamDeckSharp.IStreamDeckBoard _streamDeck = null;
        private StreamDeckButton[] _streamDeckButtons = null;
        private List<int> _levels = new List<int>();
        private StreamDeckButton[] _currentButtons = null;
        private int _currentLevel = 0;
        private Dictionary<string, List<uint>> _currentNotifications = new Dictionary<string, List<uint>>();

        public StreamDeck(StreamDeckButton[] buttons)
        {
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
            if(notifications != null && notifications.Count > 0)
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

            foreach (int level in this._levels)
            {
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

            if (currentLevel != this._currentLevel)
            {
                this._streamDeck.ClearKeys();
            }

            this._currentButtons = buttons;

            for (int count = 0; count < buttons.Length; count++)
            {
                this.AddButton(count, buttons[count]);
            }
        }


        private void AddButton(int index, StreamDeckButton button)
        {
            if(button.Action == StreamDeckButtonAction.Blank)
            {
                return;
            }

            bool notification = false;
            if(this._currentNotifications.Count > 0)
            {
                notification = this._currentNotifications.ContainsKey(button.Name.ToLower());
            }
            this._streamDeck.SetKeyBitmap(index, this.CreateBitmap(button.IconPath, notification));
        }

        private void ButtonClicked(object sender, OpenMacroBoard.SDK.KeyEventArgs e)
        {
            if (e.IsDown) return;

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
        }

        private void OpenTarget(string target)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }

        private void PressKeys(Keys[] keys)
        {
            StreamDeckHub.Hadrware.Keyboard.Press(keys);
        }
        private void OpenFolder(int index)
        {
            this._levels.Add(index);
            this.DrawButtons();
        }

        private void BackFolder()
        {
            if(this._levels.Count > 0)
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

        private KeyBitmap CreateBitmap(string icon, bool AddNotification)
        {
            return KeyBitmap.Create.FromGraphics(Constants.BUTTON_SIZE, Constants.BUTTON_SIZE, g =>
           {
               g.SmoothingMode = SmoothingMode.HighQuality;
               g.InterpolationMode = InterpolationMode.HighQualityBicubic;
               g.PixelOffsetMode = PixelOffsetMode.HighQuality;

               // draw the image
               g.DrawImage(System.Drawing.Image.FromFile(icon, true), Constants.ICON_PADDING, Constants.ICON_PADDING, Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2), Constants.BUTTON_SIZE - (Constants.ICON_PADDING * 2));

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