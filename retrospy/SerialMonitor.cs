/*  
    Copyright (c) RetroSpy Technologies
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Timers;

namespace InputVisualizer.RetroSpy
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
                Handshake = Handshake.RequestToSend,
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

            _timer = new Timer(TIMER_MS);
            _timer.Elapsed += Tick;
            _timer.AutoReset = true;
            _timer.Start();
        }

        public void Stop()
        {
            if (_datPort != null)
            {
                try
                {
                    _datPort.Close();
                }
                catch (IOException) { }
                _datPort.Dispose();
                _datPort = null;
            }
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if (_datPort == null || !_datPort.IsOpen || PacketReceived == null)
            {
                return;
            }

            try
            {
                int readCount = _datPort.BytesToRead;
                byte[] readBuffer = new byte[readCount];
                _ = _datPort.Read(readBuffer, 0, readCount);
                _localBuffer.AddRange(readBuffer);
            }
            catch (IOException)
            {
                Stop();
                Disconnected?.Invoke(this, EventArgs.Empty);
                return;
            }
            catch (OverflowException)
            {
                Stop();
                Disconnected?.Invoke(this, EventArgs.Empty);
                return;
            }

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

            int packetStart = sndLastSplitIndex + 1;
            int packetSize = lastSplitIndex - packetStart;

            PacketReceived(this, new PacketDataEventArgs(_localBuffer.GetRange(packetStart, packetSize).ToArray()));

            _localBuffer.RemoveRange(0, lastSplitIndex);
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
