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
using InputVisualizer.Layouts;

namespace InputVisualizer
{
    public class InputVisualizer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private CommonTextures _commonTextures = new CommonTextures();
        private ViewerConfig _config;
        private GameUI _ui;
        private GameState _gameState;
        private Matrix _matrix = Matrix.CreateScale(2f, 2f, 2f);
        private IControllerReader _serialReader;

        private HorizontalRectangleEngine _horizontalRectangleLayout = new HorizontalRectangleEngine();
        private VerticalRectangleEngine _verticalRectangleLayout = new VerticalRectangleEngine();
        private Dictionary<string, SystemGamePadInfo> _systemGamePads = new Dictionary<string, SystemGamePadInfo>();

        private const int DEFAULT_SCREEN_WIDTH = 1024;
        private const int DEFAULT_SCREEN_HEIGHT = 768;
        private const string CONTENT_ROOT = "Content";

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

            InitGamepads();
            LoadConfig();
            InitUI();
            SetCurrentLayout();
            InitInputSource();
            _gameState.ResetPurgeTimer(_config.DisplayConfig.TurnOffLineSpeed);
            base.Initialize();
        }

        private void InitUI()
        {
            _ui = new GameUI(this, _config, _gameState);
            _ui.InputSourceChanged += UI_InputSourceChanged;
            _ui.GamepadSettingsUpdated += UI_GamepadSettingsUpdated;
            _ui.RetroSpySettingsUpdated += UI_RetroSpySettingsUpdated;
            _ui.DisplaySettingsUpdated += UI_DisplaySettingsUpdated;
            _ui.Init(_systemGamePads);
        }

        private void UI_InputSourceChanged(object sender, InputSourceChangedEventArgs e)
        {
            SetCurrentInputSource(e.InputSourceId);
        }

        private void UI_GamepadSettingsUpdated(object sender, EventArgs e)
        {
            _config.Save();
            InitInputSource();
        }

        private void UI_RetroSpySettingsUpdated(object sender, EventArgs e)
        {
            _config.Save();
            InitInputSource();
        }

        private void UI_DisplaySettingsUpdated(object sender, EventArgs e)
        {
            _gameState.UpdateSpeed(_config.DisplayConfig.Speed);
            _gameState.ResetPurgeTimer(_config.DisplayConfig.TurnOffLineSpeed);
            SetCurrentLayout();
            _config.Save();
        }

        private void SetCurrentInputSource(string id)
        {
            _config.CurrentInputSource = id;
            _config.Save();
            InitInputSource();
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

            if (string.IsNullOrEmpty(_config.CurrentInputSource) || !_systemGamePads.Any())
            {
                _config.CurrentInputSource = "spy";
            }
            _gameState.UpdateSpeed(_config.DisplayConfig.Speed);
            _config.Save();
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
            _config.RetroSpyConfig.GenerateButtonMappings();
        }

        private void SetCurrentLayout()
        {
            switch (_config.DisplayConfig.Layout)
            {
                case DisplayLayoutStyle.Horizontal:
                    {
                        _gameState.CurrentLayout = _horizontalRectangleLayout;
                        break;
                    }
                case DisplayLayoutStyle.Vertical:
                    {
                        _gameState.CurrentLayout = _verticalRectangleLayout;
                        break;
                    }
            }
            _gameState.CurrentLayout.Clear(_gameState);
        }

        private void InitInputSource()
        {
            _gameState.CurrentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) || !_systemGamePads.Any() ? InputMode.RetroSpy : InputMode.Gamepad;
            if (_gameState.CurrentInputMode == InputMode.RetroSpy)
            {
                InitRetroSpyInputSource();
            }
            else if (_gameState.CurrentInputMode == InputMode.Gamepad)
            {
                InitGamepadInputSource();
            }
            InitButtons();
        }

        private void InitRetroSpyInputSource()
        {
            if (!string.IsNullOrEmpty(_config.RetroSpyConfig.ComPortName))
            {
                if (_serialReader != null)
                {
                    _serialReader.Finish();
                }
                try
                {
                    switch (_config.RetroSpyConfig.ControllerType)
                    {
                        case RetroSpyControllerType.NES:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, SuperNESandNES.ReadFromPacketNES);
                                break;
                            }
                        case RetroSpyControllerType.SNES:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, SuperNESandNES.ReadFromPacketSNES);
                                break;
                            }
                        case RetroSpyControllerType.GENESIS:
                            {
                                _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, Sega.ReadFromPacket);
                                break;
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

        private void InitGamepadInputSource()
        {
            if (_serialReader != null)
            {
                _serialReader.Finish();
                _serialReader = null;
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

        private void Reader_ControllerStateChanged(object? reader, ControllerStateEventArgs e)
        {
            foreach (var button in e.Buttons)
            {
                if (_gameState.ButtonStates.ContainsKey(button.Key))
                {
                    if (_gameState.ButtonStates[button.Key].IsPressed() != button.Value)
                    {
                        _gameState.ButtonStates[button.Key].AddStateChange(button.Value, DateTime.Now);
                    }
                }
            }
        }

        private void InitButtons()
        {
            switch (_gameState.CurrentInputMode)
            {
                case InputMode.RetroSpy:
                    {
                        InitRetroSpyNESButtons();
                        break;
                    }
                case InputMode.Gamepad:
                    {
                        InitGamepadButtons();
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

        private void InitRetroSpyNESButtons()
        {
            _gameState.ButtonStates.Clear();

            switch (_config.RetroSpyConfig.ControllerType)
            {
                case RetroSpyControllerType.NES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.NES.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
                        }
                        break;
                    }
                case RetroSpyControllerType.SNES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.SNES.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
                        }
                        break;
                    }
                case RetroSpyControllerType.GENESIS:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.GENESIS.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
                        }
                        break;
                    }
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
                _gameState.ButtonStates.Add(mapping.MappedButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, UnmappedButtonType = mapping.ButtonType });
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

                foreach (var button in _gameState.ButtonStates)
                {
                    _gameState.FrequencyDict[button.Key] = button.Value.GetPressedLastSecond();
                }

                _gameState.UpdateMinAge(_config.DisplayConfig.LineLength);
                _gameState.CurrentLayout?.Update(_config, _gameState, gameTime);
            }
            base.Update(gameTime);
        }

        private void ReadGamepadInputs()
        {
            var state = GamePad.GetState(_gameState.CurrentPlayerIndex);
            var analogDpadState = InputHelper.GetAnalogDpadMovement(state, _gameState.AnalogStickDeadZoneTolerance);
            var timeStamp = DateTime.Now;
            foreach (var button in _gameState.ButtonStates)
            {
                bool pressed = false;
                switch (button.Key)
                {
                    case "UP":
                        {
                            pressed = _gameState.ActiveGamepadConfig.UseLStickForDpad ? analogDpadState.UpDown == ButtonType.UP : state.DPad.Up == ButtonState.Pressed;
                            break;
                        }
                    case "DOWN":
                        {
                            pressed = _gameState.ActiveGamepadConfig.UseLStickForDpad ? analogDpadState.UpDown == ButtonType.DOWN : state.DPad.Down == ButtonState.Pressed;
                            break;
                        }
                    case "LEFT":
                        {
                            pressed = _gameState.ActiveGamepadConfig.UseLStickForDpad ? analogDpadState.LeftRight == ButtonType.LEFT : state.DPad.Left == ButtonState.Pressed;
                            break;
                        }
                    case "RIGHT":
                        {
                            pressed = _gameState.ActiveGamepadConfig.UseLStickForDpad ? analogDpadState.LeftRight == ButtonType.RIGHT : state.DPad.Right == ButtonState.Pressed;
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
                        {
                            pressed = state.Buttons.A == ButtonState.Pressed;
                            break;
                        }
                    case "B":
                        {
                            pressed = state.Buttons.B == ButtonState.Pressed;
                            break;
                        }
                    case "X":
                        {
                            pressed = state.Buttons.X == ButtonState.Pressed;
                            break;
                        }
                    case "Y":
                        {
                            pressed = state.Buttons.Y == ButtonState.Pressed;
                            break;
                        }
                    case "L":
                        {
                            pressed = state.Buttons.LeftShoulder == ButtonState.Pressed;
                            break;
                        }
                    case "R":
                        {
                            pressed = state.Buttons.RightShoulder == ButtonState.Pressed;
                            break;
                        }
                    case "LT":
                        {
                            pressed = state.Triggers.Left > 0.0f;
                            break;
                        }
                    case "RT":
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