using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace StreamDeckHub.Hadrware
{
    public enum StreamDeckButtonAction
    {
        OpenProgram = 0,
        KeyPress = 1,
        Folder=2,
        BackButton=3,
        ClearNotifications=4,
        Blank=5
    }
    class StreamDeckButton
    {
        public string Name { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public StreamDeckButtonAction Action { get; set; } = StreamDeckButtonAction.Blank;
        public Keys[] KeyPresses { get; set; } = null;
        public string Target { get; set; } = string.Empty;
        public List<StreamDeckButton> ChildElements { get; set; } = null;
    }
}
