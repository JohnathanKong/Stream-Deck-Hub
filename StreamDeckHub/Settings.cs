using System.Text.Json;

namespace StreamDeckHub
{
    static class Settings
    {
        static public void SaveButtons(StreamDeckHub.Hardware.StreamDeckButton[] buttons)
        {
            System.IO.File.WriteAllText(Constants.JSON_BUTTON_FILE, JsonSerializer.Serialize(buttons));
        }

        static public StreamDeckHub.Hardware.StreamDeckButton[] LoadButtons()
        {
            StreamDeckHub.Hardware.StreamDeckButton[] buttons = null;

            try
            {
                string jsonFile = System.IO.File.ReadAllText(Constants.JSON_BUTTON_FILE);
                buttons = JsonSerializer.Deserialize <StreamDeckHub.Hardware.StreamDeckButton[]>(jsonFile);
            }
            catch { }
            

            return buttons;
        }
    }
}
