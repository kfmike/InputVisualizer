/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.retrospy
{
    public sealed class SerialControllerReader : IControllerReader, IDisposable
    {
        public event EventHandler<ControllerStateEventArgs>? ControllerStateChanged;

        public event EventHandler? ControllerDisconnected;

        private readonly Func<byte[]?, ControllerStateEventArgs?> _packetParser;
        private SerialMonitor? _serialMonitor;

        public SerialControllerReader(string? portName, bool useLagFix, Func<byte[]?, ControllerStateEventArgs?> packetParser)
        {
            _packetParser = packetParser;

            _serialMonitor = new SerialMonitor(portName, useLagFix);
            _serialMonitor.PacketReceived += SerialMonitor_PacketReceived;
            _serialMonitor.Disconnected += SerialMonitor_Disconnected;
            _serialMonitor.Start();
        }

        private void SerialMonitor_Disconnected(object? sender, EventArgs e)
        {
            Finish();
            ControllerDisconnected?.Invoke(this, EventArgs.Empty);
        }

        private void SerialMonitor_PacketReceived(object? sender, PacketDataEventArgs packet)
        {
            if (ControllerStateChanged != null)
            {
                ControllerStateEventArgs? state = _packetParser(packet.GetPacket());
                if (state != null)
                {
                    ControllerStateChanged(this, state);
                }
            }
        }

        public void Finish()
        {
            if (_serialMonitor != null)
            {
                _serialMonitor.Stop();
                _serialMonitor.Dispose();
                _serialMonitor = null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Finish();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}