/*  
    Copyright (c) RetroSpy Technologies
*/

using System;

namespace InputVisualizer.RetroSpy
{
    public sealed class SSHControllerReader : ISSHControllerReader, IDisposable
    {
        public event EventHandler<ControllerStateEventArgs>? ControllerStateChanged;

        public event EventHandler? ControllerDisconnected;
        public event EventHandler ControllerConnected;
        public event EventHandler<ControllerConnectionFailedArgs> ControllerConnectionFailed;

        private readonly Func<byte[]?, ControllerStateEventArgs?>? _packetParser;
        private SSHMonitor? _serialMonitor;

        public SSHControllerReader(string hostname, string arguments, Func<byte[]?, ControllerStateEventArgs?>? packetParser,
            string username, string password, string? commandSub, int delayInMilliseconds = 0, bool useQuickDisconnect = false)
        {
            _packetParser = packetParser;

            _serialMonitor = new SSHMonitor(hostname, arguments, username, password, commandSub, delayInMilliseconds, useQuickDisconnect);
            _serialMonitor.PacketReceived += SerialMonitor_PacketReceived;
            _serialMonitor.Connected += SerialMonitor_Connected;
            _serialMonitor.Disconnected += SerialMonitor_Disconnected;
            _serialMonitor.ConnectionFailed += SerialMonitor_ConnectionFailed;
        }

        public void Start()
        {
            _serialMonitor.Start();
        }

        private void SerialMonitor_Connected(object sender, EventArgs e)
        {
            ControllerConnected?.Invoke(this, e);
        }

        private void SerialMonitor_ConnectionFailed(object sender, ControllerConnectionFailedArgs e)
        {
            ControllerConnectionFailed?.Invoke(this, e);
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
                ControllerStateEventArgs? state = _packetParser != null ? _packetParser(packet.GetPacket()) : null;
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
        }
    }
}
