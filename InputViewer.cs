using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using InputVisualizer.retrospy;
using System;
using System.Collections.Generic;
using InputVisualizer.retrospy.RetroSpy.Readers;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using InputVisualizer.Config;

using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using System.IO.Ports;
using System.Xml.Linq;

namespace InputVisualizer
{
    public class InputViewer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private const int MAX_SECONDS = 4;
        private const float PIXELS_PER_MILLISECOND = 0.05f;
        private const int LINE_LENGTH = 200;
        private const int ROW_HEIGHT = 16;

        private BitmapFont _bitmapFont;
        private IControllerReader _serialReader;
        private readonly BlinkReductionFilter _blinkFilter = new() { ButtonEnabled = true };
        private Dictionary<string, ButtonStateHistory> _buttonInfos = new Dictionary<string, ButtonStateHistory>();
        private Texture2D _pixel;
        private float _horizontalAngle;
        private Dictionary<string, int> _frequencyDict = new Dictionary<string, int>();
        private Dictionary<string, List<Rectangle>> _onRects = new Dictionary<string, List<Rectangle>>();
        private DateTime _minAge;
        private TimeSpan _purgeTimer = TimeSpan.Zero;
        private ViewerConfig _config;
        private Dictionary<string, GamePadInfo> _systemGamePads = new Dictionary<string, GamePadInfo>();
        private GamepadConfig _activeGamepadConfig;
        private InputMode _currentInputMode = InputMode.Gamepad;
        private PlayerIndex _currentPlayerIndex;

        private Desktop _desktop;

        public InputViewer()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 824;
            _graphics.PreferredBackBufferHeight = 420;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _bitmapFont = Content.Load<BitmapFont>("my_font");

            InitGamepads();
            LoadConfig();
            InitInputSource();
            InitViewer();
            InitGui();
            
            base.Initialize();
        }

        private void InitGui()
        {
            MyraEnvironment.Game = this;

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var inputSourceLabel = new Label
            {
                Id = "inputSourceLabel",
                Text = "Input",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(2)
            };
            grid.Widgets.Add(inputSourceLabel);
            var inputSourceCombo = new ComboBox
            {
                GridColumn = 1,
                GridRow = 0,
                Padding = new Thickness(2),
            };

            foreach (var kvp in _systemGamePads)
            {
                var name = kvp.Value.Name.Length > 32 ? kvp.Value.Name.Substring(0, 32) : kvp.Value.Name;
                inputSourceCombo.Items.Add(new ListItem(name, Color.White, kvp.Key));
            }
            inputSourceCombo.Items.Add(new ListItem("RetroSpy", Color.White, "spy"));

            foreach (var item in inputSourceCombo.Items)
            {
                if (_config.CurrentInputSource == (string)item.Tag)
                {
                    inputSourceCombo.SelectedItem = item;
                }
            }

            inputSourceCombo.SelectedIndexChanged += (s, a) =>
            {
                SetCurrentInputSource((string)inputSourceCombo.SelectedItem.Tag);
            };

            grid.Widgets.Add(inputSourceCombo);

            var configureInputButton = new TextButton
            {
                GridColumn = 2,
                GridRow = 0,
                Text = "Configure",
                Padding = new Thickness(2)
            };

            configureInputButton.Click += (s, a) =>
            {
                if (_currentInputMode == InputMode.RetroSpy)
                {
                    ShowConfigureRetroSpyDialog();
                }
                else
                {
                    ShowConfigureGamePadDialog();
                }
            };

            grid.Widgets.Add(configureInputButton);

            _desktop = new Desktop();
            _desktop.Root = grid;
        }

        private void ShowConfigureRetroSpyDialog()
        {
            var dialog = new Dialog
            {
                Title = "RetroSpy Config"
            };
            var stackPanel = new VerticalStackPanel
            {
                Spacing = 8
            };
            stackPanel.Proportions.Add(new Proportion
            {
                Type = ProportionType.Auto,
            });
            stackPanel.Proportions.Add(new Proportion
            {
                Type = ProportionType.Fill,
            });
            var label1 = new Label
            {
                Text = "COM Port Name:"
            };
            stackPanel.Widgets.Add(label1);

            var comPortComboBox = new ComboBox();
            foreach( var name in SerialPort.GetPortNames() )
            {
                var item = new ListItem(name, Color.White, name);
                comPortComboBox.Items.Add(item);
                if (string.Equals(_config.RetroSpyConfig.ComPortName, name, StringComparison.OrdinalIgnoreCase) )
                {
                    comPortComboBox.SelectedItem = item;
                }
            }
            stackPanel.Widgets.Add(comPortComboBox);

            var label2 = new Label
            {
                Text = "Style:"
            };
            stackPanel.Widgets.Add(label2);
            var styleComboBox = new ComboBox();
            foreach(RetroSpyControllerType value in Enum.GetValues( typeof(RetroSpyControllerType) ) )
            {
                var item = new ListItem(value.ToString(), Color.White, value);
                styleComboBox.Items.Add(item);
                if( _config.RetroSpyConfig.ControllerType == value)
                {
                    styleComboBox.SelectedItem = item;
                }
            }
            stackPanel.Widgets.Add(styleComboBox);

            dialog.Content = stackPanel;
            dialog.Closed += (s, a) =>
            {
                if( !dialog.Result )
                {
                    return;
                }
                _config.RetroSpyConfig.ComPortName = (string)comPortComboBox.SelectedItem.Tag;
                _config.RetroSpyConfig.ControllerType = (RetroSpyControllerType)styleComboBox.SelectedItem.Tag;

                SaveConfig();
                InitInputSource();
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureGamePadDialog()
        {

        }

        private void SetCurrentInputSource( string id )
        {
            _config.CurrentInputSource = id;
            SaveConfig();
            _currentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) ? InputMode.RetroSpy : InputMode.Gamepad;
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
            _pixel = new Texture2D(_graphics.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _pixel.SetData(new Color[] { Color.White });
            _horizontalAngle = (float)0.0f;
            _minAge = DateTime.Now.AddSeconds(-MAX_SECONDS);
        }

        private void LoadConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.txt");
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
                if( !gamepadConfig.ButtonMappings.Any() )
                {
                    gamepadConfig.GenerateButtonMappings();
                }
            }

            _config.RetroSpyConfig.GenerateButtonMappings();
            
            SaveConfig();
            _currentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) ? InputMode.RetroSpy : InputMode.Gamepad;
        }

        private void SaveConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.txt");
            File.WriteAllText(path, JsonConvert.SerializeObject(_config));
        }

        private void InitInputSource()
        {
            if (_currentInputMode == InputMode.RetroSpy)
            {
                if (!string.IsNullOrEmpty(_config.RetroSpyConfig.ComPortName))
                {
                    if( _serialReader != null )
                    {
                        _serialReader.Finish();
                    }
                    _serialReader = new SerialControllerReader("COM4 (Generic Arduino)", false, SuperNESandNES.ReadFromPacketNES);
                    _serialReader.ControllerStateChanged += Reader_ControllerStateChanged;
                }
            }
            else if (_currentInputMode == InputMode.Gamepad)
            {
                if( string.IsNullOrEmpty(_config.CurrentInputSource) || !_systemGamePads.Keys.Contains(_config.CurrentInputSource ))
                {
                    if( _config.GamepadConfigs.Any())
                    {
                        foreach( var gamepadConfig in _config.GamepadConfigs)
                        {
                            if( _systemGamePads.Keys.Contains(gamepadConfig.Id))
                            {
                                _config.CurrentInputSource = gamepadConfig.Id;
                                _activeGamepadConfig = gamepadConfig;
                                break;
                            }
                        }
                    }
                }
                else if( _systemGamePads.Keys.Contains(_config.CurrentInputSource))
                {
                    _activeGamepadConfig = _config.GamepadConfigs.First( c => c.Id == _config.CurrentInputSource );
                }
                _currentPlayerIndex = _systemGamePads[_activeGamepadConfig.Id].PlayerIndex;
            }
            InitButtons();
        }

        private void Reader_ControllerStateChanged(object? reader, ControllerStateEventArgs e)
        {
            e = _blinkFilter.Process(e);

            foreach (var button in e.Buttons)
            {
                if (_buttonInfos.ContainsKey(button.Key))
                {
                    if (_buttonInfos[button.Key].IsPressed() != button.Value)
                    {
                        _buttonInfos[button.Key].AddStateChange(button.Value, DateTime.Now);
                    }
                }
            }
        }

        private void InitButtons()
        {
            switch( _currentInputMode )
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

            _frequencyDict.Clear();
            _onRects.Clear();
            foreach (var button in _buttonInfos)
            {
                _frequencyDict.Add(button.Key, 0);
                _onRects.Add(button.Key, new List<Rectangle>());
            }
        }

        private void InitRetroSpyNESButtons()
        {
            _buttonInfos.Clear();

            switch( _config.RetroSpyConfig.ControllerType)
            {
                case RetroSpyControllerType.NES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.NES.ButtonMappings.OrderBy(m => m.Order))
                        {
                            _buttonInfos.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
                        }
                        //_buttonInfos.Add("UP", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "U" });
                        //_buttonInfos.Add("DOWN", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "D" });
                        //_buttonInfos.Add("LEFT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "L" });
                        //_buttonInfos.Add("RIGHT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "R" });
                        //_buttonInfos.Add("B", new ButtonStateHistory() { Color = Color.Gold, Label = "B" });
                        //_buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "A" });
                        //_buttonInfos.Add("SELECT", new ButtonStateHistory() { Color = Color.DimGray, Label = "E" });
                        //_buttonInfos.Add("START", new ButtonStateHistory() { Color = Color.DimGray, Label = "S" });
                        break;
                    }
                case RetroSpyControllerType.SNES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.SNES.ButtonMappings.OrderBy(m => m.Order))
                        {
                            _buttonInfos.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
                        }
                        //_buttonInfos.Add("UP", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "U" });
                        //_buttonInfos.Add("DOWN", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "D" });
                        //_buttonInfos.Add("LEFT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "L" });
                        //_buttonInfos.Add("RIGHT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "R" });
                        //_buttonInfos.Add("Y", new ButtonStateHistory() { Color = Color.DarkGreen, Label = "Y" });
                        //_buttonInfos.Add("B", new ButtonStateHistory() { Color = Color.Gold, Label = "B" });
                        //_buttonInfos.Add("X", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "X" });
                        //_buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DarkRed, Label = "A" });
                        //_buttonInfos.Add("L", new ButtonStateHistory() { Color = Color.DarkBlue, Label = "L" });
                        //_buttonInfos.Add("R", new ButtonStateHistory() { Color = Color.DarkBlue, Label = "R" });
                        //_buttonInfos.Add("SELECT", new ButtonStateHistory() { Color = Color.DimGray, Label = "E" });
                        //_buttonInfos.Add("START", new ButtonStateHistory() { Color = Color.DimGray, Label = "S" });
                        break;
                    }
            }
            
        }

        private void InitGamepadButtons()
        {
            _buttonInfos.Clear();
            foreach( var mapping in _activeGamepadConfig.ButtonMappings.OrderBy( m => m.Order ) )
            {
                _buttonInfos.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if ( _currentInputMode == InputMode.Gamepad)
            {
                ReadGamepadInputs();
            }

            foreach (var button in _buttonInfos)
            {
                _frequencyDict[button.Key] = button.Value.GetPressedLastSecond();
            }
            _minAge = DateTime.Now.AddSeconds(-MAX_SECONDS);
            _purgeTimer += gameTime.ElapsedGameTime;
            if (_purgeTimer.Milliseconds > 200)
            {
                foreach (var button in _buttonInfos.Values)
                {
                    button.RemoveOldStateChanges();
                }
                _purgeTimer = TimeSpan.Zero;
            }
            BuildRects();
            base.Update(gameTime);
        }

        private void ReadGamepadInputs()
        {
            foreach (var button in _buttonInfos)
            {
                switch (button.Key)
                {
                    case "UP":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).DPad.Up == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "DOWN":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).DPad.Down == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "LEFT":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).DPad.Left == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "RIGHT":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).DPad.Right == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "SELECT":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.Back == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "START":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.Start == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "A":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.A == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "B":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.B == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "X":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.X == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "Y":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.Y == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "L":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.LeftShoulder == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "R":
                        {
                            var pressed = GamePad.GetState(_currentPlayerIndex).Buttons.RightShoulder == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                }
            }
        }

        private void BuildRects()
        {
            var yPos = 52;
            var yInc = ROW_HEIGHT;
            var yOffset = 2;

            var now = DateTime.Now.AddMilliseconds(-2);
            foreach (var kvp in _buttonInfos)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;

                var baseX = 41;

                var currX = baseX;
                var pixelsUsed = 0;
                for (var i = info.StateChangeHistory.Count - 1; i >= 0; i--)
                {
                    var endTime = info.StateChangeHistory[i].EndTime == DateTime.MinValue ? now : info.StateChangeHistory[i].EndTime;

                    if (endTime < _minAge || pixelsUsed >= LINE_LENGTH)
                    {
                        break;
                    }

                    var startTime = info.StateChangeHistory[i].StartTime < _minAge ? _minAge : info.StateChangeHistory[i].StartTime;
                    var lengthInMs = (endTime - startTime).TotalMilliseconds;
                    var lengthInPixels = (int)(lengthInMs * PIXELS_PER_MILLISECOND);
                    if (lengthInPixels < 1)
                    {
                        lengthInPixels = 1;
                    }

                    pixelsUsed += lengthInPixels;

                    if (!info.StateChangeHistory[i].IsPressed)
                    {
                        currX += lengthInPixels;
                        continue;
                    }

                    var rec = new Rectangle();
                    rec.X = currX;
                    rec.Y = yPos - 2 - yOffset - 1;
                    rec.Width = lengthInPixels;
                    rec.Height = yOffset * 2 + 1;
                    _onRects[kvp.Key].Add(rec);

                    currX += lengthInPixels;
                }
                yPos += yInc;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            //var matrix = Matrix.CreateScale(1.f, 1.5f, 1.0f);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend /*transformMatrix: matrix */ );
            DrawButtons();
            DrawQueues();
            _spriteBatch.End();
            _desktop.Render();

            base.Draw(gameTime);
        }

        private void DrawButtons()
        {
            var yPos = 35;
            var yInc = ROW_HEIGHT;
            var rightMargin = 10;

            foreach (var kvp in _buttonInfos)
            {
                _spriteBatch.DrawString(_bitmapFont, kvp.Value.Label, new Vector2(rightMargin, yPos), Color.White);
                yPos += yInc;
            }
        }

        private void DrawQueues()
        {
            var yPos = 52;
            var yInc = ROW_HEIGHT;
            var baseX = 41;
            var infoX = baseX + LINE_LENGTH + 5;

            foreach (var kvp in _buttonInfos)
            {
                var info = kvp.Value;
                var rec = Rectangle.Empty;
                var semiTransFactor = kvp.Value.StateChangeHistory.Any() ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = kvp.Value.StateChangeHistory.Any() ? 0.75f : 0.25f;

                //empty button press rectangle
                rec.X = 28;
                rec.Y = yPos - 9;
                rec.Width = 13;
                rec.Height = 13;
                _spriteBatch.Draw(_pixel, rec, null, info.Color * semiTransFactor, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                rec.X = 29;
                rec.Y = yPos - 8;
                rec.Width = 11;
                rec.Height = 11;
                _spriteBatch.Draw(_pixel, rec, null, Color.Black * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                //draw entire off line
                rec.X = baseX;
                rec.Y = yPos - 3;
                rec.Width = LINE_LENGTH - 1;
                rec.Height = 1;
                _spriteBatch.Draw(_pixel, rec, null, info.Color * semiTransFactor, _horizontalAngle, new Vector2(0, 0), SpriteEffects.None, 0);

                foreach (var rect in _onRects[kvp.Key])
                {
                    _spriteBatch.Draw(_pixel, rect, null, info.Color, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                if (info.IsPressed())
                {
                    //fill in button rect
                    rec.X = 28;
                    rec.Y = yPos - 9;
                    rec.Width = 12;
                    rec.Height = 12;
                    _spriteBatch.Draw(_pixel, rec, null, info.Color * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                    var elapsed = info.PressedElapsed();
                    if (elapsed.TotalSeconds > 2)
                    {
                        _spriteBatch.DrawString(_bitmapFont, elapsed.ToString("ss':'f"), new Vector2(infoX, yPos - 17), info.Color);
                    }
                }

                if (_frequencyDict[kvp.Key] >= 4)
                {
                    _spriteBatch.DrawString(_bitmapFont, $"x{_frequencyDict[kvp.Key]}", new Vector2(infoX, yPos - 17), info.Color);
                }
                yPos += yInc;
            }
        }
    }
}