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

        private HorizontalRectangleLayout _horizontalRectangleLayout = new HorizontalRectangleLayout();
        private VerticalRectangleLayout _verticalRectangleLayout = new VerticalRectangleLayout();
        private TimeSpan _purgeTimer = TimeSpan.Zero;
        private Dictionary<string, GamePadInfo> _systemGamePads = new Dictionary<string, GamePadInfo>();

        public InputVisualizer()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            Content.RootDirectory = "Content";
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
            InitInputSource();
            InitViewer();

            _ui = new GameUI(this, _config, _gameState);
            _ui.InputSourceChanged += UI_InputSourceChanged;
            _ui.GamepadSettingsUpdated += UI_GamepadSettingsUpdated;
            _ui.RetroSpySettingsUpdated += UI_RetroSpySettingsUpdated;
            _ui.DisplaySettingsUpdated += UI_DisplaySettingsUpdated;
            _ui.Init(_systemGamePads);

            base.Initialize();
        }

        private void UI_InputSourceChanged(object sender, InputSourceChangedEventArgs e)
        {
            SetCurrentInputSource(e.InputSourceId);
        }

        private void UI_GamepadSettingsUpdated(object sender, EventArgs e)
        {
            SaveConfig();
            InitInputSource();
        }

        private void UI_RetroSpySettingsUpdated(object sender, EventArgs e)
        {
            SaveConfig();
            InitInputSource();
        }

        private void UI_DisplaySettingsUpdated(object sender, EventArgs e)
        {
            UpdateSpeed();
            SetCurrentLayout();
            SaveConfig();
        }

        private void SetCurrentInputSource(string id)
        {
            _config.CurrentInputSource = id;
            SaveConfig();
            _gameState.CurrentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) ? InputMode.RetroSpy : InputMode.Gamepad;
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
                    _systemGamePads.Add(caps.Identifier, new GamePadInfo
                    {
                        Id = caps.Identifier,
                        Name = caps.DisplayName,
                        PlayerIndex = i
                    });
                }
            }
        }

        private void InitViewer()
        {
            CalcMinAge();
        }

        private void UpdateSpeed()
        {
            _gameState.PixelsPerMs = 0.05f * _config.DisplayConfig.Speed;
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.json");
            if (File.Exists(path))
            {
                var configTxt = File.ReadAllText(path);
                _config = JsonConvert.DeserializeObject<ViewerConfig>(configTxt) ?? new ViewerConfig();
            }
            else { _config = new ViewerConfig(); }

            foreach (var kvp in _systemGamePads)
            {
                var gamepadConfig = _config.GamepadConfigs.FirstOrDefault(g => g.Id == kvp.Key);
                if (gamepadConfig == null)
                {
                    gamepadConfig = _config.CreateGamepadConfig(kvp.Key, GamepadStyle.XBOX);
                }
                if (!gamepadConfig.ButtonMappings.Any())
                {
                    gamepadConfig.GenerateButtonMappings();
                }
            }

            UpdateSpeed();
            _config.RetroSpyConfig.GenerateButtonMappings();

            if (string.IsNullOrEmpty(_config.CurrentInputSource) || !_systemGamePads.Any())
            {
                _config.CurrentInputSource = "spy";
            }

            SaveConfig();
            _gameState.CurrentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) || !_systemGamePads.Any() ? InputMode.RetroSpy : InputMode.Gamepad;
            SetCurrentLayout();
        }

        private void SetCurrentLayout()
        {
            switch (_config.DisplayConfig.Layout)
            {
                case LayoutStyle.Horizontal:
                    {
                        _gameState.CurrentLayout = _horizontalRectangleLayout;
                        break;
                    }
                case LayoutStyle.Vertical:
                    {
                        _gameState.CurrentLayout = _verticalRectangleLayout;
                        break;
                    }
            }
            _gameState.CurrentLayout.Clear(_gameState);
        }

        private void SaveConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        private void InitInputSource()
        {
            if (_gameState.CurrentInputMode == InputMode.RetroSpy)
            {
                if (!string.IsNullOrEmpty(_config.RetroSpyConfig.ComPortName))
                {
                    if (_serialReader != null)
                    {
                        _serialReader.Finish();
                    }
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
            }
            else if (_gameState.CurrentInputMode == InputMode.Gamepad)
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
            InitButtons();
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

            _gameState.CurrentLayout.Clear(_gameState);

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
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label, UnmappedButtonType = mapping.ButtonType });
                        }
                        break;
                    }
                case RetroSpyControllerType.SNES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.SNES.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label, UnmappedButtonType = mapping.ButtonType });
                        }
                        break;
                    }
                case RetroSpyControllerType.GENESIS:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.GENESIS.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _gameState.ButtonStates.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label, UnmappedButtonType = mapping.ButtonType });
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
            foreach (var mapping in _gameState.ActiveGamepadConfig.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
            {
                _gameState.ButtonStates.Add(mapping.MappedButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label, UnmappedButtonType = mapping.ButtonType });
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

                var lineMs = CalcMinAge();
                _gameState.MinAge = DateTime.Now.AddMilliseconds(-lineMs);
                _purgeTimer += gameTime.ElapsedGameTime;
                if (_purgeTimer.Milliseconds > 500)
                {
                    foreach (var button in _gameState.ButtonStates.Values)
                    {
                        button.RemoveOldStateChanges(lineMs + _config.DisplayConfig.TurnOffLineSpeed + 500);
                    }
                    _purgeTimer = TimeSpan.Zero;
                }

                _gameState.CurrentLayout.Update(_config, _gameState, gameTime);
            }

            base.Update(gameTime);
        }

        private float CalcMinAge()
        {
            var lineMs = _config.DisplayConfig.LineLength / _gameState.PixelsPerMs;
            _gameState.MinAge = DateTime.Now.AddMilliseconds(-lineMs);
            return lineMs;
        }

        private void ReadGamepadInputs()
        {
            foreach (var button in _gameState.ButtonStates)
            {
                switch (button.Key)
                {
                    case "UP":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).DPad.Up == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "DOWN":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).DPad.Down == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "LEFT":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).DPad.Left == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "RIGHT":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).DPad.Right == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "SELECT":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.Back == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "START":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.Start == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "A":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.A == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "B":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.B == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "X":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.X == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "Y":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.Y == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "L":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.LeftShoulder == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "R":
                        {
                            var pressed = GamePad.GetState(_gameState.CurrentPlayerIndex).Buttons.RightShoulder == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _gameState.ButtonStates[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_config.DisplayConfig.BackgroundColor);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _matrix);
            if (_gameState.CurrentLayout != null)
            {
                _gameState.CurrentLayout.Draw(_spriteBatch, _config, _gameState, gameTime, _commonTextures);
            }
            _spriteBatch.End();
            _ui.Render();
            base.Draw(gameTime);
        }
    }
}