using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace StreamDeckHub
{
    static class Settings
    {
        static public void SaveButtons(StreamDeckHub.Hadrware.StreamDeckButton[] buttons)
        {
            System.IO.File.WriteAllText(Constants.JSON_BUTTON_FILE, JsonSerializer.Serialize(buttons));
        }

        static public StreamDeckHub.Hadrware.StreamDeckButton[] LoadButtons()
        {
            StreamDeckHub.Hadrware.StreamDeckButton[] buttons = null;

            try
            {
                string jsonFile = System.IO.File.ReadAllText(Constants.JSON_BUTTON_FILE);
                buttons = JsonSerializer.Deserialize <StreamDeckHub.Hadrware.StreamDeckButton[]>(jsonFile);
            }
            catch { }
            

            return buttons;
        }
    }
}
