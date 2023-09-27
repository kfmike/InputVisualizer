/*  
    Copyright (c) RetroSpy Technologies
*/

using System;
using System.Collections.Generic;

namespace InputVisualizer.retrospy
{
    internal static class SignalTool
    {
        /// <summary>
        /// Reads a byte of data from a string of 8 bits in a controller data packet.
        /// </summary>
        public static byte ReadByte(byte[] packet, int offset, byte numBits = 8, byte mask = 0x0F)
        {
            byte val = 0;
            for (int i = 0; i < numBits; ++i)
            {
                if ((packet[i + offset] & mask) != 0)
                {
                    val |= (byte)(1 << (numBits - 1 - i));
                }
            }
            return val;
        }

        public static byte ReadByteBackwards(byte[] packet, int offset, byte numBits = 8, byte mask = 0x0F)
        {
            byte val = 0;
            for (int i = 0; i < numBits; ++i)
            {
                if ((packet[i + offset] & mask) != 0)
                {
                    val |= (byte)(1 << i);
                }
            }
            return val;
        }

        private static float MiddleOfThree(float a, float b, float c)
        {
            // Compare each three number to find middle
            // number. Enter only if a > b
            if (a > b)
            {
                return b > c ? b : a > c ? c : a;
            }
            else
            {
                // Decided a is not greater than b.
                return a > c ? a : b > c ? c : b;
            }
        }

        private class SlidingWindow
        {
            public SlidingWindow()
            {
                windowX = new float[3];
                windowPositionX = 0;
                windowY = new float[3];
                windowPositionY = 0;
            }

            public float[] windowX;
            public int windowPositionX;
            public float[] windowY;
            public int windowPositionY;
        }

        private static readonly Dictionary<string, SlidingWindow> windows = new();

        public static void SetMouseProperties(float x, float y, int xRaw, int yRaw, ControllerStateBuilder state, float maxCircleSize = 1.0f)
        {
            if (!windows.ContainsKey(""))
            {
                windows[""] = new SlidingWindow();
            }

            SetMouseProperties(x, y, xRaw, yRaw, state, maxCircleSize, windows[""], "");
        }

        public static void SetPCMouseProperties(float x, float y, int xRaw, int yRaw, ControllerStateBuilder state, float maxCircleSize = 1.0f)
        {
            if (!windows.ContainsKey("PC_"))
            {
                windows["PC_"] = new SlidingWindow();
            }

            SetMouseProperties(x, y, xRaw, yRaw, state, maxCircleSize, windows["PC_"], "PC_");
        }

        private static void SetMouseProperties(float x, float y, int xRaw, int yRaw, ControllerStateBuilder state,
            float maxCircleSize, SlidingWindow window, string prefix)
        {
            window.windowX[window.windowPositionX] = x;
            window.windowPositionX += 1;
            window.windowPositionX %= 3;

            window.windowY[window.windowPositionY] = y;
            window.windowPositionY += 1;
            window.windowPositionY %= 3;

            y = MiddleOfThree(window.windowY[0], window.windowY[1], window.windowY[2]);
            x = MiddleOfThree(window.windowX[0], window.windowX[1], window.windowX[2]);

            float y1 = y;
            float x1 = x;

            if (y != 0 || x != 0)
            {
                // Direction shows around the unit circle
                double radian = Math.Atan2(y, x);
                x1 = maxCircleSize * (float)Math.Cos(radian);
                y1 = maxCircleSize * (float)Math.Sin(radian);

                // Don't let magnitude exceed the unit circle
                if (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) > maxCircleSize)
                {
                    x = x1;
                    y = y1;
                }
            }

            state.SetAnalog(prefix + "mouse_center_x", 0, 0);
            state.SetAnalog(prefix + "mouse_center_y", 0, 0);
            state.SetAnalog(prefix + "mouse_direction_x", x1, xRaw);
            state.SetAnalog(prefix + "mouse_direction_y", y1, yRaw);
            state.SetAnalog(prefix + "mouse_magnitude_x", x, xRaw);
            state.SetAnalog(prefix + "mouse_magnitude_y", y, yRaw);
        }

        public static void FakeAnalogStick(byte up, byte down, byte left, byte right, ControllerStateBuilder state, string xName, string yName)
        {
            float x = 0;
            float y = 0;

            if (right != 0x00)
            {
                x = 1;
            }
            else if (left != 0x00)
            {
                x = -1;
            }

            if (up != 0x00)
            {
                y = 1;
            }
            else if (down != 0x00)
            {
                y = -1;
            }

            if (y != 0 || x != 0)
            {
                // point on the unit circle at the same angle
                double radian = Math.Atan2(y, x);
                float x1 = (float)Math.Cos(radian);
                float y1 = (float)Math.Sin(radian);

                // Don't let magnitude exceed the unit circle
                if (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) > 1.0)
                {
                    x = x1;
                    y = y1;
                }
            }

            state.SetAnalog(xName, x, 0);
            state.SetAnalog(yName, y, 0);
        }

        public static void GenerateFakeStick(ControllerStateBuilder state, string xname, string yname, bool up, bool down, bool left, bool right)
        {
            float x = 0;
            float y = 0;

            if (right)
            {
                x = 1;
            }
            else if (left)
            {
                x = -1;
            }

            if (up)
            {
                y = 1;
            }
            else if (down)
            {
                y = -1;
            }

            if (y != 0 || x != 0)
            {
                // point on the unit circle at the same angle
                double radian = Math.Atan2(y, x);
                float x1 = (float)Math.Cos(radian);
                float y1 = (float)Math.Sin(radian);

                // Don't let magnitude exceed the unit circle
                if (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) > 1.0)
                {
                    x = x1;
                    y = y1;
                }
            }

            state.SetAnalog(xname, x, 0);
            state.SetAnalog(yname, y, 0);
        }
    }
}
