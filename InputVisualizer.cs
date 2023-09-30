using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using InputVisualizer.retrospy;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using InputVisualizer.Config;
using InputVisualizer.UI;
using InputVisualizer.Hooks;
using InputVisualizer.Usb2Snes;
using System.Threading.Tasks;
using InputVisualizer.RetroSpyStateHandlers;
using InputVisualizer.VisualizationEngines;

namespace InputVisualizer
{
    public class InputVisualizer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private CommonTextures _commonTextures = new CommonTextures();
        private ViewerConfig _config;
        private Usb2SnesGameList _usb2SnesGameList;
        private GameUI _ui;
        private GameState _gameState;
        private Matrix _matrix = Matrix.CreateScale(2f, 2f, 2f);
        private IControllerReader _serialReader;
        private RetroSpyControllerHandler _retroSpyControllerHandler;
        private KeyboardHook _keyboardHook;
        private Usb2SnesClient _usb2snesClient;

        private RectangleEngine _rectangleEngine = new RectangleEngine();
        private Dictionary<string, SystemGamePadInfo> _systemGamePads = new Dictionary<string, SystemGamePadInfo>();

        private const int DEFAULT_SCREEN_WIDTH = 1024;
        private const int DEFAULT_SCREEN_HEIGHT = 768;
        private const string CONTENT_ROOT = "Content";
        private const string USB2SNES_GAME_LIST_PATH = @"usb2snesGameList.json";

        public InputVisualizer()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = DEFAULT_SCREEN_WIDTH;
            _graphics.PreferredBackBufferHeight = DEFAULT_SCREEN_HEIGHT;
            Content.RootDirectory = CONTENT_ROOT;
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            InactiveSleepTime = TimeSpan.Zero;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameState = new GameState();
            _keyboardHook = new KeyboardHook();
            _keyboardHook.InstallHook();
            _usb2snesClient = new Usb2SnesClient();

            InitGamepads();
            RefreshUsb2SnesDeviceList().GetAwaiter().GetResult();
            LoadConfig();
            InitUI();
            SetCurrentLayout();
            InitInputSource().GetAwaiter().GetResult();
            _gameState.ResetPurgeTimer(1.5f);
            base.Initialize();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            CloseCurrentInputMode();
            _usb2snesClient.StopUsb2SnesClient().GetAwaiter().GetResult();
            _keyboardHook?.UninstallHook();
            base.OnExiting(sender, args);
        }

        private void InitUI()
        {
            _ui = new GameUI(this, _config, _gameState);
            _ui.InputSourceChanged += UI_InputSourceChanged;
            _ui.GamepadSettingsUpdated += UI_GamepadSettingsUpdated;
            _ui.RetroSpySettingsUpdated += UI_RetroSpySettingsUpdated;
            _ui.Usb2SnesSettingsUpdated += UI_Usb2SnesSettingsUpdated;
            _ui.DisplaySettingsUpdated += UI_DisplaySettingsUpdated;
            _ui.Usb2SnesGameChanged += UI_Usb2SnesGameChanged;
            _ui.RefreshInputSources += UI_RefreshInputSources;
            _ui.Init(_systemGamePads, _usb2snesClient.Devices, _usb2SnesGameList);
        }

        private async void UI_RefreshInputSources(object sender, EventArgs e)
        {
            _ui.ShowWaitMessage("Please Wait", "Refreshing input sources...");
            CloseCurrentInputMode();
            InitGamepads();
            await RefreshUsb2SnesDeviceList();
            _ui.UpdateMainMenu(_systemGamePads, _usb2snesClient.Devices, _usb2SnesGameList);
            await InitInputSource();
            _ui.HideWaitMessage();
        }

        private void UI_Usb2SnesGameChanged(object sender, Usb2SnesGameChangedEventArgs e)
        {
            var selectedGame = _usb2SnesGameList.Games.FirstOrDefault(g => string.Equals(g.Name, e.Game, StringComparison.InvariantCultureIgnoreCase));
            var tokens = _config.CurrentInputSource.Split("||");
            if (tokens.Length < 2)
            {
                return;
            }
            _usb2snesClient.SetCurrentGame(selectedGame);
            _config.Save();
        }

        private async void UI_InputSourceChanged(object sender, InputSourceChangedEventArgs e)
        {
            await SetCurrentInputSource(e.InputSourceId);
        }

        private async void UI_GamepadSettingsUpdated(object sender, EventArgs e)
        {
            _config.Save();
            await InitInputSource();
        }

        private async void UI_RetroSpySettingsUpdated(object sender, EventArgs e)
        {
            _config.Save();
            await InitInputSource();
        }

        private async void UI_Usb2SnesSettingsUpdated(object sender, EventArgs e)
        {
            _config.Save();
            await InitInputSource();
        }

        private void UI_DisplaySettingsUpdated(object sender, EventArgs e)
        {
            _gameState.UpdateSpeed(_config.DisplayConfig.Speed);
            SetCurrentLayout();
            _config.Save();
        }

        private async Task SetCurrentInputSource(string id)
        {
            _config.CurrentInputSource = id;
            _config.Save();
            await InitInputSource();
        }

        private void InitGamepads()
        {
            _systemGamePads.Clear();
            for (var i = PlayerIndex.One; i <= PlayerIndex.Four; i++)
            {
                var state = GamePad.GetState(i);
                if (state.IsConnected)
                {
                    var caps = GamePad.GetCapabilities(i);
                    _systemGamePads.Add(caps.Identifier, new SystemGamePadInfo
                    {
                        Id = caps.Identifier,
                        Name = caps.DisplayName,
                        PlayerIndex = i
                    });
                }
            }
        }

        private async Task RefreshUsb2SnesDeviceList()
        {
            await _usb2snesClient.GetDeviceList();
            LoadUsb2SnesGameList();
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ViewerConfig.CONFIG_PATH);
            if (File.Exists(path))
            {
                var configTxt = File.ReadAllText(path);
                _config = JsonConvert.DeserializeObject<ViewerConfig>(configTxt) ?? new ViewerConfig();
            }
            else { _config = new ViewerConfig(); }

            GenerateDefaultGamepadConfigs();

            if (string.IsNullOrEmpty(_config.CurrentInputSource))
            {
                _config.CurrentInputSource = "keyboard";
            }
            _gameState.UpdateSpeed(_config.DisplayConfig.Speed);
            _config.Save();
        }

        private void LoadUsb2SnesGameList()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), USB2SNES_GAME_LIST_PATH);
            if (File.Exists(path))
            {
                var gameListTxt = File.ReadAllText(path);
                _usb2SnesGameList = JsonConvert.DeserializeObject<Usb2SnesGameList>(gameListTxt) ?? new Usb2SnesGameList();
            }
            else { _usb2SnesGameList = new Usb2SnesGameList(); }
        }

        private void GenerateDefaultGamepadConfigs()
        {
            foreach (var kvp in _systemGamePads)
            {
                var gamepadConfig = _config.GamepadConfigs.FirstOrDefault(g => g.Id == kvp.Key);
                if (gamepadConfig == null)
                {
                    gamepadConfig = _config.CreateGamepadConfig(kvp.Key, GamepadStyle.XBOX);
                }
                if (!gamepadConfig.ButtonMappingSet.ButtonMappings.Any())
                {
                    gamepadConfig.GenerateButtonMappings();
                }
            }
            var keyboardConfig = _config.GamepadConfigs.FirstOrDefault(g => g.Id == "keyboard");
            if (keyboardConfig == null)
            {
                keyboardConfig = _config.CreateGamepadConfig("keyboard", GamepadStyle.NES);
            }
            if (!keyboardConfig.ButtonMappingSet.ButtonMappings.Any())
            {
                keyboardConfig.GenerateButtonMappings();
            }
            _config.RetroSpyConfig.GenerateButtonMappings();
            _config.Usb2SnesConfig.GenerateButtonMappings();
        }

        private void SetCurrentLayout()
        {
            _rectangleEngine.UpdateContainerSettings(_config.DisplayConfig.MaxContainers, _config.DisplayConfig.EmptyContainerColor);
            switch (_config.DisplayConfig.Layout)
            {
                case DisplayLayoutStyle.Horizontal:
                    {
                        _rectangleEngine.SetOrientation(RectangeOrientation.Right);
                        
                        break;
                    }
                case DisplayLayoutStyle.VerticalDown:
                    {
                        _rectangleEngine.SetOrientation(RectangeOrientation.Down);
                        break;
                    }
                case DisplayLayoutStyle.VerticalUp:
                    {
                        _rectangleEngine.SetOrientation(RectangeOrientation.Up);
                        break;
                    }
            }
            _gameState.CurrentLayout = _rectangleEngine;
            _gameState.CurrentLayout.Clear(_gameState);
        }

        private void SetCurrentInputMode()
        {
            if (string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase))
            {
                _gameState.CurrentInputMode = InputMode.RetroSpy;
            }
            else
            {
                var tokens = _config.CurrentInputSource.Split("||");
                if (tokens.Length > 1 && string.Equals("usb2snes", tokens[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    _gameState.CurrentInputMode = InputMode.Usb2Snes;
                }
                else
                {
                    _gameState.CurrentInputMode = InputMode.Gamepad;
                }
            }
        }

        private async Task InitInputSource()
        {
            CloseCurrentInputMode();
            SetCurrentInputMode();
            switch (_gameState.CurrentInputMode)
            {
                case InputMode.RetroSpy:
                    {
                        InitRetroSpyInputSource();
                        break;
                    }
                case InputMode.Usb2Snes:
                    {
                        await InitUsb2SnesInputSource();
                        break;
                    }
                case InputMode.Gamepad:
                    {
                        InitGamepadInputSource();
                        break;
                    }
            }
            InitButtons();
        }

        private void CloseCurrentInputMode()
        {
            if (_serialReader != null)
            {
                _serialReader.Finish();
                _serialReader = null;
            }
            _usb2snesClient.StopListening();
            _gameState.CurrentInputMode = InputMode.Gamepad;
        }

        private void InitRetroSpyInputSource()
        {
            if (!string.IsNullOrEmpty(_config.RetroSpyConfig.ComPortName))
            {
                try
                {
                    switch (_config.RetroSpyConfig.ControllerType)
                    {
                        case RetroSpyControllerType.NES:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, SuperNESandNES.ReadFromPacketNES);
                                _retroSpyControllerHandler = new RetroSpyControllerHandler(_gameState);
                                break;
                            }
                        case RetroSpyControllerType.SNES:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, SuperNESandNES.ReadFromPacketSNES);
                                _retroSpyControllerHandler = new RetroSpyControllerHandler(_gameState);
                                break;
                            }
                        case RetroSpyControllerType.Genesis:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, Sega.ReadFromPacket);
                                _retroSpyControllerHandler = new RetroSpyControllerHandler(_gameState);
                                break;
                            }
                        case RetroSpyControllerType.Playstation:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, Playstation2.ReadFromPacket);
                                _retroSpyControllerHandler = new PlaystationHandler(_gameState);
                                break;
                            }
                        default:
                            {
                                _ui.ShowMessage("RetroSpy Error", "Invalid controller type selected");
                                return;
                            }
                    }

                    _serialReader.ControllerStateChanged += Reader_ControllerStateChanged;
                }
                catch (Exception ex)
                {
                    _ui.ShowMessage("RetroSpy Error", ex.Message);
                }
            }
        }

        private void Reader_ControllerStateChanged(object? reader, ControllerStateEventArgs e)
        {
            _retroSpyControllerHandler.ProcessControllerState(e);
        }

        private async Task InitUsb2SnesInputSource()
        {
            try
            {
                var tokens = _config.CurrentInputSource.Split("||");
                if (tokens.Length < 2)
                {
                    return;
                }
                if (!await _usb2snesClient.StartListening(tokens[1]))
                {
                    _ui.ShowMessage("USB2SNES Error", "Failed to attach to device.");
                }
            }
            catch (Exception ex)
            {
                _ui.ShowMessage("USB2SNES Error", ex.Message);
            }
        }

        private void InitGamepadInputSource()
        {
            if (string.Equals(_config.CurrentInputSource, "keyboard", StringComparison.InvariantCultureIgnoreCase))
            {
                _gameState.ActiveGamepadConfig = _config.GamepadConfigs.FirstOrDefault(c => c.IsKeyboard);
                return;
            }

            if (string.IsNullOrEmpty(_config.CurrentInputSource) || !_systemGamePads.Keys.Contains(_config.CurrentInputSource))
            {
                if (_config.GamepadConfigs.Any())
                {
                    foreach (var gamepadConfig in _config.GamepadConfigs)
                    {
                        if (_systemGamePads.Keys.Contains(gamepadConfig.Id))
                        {
                            _config.CurrentInputSource = gamepadConfig.Id;
                            _gameState.ActiveGamepadConfig = gamepadConfig;
                            break;
                        }
                    }
                }
            }
            else if (_systemGamePads.Keys.Contains(_config.CurrentInputSource))
            {
                _gameState.ActiveGamepadConfig = _config.GamepadConfigs.First(c => c.Id == _config.CurrentInputSource);
            }
            if (_gameState.ActiveGamepadConfig != null)
            {
                _gameState.CurrentPlayerIndex = _systemGamePads[_gameState.ActiveGamepadConfig.Id].PlayerIndex;
            }
        }

        private void InitButtons()
        {
            switch (_gameState.CurrentInputMode)
            {
                case InputMode.RetroSpy:
                    {
                        InitRetroSpyButtons();
                        break;
                    }
                case InputMode.Gamepad:
                    {
                        InitGamepadButtons();
                        break;
                    }
                case InputMode.Usb2Snes:
                    {
                        InitUsb2SnesButtons();
                        break;
                    }
            }

            _gameState.CurrentLayout?.Clear(_gameState);
            _gameState.FrequencyDict.Clear();

            foreach (var button in _gameState.ButtonStates)
            {
                _gameState.FrequencyDict.Add(button.Key, 0);
            }
        }

        private void InitRetroSpyButtons()
        {
            _gameState.ButtonStates.Clear();

            List<ButtonMapping> buttonMappings = null;
            switch (_config.RetroSpyConfig.ControllerType)
            {
                case RetroSpyControllerType.NES:
                    {
                        buttonMappings = _config.RetroSpyConfig.NES.ButtonMappings;
                        break;
                    }
                case RetroSpyControllerType.SNES:
                    {
                        buttonMappings = _config.RetroSpyConfig.SNES.ButtonMappings;
                        break;
                    }
                case RetroSpyControllerType.Genesis:
                    {
                        buttonMappings = _config.RetroSpyConfig.Genesis.ButtonMappings;
                        break;
                    }
                case RetroSpyControllerType.Playstation:
                    {
                        buttonMappings = _config.RetroSpyConfig.Playstation.ButtonMappings;
                        break;
                    }
            }

            if (buttonMappings == null)
            {
                return;
            }
            foreach (var mapping in buttonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
            {
                _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
            }
        }

        private void InitUsb2SnesButtons()
        {
            _gameState.ButtonStates.Clear();
            foreach (var mapping in _config.Usb2SnesConfig.ButtonMappingSet.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
            {
                _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
            }
        }

        private void InitGamepadButtons()
        {
            _gameState.ButtonStates.Clear();
            if (_gameState.ActiveGamepadConfig == null)
            {
                return;
            }
            foreach (var mapping in _gameState.ActiveGamepadConfig.ButtonMappingSet.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
            {
                var dictKey = string.Empty;

                if (mapping.MappingType == ButtonMappingType.Button && mapping.MappedButtonType != ButtonType.NONE)
                {
                    dictKey = mapping.MappedButtonType.ToString();
                }
                else if (mapping.MappingType == ButtonMappingType.Key && mapping.MappedKey != Keys.None)
                {
                    dictKey = mapping.MappedKey.ToString();
                }
                if (string.IsNullOrEmpty(dictKey))
                {
                    continue;
                }
                _gameState.ButtonStates.Add(dictKey, new ButtonStateHistory()
                {
                    Color = mapping.Color,
                    UnmappedButtonType = mapping.ButtonType,
                    MappingType = mapping.MappingType,
                    MappedKey = mapping.MappedKey
                });
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _commonTextures.Init(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_ui.ListeningForInput)
            {
                _ui.CheckForListeningInput();
            }
            else
            {
                if (_gameState.CurrentInputMode == InputMode.Gamepad)
                {
                    ReadGamepadInputs();
                }
                else if (_gameState.CurrentInputMode == InputMode.Usb2Snes)
                {
                    ReadUsb2SnesInputs();
                }

                foreach (var button in _gameState.ButtonStates)
                {
                    _gameState.FrequencyDict[button.Key] = button.Value.GetPressedLastSecond();
                }

                _gameState.UpdateMinAge(_config.DisplayConfig.LineLength);
                _gameState.CurrentLayout?.Update(_config, _gameState, gameTime);
            }
            base.Update(gameTime);
        }

        private void ReadUsb2SnesInputs()
        {
            var timeStamp = DateTime.Now;
            foreach (var button in _gameState.ButtonStates)
            {
                var pressed = false;

                switch (button.Key)
                {
                    case "UP":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Up];
                            break;
                        }
                    case "DOWN":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Down];
                            break;
                        }
                    case "LEFT":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Left];
                            break;
                        }
                    case "RIGHT":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Right];
                            break;
                        }
                    case "SELECT":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Select];
                            break;
                        }
                    case "START":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Start];
                            break;
                        }
                    case "A":
                        {
                            pressed = _usb2snesClient.ButtonStates1[Usb2SnesButtonFlags1.A];
                            break;
                        }
                    case "B":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.B];
                            break;
                        }
                    case "X":
                        {
                            pressed = _usb2snesClient.ButtonStates1[Usb2SnesButtonFlags1.X];
                            break;
                        }
                    case "Y":
                        {
                            pressed = _usb2snesClient.ButtonStates2[Usb2SnesButtonFlags2.Y];
                            break;
                        }
                    case "L":
                        {
                            pressed = _usb2snesClient.ButtonStates1[Usb2SnesButtonFlags1.L];
                            break;
                        }
                    case "R":
                        {
                            pressed = _usb2snesClient.ButtonStates1[Usb2SnesButtonFlags1.R];
                            break;
                        }
                }
                if (button.Value.IsPressed() != pressed)
                {
                    _gameState.ButtonStates[button.Key].AddStateChange(pressed, timeStamp);
                }
            }
        }

        private void ReadGamepadInputs()
        {
            var keyboardState = Keyboard.GetState();
            var state = GamePad.GetState(_gameState.CurrentPlayerIndex);
            var analogDpadState = InputHelper.GetAnalogDpadMovement(state, _gameState.AnalogStickDeadZoneTolerance);
            var timeStamp = DateTime.Now;
            var gamepad = _gameState.ActiveGamepadConfig;

            foreach (var button in _gameState.ButtonStates)
            {
                bool pressed = false;

                if (button.Value.MappingType == ButtonMappingType.Key)
                {
                    if (IsActive)
                    {
                        pressed = keyboardState.IsKeyDown(button.Value.MappedKey);
                        if (button.Value.IsPressed() != pressed)
                        {
                            _gameState.ButtonStates[button.Key].AddStateChange(pressed, timeStamp);
                        }
                    }
                    else
                    {
                        pressed = KeyboardHook.KeyboardState[button.Value.MappedKey];
                        if (button.Value.IsPressed() != pressed)
                        {
                            _gameState.ButtonStates[button.Key].AddStateChange(pressed, timeStamp);
                        }
                    }
                    continue;
                }

                switch (button.Key)
                {
                    case "UP":
                        {
                            pressed = gamepad.UseLStickForDpad ? analogDpadState.UpDown == ButtonType.UP : state.DPad.Up == ButtonState.Pressed;
                            break;
                        }
                    case "DOWN":
                        {
                            pressed = gamepad.UseLStickForDpad ? analogDpadState.UpDown == ButtonType.DOWN : state.DPad.Down == ButtonState.Pressed;
                            break;
                        }
                    case "LEFT":
                        {
                            pressed = gamepad.UseLStickForDpad ? analogDpadState.LeftRight == ButtonType.LEFT : state.DPad.Left == ButtonState.Pressed;
                            break;
                        }
                    case "RIGHT":
                        {
                            pressed = gamepad.UseLStickForDpad ? analogDpadState.LeftRight == ButtonType.RIGHT : state.DPad.Right == ButtonState.Pressed;
                            break;
                        }
                    case "SELECT":
                        {
                            pressed = state.Buttons.Back == ButtonState.Pressed;
                            break;
                        }
                    case "START":
                        {
                            pressed = state.Buttons.Start == ButtonState.Pressed;
                            break;
                        }
                    case "A":
                    case "CROSS":
                        {
                            pressed = state.Buttons.A == ButtonState.Pressed;
                            break;
                        }
                    case "B":
                    case "CIRCLE":
                        {
                            pressed = state.Buttons.B == ButtonState.Pressed;
                            break;
                        }
                    case "X":
                    case "SQUARE":
                        {
                            pressed = state.Buttons.X == ButtonState.Pressed;
                            break;
                        }
                    case "Y":
                    case "TRIANGLE":
                        {
                            pressed = state.Buttons.Y == ButtonState.Pressed;
                            break;
                        }
                    case "L":
                    case "L1":
                        {
                            pressed = state.Buttons.LeftShoulder == ButtonState.Pressed;
                            break;
                        }
                    case "R":
                    case "R1":
                        {
                            pressed = state.Buttons.RightShoulder == ButtonState.Pressed;
                            break;
                        }
                    case "LT":
                    case "L2":
                        {
                            pressed = state.Triggers.Left > 0.0f;
                            break;
                        }
                    case "RT":
                    case "R2":
                        {
                            pressed = state.Triggers.Right > 0.0f;
                            break;
                        }
                }

                if (button.Value.IsPressed() != pressed)
                {
                    _gameState.ButtonStates[button.Key].AddStateChange(pressed, timeStamp);
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_config.DisplayConfig.BackgroundColor);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _matrix);
            _gameState.CurrentLayout?.Draw(_spriteBatch, _config, _gameState, gameTime, _commonTextures);
            _spriteBatch.End();
            _ui.Render();
            base.Draw(gameTime);
        }
    }
}