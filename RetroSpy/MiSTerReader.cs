/*  
    Copyright (c) RetroSpy Technologies
*/

using System;
using System.Globalization;

namespace InputVisualizer.RetroSpy
{
    public static class MiSTerReader
    {
        private static readonly string[] AXES_NAMES = {
            "x", "y", "z", "rx", "ry", "rz", "s0", "s1"
        };

        public static ControllerStateEventArgs? ReadFromPacket(byte[]? packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet.Length < 16)
            {
                return null;
            }

            int axes = 0;
            for (byte j = 0; j < 8; ++j)
            {
                axes |= (packet[j] == 0x30 ? 0 : 1) << j;
            }

            int buttons = 0;
            for (byte j = 0; j < 8; ++j)
            {
                buttons |= (packet[8 + j] == 0x30 ? 0 : 1) << j;
            }

            int packetSize = 16 + (axes * 32) + buttons + 1;

            if (packet.Length != packetSize)
            {
                return null;
            }

            byte[] buttonValues = new byte[buttons];
            int[] axesValues = new int[axes];

            for (int i = 0; i < buttons; ++i)
            {
                buttonValues[i] = (byte)((packet[16 + i] == 0x31) ? 1 : 0);
            }

            for (int i = 0; i < axes; ++i)
            {
                axesValues[i] = 0;
                for (byte j = 0; j < 32; ++j)
                {
                    axesValues[i] |= (packet[16 + buttons + (i * 32) + j] == 0x30 ? 0 : 1) << j;
                }
            }

            ControllerStateBuilder outState = new();

            for (int i = 0; i < buttonValues.Length; ++i)
            {
                outState.SetButton("b" + i.ToString(CultureInfo.CurrentCulture), buttonValues[i] != 0x00);
            }

            for (int i = 0; i < axesValues.Length; ++i)
            {
                if (i < AXES_NAMES.Length)
                {
                    outState.SetAnalog(AXES_NAMES[i], axesValues[i] / (float)short.MaxValue, axesValues[i]);
                }

                outState.SetAnalog("a" + i.ToString(CultureInfo.CurrentCulture), axesValues[i] / (float)short.MaxValue, axesValues[i]);
            }

            if (axes >= 2)
            {
                if (axesValues[axes - 2] < 0)
                {
                    outState.SetButton("left", true);
                    outState.SetButton("right", false);
                }
                else if (axesValues[axes - 2] > 0)
                {
                    outState.SetButton("right", true);
                    outState.SetButton("left", false);
                }
                else
                {
                    outState.SetButton("left", false);
                    outState.SetButton("right", false);
                }

                if (axesValues[axes - 1] < 0)
                {
                    outState.SetButton("up", true);
                    outState.SetButton("down", false);
                }
                else if (axesValues[axes - 1] > 0)
                {
                    outState.SetButton("down", true);
                    outState.SetButton("up", false);
                }
                else
                {
                    outState.SetButton("up", false);
                    outState.SetButton("down", false);
                }
            }
            else
            {
                outState.SetButton("up", false);
                outState.SetButton("down", false);
                outState.SetButton("left", false);
                outState.SetButton("right", false);
            }

            return outState.Build();
        }
    }
}