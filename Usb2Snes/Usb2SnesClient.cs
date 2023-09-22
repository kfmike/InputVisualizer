using InputVisualizer.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace InputVisualizer.Usb2Snes
{
    public class Usb2SnesClient
    {
        private const string SERVER_URL = "ws://localhost:8080";

        private const string SNES_SPACE = "SNES";

        private const string OPCODE_DEVICE_LIST = "DeviceList";
        private const string OPCODE_ATTACH = "Attach";
        private const string OPCODE_INFO = "Info";
        private const string OPCODE_GETADDRESS = "GetAddress";
        private const string DEFAULT_ADDRESS = "F90718";
        private const int RESPONSE_BUFFER_CHUNK = 1024;
        private const int INPUT_TIMER_MS = 1;
        private const int ROM_TIMER_MS = 2000;

        ClientWebSocket _socket = null;
        private List<string> _deviceList = new List<string>();
        private Usb2SnesGame _selectedGame;
        public List<string> Devices => _deviceList;
        private System.Timers.Timer _inputTimer;
        private Usb2SnesState _state = Usb2SnesState.Idle;
        private string _currentDevice = null;
        private System.Timers.Timer _restartListenTimer;

        public Dictionary<Usb2SnesButtonFlags1, bool> ButtonStates1 = new Dictionary<Usb2SnesButtonFlags1, bool>();
        public Dictionary<Usb2SnesButtonFlags2, bool> ButtonStates2 = new Dictionary<Usb2SnesButtonFlags2, bool>();

        public Usb2SnesClient()
        {
            _selectedGame = CreateDefaultGame();
            _inputTimer = new System.Timers.Timer();
            _inputTimer.Elapsed += InputTimerElapsed;
            _inputTimer.Interval = INPUT_TIMER_MS;
            _inputTimer.AutoReset = false;
            _inputTimer.Enabled = false;

            _restartListenTimer = new System.Timers.Timer(ROM_TIMER_MS);
            _restartListenTimer.Elapsed += RestartListenElapsed;
            _restartListenTimer.AutoReset = false;
            _restartListenTimer.Enabled = false;

            foreach (var flag in Enum.GetValues(typeof(Usb2SnesButtonFlags2)))
            {
                ButtonStates2.Add((Usb2SnesButtonFlags2)flag, false);
            }
            foreach (var flag in Enum.GetValues(typeof(Usb2SnesButtonFlags1)))
            {
                ButtonStates1.Add((Usb2SnesButtonFlags1)flag, false);
            }
        }

        private Usb2SnesGame CreateDefaultGame()
        {
            return new Usb2SnesGame { Name = "Default Game", Address = new string[] { DEFAULT_ADDRESS } };
        }

        private async void RestartListenElapsed(object sender, ElapsedEventArgs e)
        {
            if (_state != Usb2SnesState.ListeningError)
            {
                return;
            }

            if (await StartListening(_currentDevice))
            {
                return;
            }
            _restartListenTimer.Start();
        }

        private void RestartListener()
        {
            _state = Usb2SnesState.ListeningError;
            _restartListenTimer.Start();
        }

        public void SetCurrentGame(Usb2SnesGame game)
        {
            _selectedGame = game ?? CreateDefaultGame();
        }

        public async Task StopUsb2SnesClient()
        {
            try
            {
                StopListening();
                await Disconnect();
            }
            catch { }
        }

        public async Task<List<string>> GetDeviceList()
        {
            _deviceList.Clear();
            try
            {
                if (!await Connect())
                {
                    return _deviceList;
                }

                await SendRequest(OPCODE_DEVICE_LIST, SNES_SPACE, CreateCancellationToken());
                var response = await GetResponse();

                if (response != null)
                {
                    _deviceList.AddRange(response.Results);
                }
            }
            catch
            { }
            return _deviceList;
        }

        public async Task<bool> StartListening(string deviceName)
        {
            _currentDevice = deviceName;

            if (!await Connect())
            {
                return false;
            }

            if (!await SendRequest(OPCODE_ATTACH, SNES_SPACE, CancellationToken.None, new string[] { _currentDevice }))
            {
                return false;
            }

            if (!await SendRequest(OPCODE_INFO, SNES_SPACE, CancellationToken.None))
            {
                return false;
            }
            var infoResponse = await GetResponse();

            if (infoResponse == null)
            {
                return false;
            }

            _state = Usb2SnesState.Listening;
            _inputTimer.Start();

            return true;
        }

        public void StopListening()
        {
            _state = Usb2SnesState.Idle;
            _inputTimer.Stop();
            _restartListenTimer.Stop();
        }

        private async void InputTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (_state != Usb2SnesState.Listening)
                {
                    return;
                }

                if (_socket.State != WebSocketState.Open && !string.IsNullOrEmpty(_currentDevice))
                {
                    if (!await StartListening(_currentDevice))
                    {
                        return;
                    }
                }
                byte[] inputData = new byte[2];

                if (_selectedGame.Address.Length == 1)
                {
                    if (!await SendRequest(OPCODE_GETADDRESS, SNES_SPACE, CancellationToken.None, new string[] { _selectedGame.Address[0], "2" }))
                    {
                        return;
                    }
                    await GetBinaryResponse(inputData);
                }
                else
                {
                    var oneByteBuffer = new byte[1];
                    if (!await SendRequest(OPCODE_GETADDRESS, SNES_SPACE, CancellationToken.None, new string[] { _selectedGame.Address[0], "1" }))
                    {
                        return;
                    }
                    await GetBinaryResponse(oneByteBuffer);
                    inputData[0] = oneByteBuffer[0];

                    if (!await SendRequest(OPCODE_GETADDRESS, SNES_SPACE, CancellationToken.None, new string[] { _selectedGame.Address[1], "1" }))
                    {
                        return;
                    }

                    await GetBinaryResponse(oneByteBuffer);
                    inputData[1] = oneByteBuffer[0];
                }

                var flags1 = (Usb2SnesButtonFlags1)inputData[0];
                var flags2 = (Usb2SnesButtonFlags2)inputData[1];

                foreach (var state in ButtonStates1)
                {
                    ButtonStates1[state.Key] = (flags1 & state.Key) != Usb2SnesButtonFlags1.None;
                }
                foreach (var state in ButtonStates2)
                {
                    ButtonStates2[state.Key] = (flags2 & state.Key) != Usb2SnesButtonFlags2.None;
                }
            }
            catch
            {
                RestartListener();
            }
            finally
            {
                _inputTimer.Start();
            }
        }

        private async Task<bool> Connect()
        {
            if (_socket?.State == WebSocketState.Connecting)
            {
                return false;
            }
            if (_socket?.State == WebSocketState.Open)
            {
                return true;
            }

            _socket?.Dispose();
            _socket = new ClientWebSocket();

            try
            {
                await _socket.ConnectAsync(new Uri(SERVER_URL), CreateCancellationToken());
                return _socket.State == WebSocketState.Open;
            }
            catch
            {
                if (_state == Usb2SnesState.Listening)
                {
                    RestartListener();
                }
                return false;
            }
        }

        private async Task Disconnect()
        {
            if (_socket?.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed by client", CreateCancellationToken());
            }
            _socket?.Dispose();
            _socket = null;
        }

        private async Task<bool> SendRequest(string opCode, string space, CancellationToken cancellationToken, string[] operands = null)
        {
            try
            {
                if (!await Connect())
                {
                    return false;
                }
                var command = new Usb2SnesRequest { Opcode = opCode, Space = space, Operands = operands ?? Array.Empty<string>() };
                var request = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command));
                await _socket.SendAsync(new ArraySegment<byte>(request), WebSocketMessageType.Text, true, CreateCancellationToken());
                return _socket?.State == WebSocketState.Open;
            }
            catch
            {
                if (_state == Usb2SnesState.Listening)
                {
                    RestartListener();
                }
                return false;
            }
        }

        private async Task<Usb2SnesResponse> GetResponse()
        {
            try
            {
                if (_socket?.State != WebSocketState.Open)
                {
                    return null;
                }
                var buffer = new byte[RESPONSE_BUFFER_CHUNK];
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CreateCancellationToken());
                return JsonConvert.DeserializeObject<Usb2SnesResponse>(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            catch
            {
                if (_state == Usb2SnesState.Listening)
                {
                    RestartListener();
                }
                return null;
            }
        }

        private async Task GetBinaryResponse(byte[] buffer)
        {
            if (_socket?.State == WebSocketState.Open)
            {
                await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }

        private CancellationToken CreateCancellationToken(double seconds = 1.0)
        {
            var source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromSeconds(seconds));
            return source.Token;
        }
    }
}
