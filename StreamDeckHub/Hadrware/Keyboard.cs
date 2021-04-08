using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StreamDeckHub.Hadrware
{
    /// <summary>
    /// Methods to simulate key presses in windows.
    /// </summary>
    class Keyboard
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
        [DllImport("user32.dll")]
        private static extern IntPtr GetMessageExtraInfo();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// Simulate key presses. The method will press all passed keys first, then release them all in the order
        /// that they were sent.
        /// </summary>
        /// <param name="keys">An array of keys to press</param>
        static public void Press(Keys[] keys)
        {
            List<Input> keyUpChain = new List<Input>();
            List<Input> keyDownChain = new List<Input>();
            Input[] keyChain = null;

            foreach (Keys key in keys)
            {
                keyDownChain.Add(new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)MapVirtualKey((uint)key, (uint)0x0),
                            dwFlags = (uint)(KeyEventF.KeyDown | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                });

                keyUpChain.Add(new Input
                {
                    type = (int)InputType.Keyboard,
                    u = new InputUnion
                    {
                        ki = new KeyboardInput
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)MapVirtualKey((uint)key, (uint)0x0),
                            dwFlags = (uint)(KeyEventF.KeyUp | KeyEventF.Scancode),
                            dwExtraInfo = GetMessageExtraInfo()
                        }
                    }
                });
            }

            keyDownChain.AddRange(keyUpChain);
            keyChain = keyDownChain.ToArray();

            SendInput((uint)keyChain.Length, keyChain, Marshal.SizeOf(typeof(Input)));
        }
    }
}
