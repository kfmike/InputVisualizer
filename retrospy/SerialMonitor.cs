using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace InputVisualizer.retrospy
{
    public class PacketDataEventArgs : EventArgs
    {
        public PacketDataEventArgs(byte[] packet)
        {
            Packet = packet;
        }
        public byte[]? GetPacket() { return Packet; }

        private readonly byte[] Packet;
    }

    //public delegate void PacketEventHandler(object sender, PacketDataEventArgs e);

    public class SerialMonitor : IDisposable
    {
        private const int BAUD_RATE = 115200;
        private const int TIMER_MS = 1;

        public event EventHandler<PacketDataEventArgs>? PacketReceived;

        public event EventHandler? Disconnected;

        private SerialPort? _datPort;
        private readonly List<byte> _localBuffer;
        private Timer? _timer;

        public SerialMonitor(string? portName, bool useLagFix, bool printerMode = false)
        {
            _localBuffer = new List<byte>();
            _datPort = new SerialPort(portName != null ? portName.Split(' ')[0] : "", useLagFix ? 57600 : BAUD_RATE)
            {
                Handshake = Handshake.RequestToSend, // Improves support for devices expecting RTS & DTR signals.
                DtrEnable = true
            };
        }

        public void Start()
        {
            if (_timer != null)
            {
                return;
            }

            _localBuffer.Clear();
            _datPort?.Open();

            _timer = new Timer(Tick, null, 0, TIMER_MS);

        }

        public void Stop()
        {
            if (_datPort != null)
            {
                try
                { // If the device has been unplugged, Close will throw an IOException.  This is fine, we'll just keep cleaning up.
                    _datPort.Close();
                }
                catch (IOException) { }
                _datPort.Dispose();
                _datPort = null;
            }
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void Tick(Object stateInfo)
        {
            lock (this)
            {
                if (_datPort == null || !_datPort.IsOpen || PacketReceived == null)
                {
                    return;
                }

                // Try to read some data from the COM port and append it to our localBuffer.
                // If there's an IOException then the device has been disconnected.
                try
                {
                    int readCount = _datPort.BytesToRead;
                    byte[] readBuffer = new byte[readCount];
                    _ = _datPort.Read(readBuffer, 0, readCount);
                    //_datPort.DiscardInBuffer();
                    _localBuffer.AddRange(readBuffer);
                }
                catch (IOException)
                {
                    Stop();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    return;
                }
                catch (OverflowException)  // Linux throws this when the printer emulator is unplugged ???
                {
                    Stop();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    return;
                }

                // Try and find 2 splitting characters in our buffer.
                int lastSplitIndex = _localBuffer.LastIndexOf(0x0A);
                if (lastSplitIndex <= 1)
                {
                    return;
                }

                int sndLastSplitIndex = _localBuffer.LastIndexOf(0x0A, lastSplitIndex - 1);
                if (lastSplitIndex == -1)
                {
                    return;
                }

                // Grab the latest packet out of the buffer and fire it off to the receive event listeners.
                int packetStart = sndLastSplitIndex + 1;
                int packetSize = lastSplitIndex - packetStart;

                PacketReceived(this, new PacketDataEventArgs(_localBuffer.GetRange(packetStart, packetSize).ToArray()));

                // Clear our buffer up until the last split character.
                _localBuffer.RemoveRange(0, lastSplitIndex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
