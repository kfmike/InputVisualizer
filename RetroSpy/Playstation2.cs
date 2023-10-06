/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.RetroSpy
{
    public static class Playstation2
    {
        private const int PACKET_SIZE = 168;
        private const int PACKET_SIZE_WO_RUMBLE = 152;
        private const int POLISHED_PACKET_SIZE = 35;

        private static readonly string?[] BUTTONS = {
            null, "select", "lstick", "rstick", "start", "up", "right", "down", "left", "l2", "r2", "l1", "r1", "triangle", "circle", "cross", "square"
        };

        private static readonly string?[] ANALOG_BUTTONS = {
            null, null, null, null, "start", "up", "right", "down", "left", "l2", "l1", "square", "triangle", "r1", "circle", "cross", "r2"
        };

        private static readonly string?[] NEGCON_BUTTONS = {
            null, null, null, null, "start", "up", "right", "down", "left", null, null, null, "r1", "a", "b", null, null
        };

        private static float ReadAnalogButton(byte input)
        {
            return (float)input / 256;
        }

        private static float ReadStick(byte input)
        {
            return (float)(input - 128) / 128;
        }
        private static float ReadNegConSteering(byte input)
        {
            return (input - 128.0f) / -128.0f;
        }

        private static float ReadMouse(bool over, byte data)
        {
            float val = over ? data >= 128 ? -1.0f : 1.0f : data >= 128 ? -1.0f * (255 - data) / 127.0f : data / 127.0f;
            return val;
        }

        public static ControllerStateEventArgs? ReadFromPacket(byte[]? packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet.Length < PACKET_SIZE_WO_RUMBLE)
            {
                return null;
            }

            bool hasRumble = packet.Length == PACKET_SIZE;

            byte[] polishedPacket = new byte[POLISHED_PACKET_SIZE];

            polishedPacket[0] = 0;
            for (byte i = 0; i < 8; ++i)
            {
                polishedPacket[0] |= (byte)((packet[i] == 0 ? 0 : 1) << i);
            }

            for (byte i = 0; i < 16; ++i)
            {
                polishedPacket[i + 1] = packet[i + 8];
            }

            int nextNumBytes = 0;
            if (polishedPacket[0] == 0x73 || polishedPacket[0] == 0x79
                    || polishedPacket[0] == 0x53 || polishedPacket[0] == 0x23)
            {
                nextNumBytes = 4;
            }
            else if (polishedPacket[0] == 0x12)
            {
                nextNumBytes = 2;
            }

            for (int i = 0; i < nextNumBytes; ++i)
            {
                polishedPacket[17 + i] = 0;
                for (byte j = 0; j < 8; ++j)
                {
                    polishedPacket[17 + i] |= (byte)((packet[24 + (i * 8) + j] == 0 ? 0 : 1) << j);
                }
            }

            if (polishedPacket[0] == 0x79)
            {
                for (int i = 0; i < 12; ++i)
                {
                    polishedPacket[21 + i] = 0;
                    for (byte j = 0; j < 8; ++j)
                    {
                        polishedPacket[21 + i] |= (byte)((packet[56 + (i * 8) + j] == 0 ? 0 : 1) << j);
                    }
                }
            }

            if (hasRumble)
            {
                for (int i = 0; i < 2; ++i)
                {
                    polishedPacket[33 + i] = 0;
                    for (byte j = 0; j < 8; ++j)
                    {
                        polishedPacket[33 + i] |= (byte)((packet[152 + (i * 8) + j] == 0 ? 0 : 1) << j);
                    }
                }
            }

            if (polishedPacket.Length < (hasRumble ? POLISHED_PACKET_SIZE : POLISHED_PACKET_SIZE - 2))
            {
                return null;
            }

            if (polishedPacket[0] != 0x41 && polishedPacket[0] != 0x73 && polishedPacket[0] != 0x79
                    && polishedPacket[0] != 0x12 && polishedPacket[0] != 0x23 && polishedPacket[0] != 0x53)
            {
                return null;
            }

            ControllerStateBuilder state = new();

            if (polishedPacket[0] == 0x53)
            {
                SetButtons(ANALOG_BUTTONS, polishedPacket, state);
            }
            else if (polishedPacket[0] == 0x23)
            {
                SetButtons(NEGCON_BUTTONS, polishedPacket, state);
            }
            else
            {
                SetButtons(BUTTONS, polishedPacket, state);
            }

            if (hasRumble)
            {
                state.SetAnalog("motor1", ReadAnalogButton(polishedPacket[33]), polishedPacket[33]);
                state.SetAnalog("motor2", ReadAnalogButton(polishedPacket[34]), polishedPacket[34]);
            }
            else
            {
                state.SetAnalog("motor1", 0, 0);
                state.SetAnalog("motor2", 0, 0);
            }

            if (polishedPacket[0] == 0x73 || polishedPacket[0] == 0x79 || polishedPacket[0] == 0x53)
            {
                state.SetAnalog("rstick_x", ReadStick(polishedPacket[17]), polishedPacket[17]);
                state.SetAnalog("rstick_y", ReadStick(polishedPacket[18]), polishedPacket[18]);
                state.SetAnalog("lstick_x", ReadStick(polishedPacket[19]), polishedPacket[19]);
                state.SetAnalog("lstick_y", ReadStick(polishedPacket[20]), polishedPacket[20]);
            }
            else if (polishedPacket[0] == 0x23)
            {
                float steering = ReadNegConSteering(polishedPacket[17]);
                if (steering < 0)
                {
                    state.SetAnalog("l_steering", steering * -1, polishedPacket[17]);
                    state.SetAnalog("r_steering", 0.0f, 0);
                }
                else if (steering > 0)
                {
                    state.SetAnalog("l_steering", 0.0f, 0);
                    state.SetAnalog("r_steering", steering, polishedPacket[17]);
                }
                else
                {
                    state.SetAnalog("l_steering", 0.0f, 0);
                    state.SetAnalog("r_steering", 0.0f, 0);
                }

                state.SetAnalog("I", ReadAnalogButton(polishedPacket[18]), polishedPacket[18]);
                state.SetAnalog("II", ReadAnalogButton(polishedPacket[19]), polishedPacket[19]);
                state.SetAnalog("l1", ReadAnalogButton(polishedPacket[20]), polishedPacket[20]);
            }
            else
            {
                state.SetAnalog("rstick_x", 0, 0);
                state.SetAnalog("rstick_y", 0, 0);
                state.SetAnalog("lstick_x", 0, 0);
                state.SetAnalog("lstick_y", 0, 0);
            }

            if (polishedPacket[0] == 0x79)
            {
                state.SetAnalog("analog_right", polishedPacket[6] != 0x00 ? ReadAnalogButton(polishedPacket[21]) : 0.0f, polishedPacket[6] != 0x00 ? polishedPacket[21] : 0);
                state.SetAnalog("analog_left", polishedPacket[8] != 0x00 ? ReadAnalogButton(polishedPacket[22]) : 0.0f, polishedPacket[8] != 0x00 ? polishedPacket[22] : 0);
                state.SetAnalog("analog_up", polishedPacket[5] != 0x00 ? ReadAnalogButton(polishedPacket[23]) : 0.0f, polishedPacket[5] != 0x00 ? polishedPacket[23] : 0);
                state.SetAnalog("analog_down", polishedPacket[7] != 0x00 ? ReadAnalogButton(polishedPacket[24]) : 0.0f, polishedPacket[7] != 0x00 ? polishedPacket[24] : 0);

                state.SetAnalog("analog_triangle", polishedPacket[13] != 0x00 ? ReadAnalogButton(polishedPacket[25]) : 0.0f, polishedPacket[13] != 0x00 ? polishedPacket[25] : 0);
                state.SetAnalog("analog_circle", polishedPacket[14] != 0x00 ? ReadAnalogButton(polishedPacket[26]) : 0.0f, polishedPacket[14] != 0x00 ? polishedPacket[26] : 0);
                state.SetAnalog("analog_x", polishedPacket[15] != 0x00 ? ReadAnalogButton(polishedPacket[27]) : 0.0f, polishedPacket[15] != 0x00 ? polishedPacket[27] : 0);
                state.SetAnalog("analog_square", polishedPacket[16] != 0x00 ? ReadAnalogButton(polishedPacket[28]) : 0.0f, polishedPacket[16] != 0x00 ? polishedPacket[28] : 0);

                state.SetAnalog("analog_l1", polishedPacket[11] != 0x00 ? ReadAnalogButton(polishedPacket[29]) : 0.0f, polishedPacket[11] != 0x00 ? polishedPacket[29] : 0);
                state.SetAnalog("analog_r1", polishedPacket[12] != 0x00 ? ReadAnalogButton(polishedPacket[30]) : 0.0f, polishedPacket[12] != 0x00 ? polishedPacket[30] : 0);
                state.SetAnalog("analog_l2", polishedPacket[9] != 0x00 ? ReadAnalogButton(polishedPacket[31]) : 0.0f, polishedPacket[9] != 0x00 ? polishedPacket[31] : 0);
                state.SetAnalog("analog_r2", polishedPacket[10] != 0x00 ? ReadAnalogButton(polishedPacket[32]) : 0.0f, polishedPacket[10] != 0x00 ? polishedPacket[32] : 0);
            }
            else
            {
                state.SetAnalog("analog_right", (float)(polishedPacket[6] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_left", (float)(polishedPacket[8] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_up", (float)(polishedPacket[5] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_down", (float)(polishedPacket[7] != 0x00 ? 1.0 : 0.0), 0);

                state.SetAnalog("analog_triangle", (float)(polishedPacket[13] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_circle", (float)(polishedPacket[14] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_x", (float)(polishedPacket[15] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_square", (float)(polishedPacket[16] != 0x00 ? 1.0 : 0.0), 0);

                state.SetAnalog("analog_l1", (float)(polishedPacket[11] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_r1", (float)(polishedPacket[12] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_l2", (float)(polishedPacket[9] != 0x00 ? 1.0 : 0.0), 0);
                state.SetAnalog("analog_r2", (float)(polishedPacket[10] != 0x00 ? 1.0 : 0.0), 0);
            }

            if (polishedPacket[0] == 0x12)
            {
                float x = ReadMouse(polishedPacket[10] == 0x00, polishedPacket[17]);
                float y = ReadMouse(polishedPacket[9] == 0x00, polishedPacket[18]);
                SignalTool.SetMouseProperties(x, y, polishedPacket[17], polishedPacket[18], state);
            }
            else
            {
                SignalTool.SetMouseProperties(0, 0, 0, 0, state);
            }

            return state.Build();
        }

        private static void SetButtons(string?[] buttons, byte[] polishedPacket, ControllerStateBuilder state)
        {
            for (int i = 0; i < buttons.Length; ++i)
            {
                if (string.IsNullOrEmpty(buttons[i]))
                {
                    continue;
                }

                state.SetButton(BUTTONS[i], polishedPacket[i] != 0x00);
            }
        }
    }
}
