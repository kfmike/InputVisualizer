using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Gui;
using MonoGame.Extended.Gui.Controls;
using MonoGame.Extended.ViewportAdapters;
using InputVisualizer.retrospy;
using System;
using System.Collections.Generic;
using InputVisualizer.retrospy.RetroSpy.Readers;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using InputVisualizer.Config;
using InputVisualizer.UI;

namespace InputVisualizer
{
    public class InputViewer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GuiSystem _guiSystem;

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
        private bool _readGamepad = false;
        private Dictionary<string, PlayerIndex> _activeGamepads = new Dictionary<string, PlayerIndex>();
        private int _activeGamepadIndex = 0;
        private GamepadConfig _activeGamepadConfig;

        public InputViewer()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 420;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += WindowOnClientSizeChanged;
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs eventArgs)
        {
            _guiSystem.ClientSizeChanged();
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _bitmapFont = Content.Load<BitmapFont>("my_font");

            InitGamepads();
            InitViewer();
            InitGui();

            base.Initialize();
        }

        private void InitGui()
        {
            var viewportAdapter = new DefaultViewportAdapter(GraphicsDevice);
            var guiRenderer = new GuiSpriteBatchRenderer(GraphicsDevice, () => Matrix.Identity);

            BitmapFont.UseKernings = false;
            Skin.CreateDefault(_bitmapFont);

            var generalContent = new ConfigViewModel("General",
                new StackPanel
                {
                    Margin = 5,
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        new Label("Buttons") { Margin = 5 },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                new Button { Content = "Enabled" },
                                new Button { Content = "Disabled", IsEnabled = false },
                                new ToggleButton { Content = "ToggleButton" }
                            }
                        },

                        new Label("TextBox") { Margin = 5 },
                        new TextBox {Text = "TextBox" },

                        new Label("CheckBox") { Margin = 5 },
                        new CheckBox {Content = "Check me please!"},

                        new Label("ListBox") { Margin = 5 },
                        new ListBox {Items = {"ListBoxItem1", "ListBoxItem2", "ListBoxItem3"}, SelectedIndex = 0},

                        new Label("ProgressBar") { Margin = 5 },
                        new ProgressBar {Progress = 0.5f, Width = 100},

                        new Label("ComboBox") { Margin = 5 },
                        new ComboBox {Items = {"ComboBoxItemA", "ComboBoxItemB", "ComboBoxItemC"}, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Left}
                    }
                });

            var displayContent = new ConfigViewModel("Display",
                new StackPanel
                {
                    Margin = 5,
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        new Label("Buttons") { Margin = 5 },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                new Button { Content = "Enabled" },
                                new Button { Content = "Disabled", IsEnabled = false },
                                new ToggleButton { Content = "ToggleButton" }
                            }
                        },

                        new Label("TextBox") { Margin = 5 },
                        new TextBox {Text = "TextBox" },

                        new Label("CheckBox") { Margin = 5 },
                        new CheckBox {Content = "Check me please!"},

                        new Label("ListBox") { Margin = 5 },
                        new ListBox {Items = {"ListBoxItem1", "ListBoxItem2", "ListBoxItem3"}, SelectedIndex = 0},

                        new Label("ProgressBar") { Margin = 5 },
                        new ProgressBar {Progress = 0.5f, Width = 100},

                        new Label("ComboBox") { Margin = 5 },
                        new ComboBox {Items = {"ComboBoxItemA", "ComboBoxItemB", "ComboBoxItemC"}, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Left}
                    }
                });

            var gamePadConfigViewModels = new List<ConfigViewModel>();
            foreach( var gamepad in _activeGamepads )
            {
                var gamepadContent = new ConfigViewModel($"Gamepad {gamepad.Value}",
                new StackPanel
                {
                    Margin = 5,
                    Orientation = Orientation.Vertical,
                    Items =
                    {
                        new Label("Buttons") { Margin = 5 },
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 5,
                            Items =
                            {
                                new Button { Content = "Enabled" },
                                new Button { Content = "Disabled", IsEnabled = false },
                                new ToggleButton { Content = "ToggleButton" }
                            }
                        },

                        new Label("TextBox") { Margin = 5 },
                        new TextBox {Text = "TextBox" },

                        new Label("CheckBox") { Margin = 5 },
                        new CheckBox {Content = "Check me please!"},

                        new Label("ListBox") { Margin = 5 },
                        new ListBox {Items = {"ListBoxItem1", "ListBoxItem2", "ListBoxItem3"}, SelectedIndex = 0},

                        new Label("ProgressBar") { Margin = 5 },
                        new ProgressBar {Progress = 0.5f, Width = 100},

                        new Label("ComboBox") { Margin = 5 },
                        new ComboBox {Items = {"ComboBoxItemA", "ComboBoxItemB", "ComboBoxItemC"}, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Left}
                    }
                });
                gamePadConfigViewModels.Add(gamepadContent);
            }

            var allContents = new List<ConfigViewModel>() { generalContent, displayContent };
            allContents.AddRange(gamePadConfigViewModels);

            var configScreen = new Screen
            {
                Content = new DockPanel
                {
                    LastChildFill = false,
                    Items =
                    {
                        new ListBox
                        {
                            Name = "MenuList",
                            AttachedProperties = { { DockPanel.DockProperty, Dock.Right} },
                            ItemPadding = new Thickness(5),
                            VerticalAlignment = VerticalAlignment.Stretch,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            SelectedIndex = 0,
                            Items = {  }
                        },
                        new ContentControl
                        {
                            AttachedProperties = { { DockPanel.DockProperty, Dock.Right} },
                            Name = "Content",
                            BackgroundColor = new Color(30, 30, 30),

                        }
                    }
                }
            };

            _guiSystem = new GuiSystem(viewportAdapter, guiRenderer) { ActiveScreen = configScreen };

            var menuList = configScreen.FindControl<ListBox>("MenuList");

            foreach( var content in allContents )
            {
                menuList.Items.Add(content);
            }

            var menuContent = configScreen.FindControl<ContentControl>("Content");

            menuList.SelectedIndexChanged += (sender, args) => menuContent.Content = (menuList.SelectedItem as ConfigViewModel)?.Content;
            menuContent.Content = (menuList.SelectedItem as ConfigViewModel)?.Content;
        }

        private void InitGamepads()
        {
            _activeGamepads.Clear();
            for (var i = PlayerIndex.One; i <= PlayerIndex.Four; i++)
            {
                var state = GamePad.GetState(i);
                if (state.IsConnected)
                {
                    var caps = GamePad.GetCapabilities(i);
                    _activeGamepads.Add(caps.Identifier, i);
                }
            }
        }

        private void InitViewer()
        {
            LoadConfig();

            if (_config.CurrentControllerType == ControllerType.RetroSpy)
            {
                _serialReader = new SerialControllerReader("COM4 (Generic Arduino)", false, SuperNESandNES.ReadFromPacketNES);
                _serialReader.ControllerStateChanged += Reader_ControllerStateChanged;
                InitRetroSpyNESButtons();
            }
            else if (_config.CurrentControllerType == ControllerType.Gamepad)
            {
                _readGamepad = true;

                if (string.IsNullOrEmpty(_config.CurrentGamepad) && _config.Gamepads.Any())
                {
                    _activeGamepadIndex = 0;
                    _activeGamepadConfig = _config.Gamepads[0];
                }

                InitGamepadButtons();
            }

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

            foreach (var kvp in _activeGamepads)
            {
                var gamepadConfig = _config.Gamepads.FirstOrDefault(g => g.Id == kvp.Key);
                if (gamepadConfig == null)
                {
                    _config.Gamepads.Add(new GamepadConfig() { Id = kvp.Key, Style = GamepadStyle.XBOX });
                }
            }
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

        private void InitRetroSpyNESButtons()
        {
            _buttonInfos.Add("UP", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "U" });
            _buttonInfos.Add("DOWN", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "D" });
            _buttonInfos.Add("LEFT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "L" });
            _buttonInfos.Add("RIGHT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "R" });
            _buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "A" });
            _buttonInfos.Add("B", new ButtonStateHistory() { Color = Color.Gold, Label = "B" });
            _buttonInfos.Add("SELECT", new ButtonStateHistory() { Color = Color.DimGray, Label = "E" });
            _buttonInfos.Add("START", new ButtonStateHistory() { Color = Color.DimGray, Label = "S" });

            foreach (var button in _buttonInfos)
            {
                _frequencyDict.Add(button.Key, 0);
                _onRects.Add(button.Key, new List<Rectangle>());
            }
        }

        private void InitGamepadButtons()
        {
            _buttonInfos.Add("UP", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "U" });
            _buttonInfos.Add("DOWN", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "D" });
            _buttonInfos.Add("LEFT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "L" });
            _buttonInfos.Add("RIGHT", new ButtonStateHistory() { Color = Color.DarkSeaGreen, Label = "R" });

            if (_activeGamepadConfig.Style == GamepadStyle.NES)
            {
                _buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "A" });
                _buttonInfos.Add("B", new ButtonStateHistory() { Color = Color.Gold, Label = "B" });
            }
            else if (_activeGamepadConfig.Style == GamepadStyle.Arcade)
            {
                _buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DarkRed, Label = "A" });
                _buttonInfos.Add("X", new ButtonStateHistory() { Color = Color.Gold, Label = "B" });
                _buttonInfos.Add("Y", new ButtonStateHistory() { Color = Color.DarkGreen, Label = "C" });
                _buttonInfos.Add("R", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "D" });
            }
            else
            {
                _buttonInfos.Add("A", new ButtonStateHistory() { Color = Color.DarkGreen, Label = "A" });
                _buttonInfos.Add("B", new ButtonStateHistory() { Color = Color.DarkRed, Label = "B" });
                _buttonInfos.Add("X", new ButtonStateHistory() { Color = Color.DeepSkyBlue, Label = "X" });
                _buttonInfos.Add("Y", new ButtonStateHistory() { Color = Color.Gold, Label = "Y" });
            }

            _buttonInfos.Add("SELECT", new ButtonStateHistory() { Color = Color.DimGray, Label = "E" });
            _buttonInfos.Add("START", new ButtonStateHistory() { Color = Color.DimGray, Label = "S" });

            foreach (var button in _buttonInfos)
            {
                _frequencyDict.Add(button.Key, 0);
                _onRects.Add(button.Key, new List<Rectangle>());
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

            if (_readGamepad)
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
            _guiSystem.Update(gameTime);
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
                            var pressed = GamePad.GetState(_activeGamepadIndex).DPad.Up == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "DOWN":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).DPad.Down == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "LEFT":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).DPad.Left == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "RIGHT":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).DPad.Right == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "SELECT":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.Back == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "START":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.Start == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "A":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.A == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "B":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.B == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "X":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.X == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "Y":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.Y == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "L":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.LeftShoulder == ButtonState.Pressed;
                            if (button.Value.IsPressed() != pressed)
                            {
                                _buttonInfos[button.Key].AddStateChange(pressed, DateTime.Now);
                            }
                            break;
                        }
                    case "R":
                        {
                            var pressed = GamePad.GetState(_activeGamepadIndex).Buttons.RightShoulder == ButtonState.Pressed;
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
            _guiSystem.Draw(gameTime);
            DrawButtons();
            DrawQueues();
            _spriteBatch.End();


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