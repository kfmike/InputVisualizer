/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.RetroSpy
{
    public static class Sega
    {
        private const int PACKET_SIZE = 13;
        private const int MOUSE_PACKET_SIZE = 24;

        private static readonly string[] BUTTONS = {
            "ctrl", "up", "down", "left", "right", "b", "c", "a", "start", "z", "y", "x", "mode"
        };

        private static readonly string[] MOUSE_BUTTONS = {
            "left", "right", "middle", "start"
        };

        private static float ReadMouse(bool sign, bool over, byte data)
        {
            float val = over ? 1.0f : sign ? 0xFF - data : data;
            return val * (sign ? -1 : 1) / 255;
        }

        public static ControllerStateEventArgs? ReadFromPacket(byte[]? packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet.Length != PACKET_SIZE && packet.Length != MOUSE_PACKET_SIZE)
            {
                return null;
            }

            ControllerStateBuilder state = new();
            if (packet.Length == PACKET_SIZE)
            {
                for (int i = 0; i < BUTTONS.Length; ++i)
                {
                    if (string.IsNullOrEmpty(BUTTONS[i]))
                    {
                        continue;
                    }

                    state.SetButton(BUTTONS[i], packet[i] != 0x00);
                }
                state.SetButton("1", packet[5] != 0);
                state.SetButton("2", packet[6] != 0);
            }
            else if (packet.Length == MOUSE_PACKET_SIZE)
            {
                for (int i = 0; i < MOUSE_BUTTONS.Length; ++i)
                {
                    if (string.IsNullOrEmpty(MOUSE_BUTTONS[i]))
                    {
                        continue;
                    }

                    state.SetButton(MOUSE_BUTTONS[i], packet[i] != 0x00);
                }

                bool xSign = packet[4] != 0;
                bool ySign = packet[5] != 0;
                bool xOver = packet[6] != 0;
                bool yOver = packet[7] != 0;

                byte xVal = SignalTool.ReadByteBackwards(packet, 8);
                byte yVal = SignalTool.ReadByteBackwards(packet, 16);

                float x = ReadMouse(xSign, xOver, xVal);
                float y = ReadMouse(ySign, yOver, yVal);

                SignalTool.SetMouseProperties(x, y, xVal, yVal, state);
            }

            return state.Build();
        }
    }
}
