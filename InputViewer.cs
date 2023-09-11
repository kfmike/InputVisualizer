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
using Myra.Graphics2D.UI.ColorPicker;

namespace InputVisualizer
{
    public class InputViewer : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private float _pixelsPerMs = 0.05f;
        private const int ROW_HEIGHT = 16;

        private BitmapFont _bitmapFont;
        private BitmapFont _bitmapFont2;
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

        private bool _listeningForInput = false;
        private bool _listeningCancelPressed = false;
        private GamepadButtonMapping _listeningMapping;
        private TextButton _listeningButton;
        private Grid _listeningGrid;

        private Desktop _desktop;

        public InputViewer()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 824;
            _graphics.PreferredBackBufferHeight = 620;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            InactiveSleepTime = TimeSpan.Zero;
        }

        protected override void Initialize()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _bitmapFont = Content.Load<BitmapFont>("my_font");
            _bitmapFont2 = Content.Load<BitmapFont>("my_font2");

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
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var inputSourceCombo = new ComboBox
            {
                GridColumn = 0,
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
                GridColumn = 1,
                GridRow = 0,
                Text = "Input",
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

            var configureDisplayButton = new TextButton
            {
                GridColumn = 2,
                GridRow = 0,
                Text = "Display",
                Padding = new Thickness(2)
            };
            configureDisplayButton.Click += (s, a) =>
            {
                ShowConfigureDisplayDialog();
            };
            grid.Widgets.Add(configureDisplayButton);

            _desktop = new Desktop();
            _desktop.Root = grid;
            _desktop.Root.VerticalAlignment = VerticalAlignment.Bottom;
            _desktop.Root.HorizontalAlignment = HorizontalAlignment.Right;
        }

        private void ShowConfigureGamePadDialog()
        {
            var buttonMapWidgets = new List<Widget>();

            var gamePadName = _systemGamePads[_activeGamepadConfig.Id].Name;
            var name = gamePadName.Length > 32 ? gamePadName.Substring(0, 32) : gamePadName;
            var dialog = new Dialog
            {

                Title = $"{name} Config"
            };

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var label2 = new Label
            {
                Text = "Style:",
                GridRow = 0,
                GridColumn = 0,
                GridColumnSpan = 2,
            };
            grid.Widgets.Add(label2);
            var styleComboBox = new ComboBox()
            {
                GridRow = 0,
                GridColumn = 2,
                GridColumnSpan = 3,
            };

            foreach (GamepadStyle value in Enum.GetValues(typeof(GamepadStyle)))
            {
                var item = new ListItem(value.ToString(), Color.White, value);
                styleComboBox.Items.Add(item);
                if (_activeGamepadConfig.Style == value)
                {
                    styleComboBox.SelectedItem = item;
                }
            }
            styleComboBox.SelectedIndexChanged += (o, e) =>
            {
                _activeGamepadConfig.Style = (GamepadStyle)styleComboBox.SelectedItem.Tag;
                _activeGamepadConfig.GenerateButtonMappings();
                DrawButtonMappings(_activeGamepadConfig.ButtonMappings, grid, buttonMapWidgets, 2, showMapButton: true);
            };
            grid.Widgets.Add(styleComboBox);

            var mapLabelVisible = new Label
            {
                Text = "Visible",
                GridRow = 1,
                GridColumn = 0
            };
            var mapLabelButton = new Label
            {
                Text = "Button",
                GridRow = 1,
                GridColumn = 1
            };
            var mapLabelButtonMap = new Label
            {
                Text = "Mapped To",
                GridRow = 1,
                GridColumn = 2
            };
            var mapLabelColor = new Label
            {
                Text = "Color",
                GridRow = 1,
                GridColumn = 3
            };
            var mapLabelOrder = new Label
            {
                Text = "Order",
                GridRow = 1,
                GridColumn = 4,
                GridColumnSpan = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            grid.Widgets.Add(mapLabelVisible);
            grid.Widgets.Add(mapLabelButton);
            grid.Widgets.Add(mapLabelButtonMap);
            grid.Widgets.Add(mapLabelColor);
            grid.Widgets.Add(mapLabelOrder);

            DrawButtonMappings(_activeGamepadConfig.ButtonMappings, grid, buttonMapWidgets, 2, showMapButton: true);

            dialog.Content = grid;
            dialog.Closing += (s, a) =>
            {
                if (_listeningForInput)
                {
                    var messageBox = Dialog.CreateMessageBox("Hey you!", "Finish mapping button or hit DEL to cancel");
                    messageBox.ShowModal(_desktop);
                    a.Cancel = true;
                }
                else if (_listeningCancelPressed)
                {
                    a.Cancel = true;
                    _listeningCancelPressed = false;
                }
            };
            dialog.Closed += (s, a) =>
            {
                _listeningForInput = false;
                if (!dialog.Result)
                {
                    return;
                }

                SaveConfig();
                InitInputSource();
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureRetroSpyDialog()
        {
            var buttonMapWidgets = new List<Widget>();

            var dialog = new Dialog
            {
                Title = "RetroSpy Config"
            };

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var label1 = new Label
            {
                Text = "COM Port:",
                GridColumnSpan = 2
            };
            grid.Widgets.Add(label1);

            var comPortComboBox = new ComboBox()
            {
                GridRow = 0,
                GridColumn = 2,
                GridColumnSpan = 2,
            };

            foreach (var name in SerialPort.GetPortNames())
            {
                var item = new ListItem(name, Color.White, name);
                comPortComboBox.Items.Add(item);
                if (string.Equals(_config.RetroSpyConfig.ComPortName, name, StringComparison.OrdinalIgnoreCase))
                {
                    comPortComboBox.SelectedItem = item;
                }
            }
            grid.Widgets.Add(comPortComboBox);

            var label2 = new Label
            {
                Text = "Style:",
                GridRow = 1,
                GridColumn = 0,
                GridColumnSpan = 2,
            };
            grid.Widgets.Add(label2);
            var styleComboBox = new ComboBox()
            {
                GridRow = 1,
                GridColumn = 2,
                GridColumnSpan = 2,
            };

            foreach (RetroSpyControllerType value in Enum.GetValues(typeof(RetroSpyControllerType)))
            {
                var item = new ListItem(value.ToString(), Color.White, value);
                styleComboBox.Items.Add(item);
                if (_config.RetroSpyConfig.ControllerType == value)
                {
                    styleComboBox.SelectedItem = item;
                }
            }
            styleComboBox.SelectedIndexChanged += (o, e) =>
            {
                _config.RetroSpyConfig.ControllerType = (RetroSpyControllerType)styleComboBox.SelectedItem.Tag;
                DrawButtonMappings(_config.RetroSpyConfig.GetMappingSet(_config.RetroSpyConfig.ControllerType).ButtonMappings, grid, buttonMapWidgets, 3);
            };
            grid.Widgets.Add(styleComboBox);

            var mapLabelVisible = new Label
            {
                Text = "Visible",
                GridRow = 2,
                GridColumn = 0
            };
            var mapLabelButton = new Label
            {
                Text = "Button",
                GridRow = 2,
                GridColumn = 1
            };
            var mapLabelColor = new Label
            {
                Text = "Color",
                GridRow = 2,
                GridColumn = 2
            };
            var mapLabelOrder = new Label
            {
                Text = "Order",
                GridRow = 2,
                GridColumn = 3,
                GridColumnSpan = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            grid.Widgets.Add(mapLabelVisible);
            grid.Widgets.Add(mapLabelButton);
            grid.Widgets.Add(mapLabelColor);
            grid.Widgets.Add(mapLabelOrder);

            DrawButtonMappings(_config.RetroSpyConfig.GetMappingSet(_config.RetroSpyConfig.ControllerType).ButtonMappings, grid, buttonMapWidgets, 3);

            dialog.Content = grid;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                if (comPortComboBox.SelectedItem != null)
                {
                    _config.RetroSpyConfig.ComPortName = (string)comPortComboBox.SelectedItem.Tag;
                }
                _config.RetroSpyConfig.ControllerType = (RetroSpyControllerType)styleComboBox.SelectedItem.Tag;

                SaveConfig();
                InitInputSource();
            };
            dialog.ShowModal(_desktop);
        }

        private void DrawButtonMappings(List<GamepadButtonMapping> mappings, Grid grid, List<Widget> currentWidgets, int gridStartRow, bool showMapButton = false)
        {
            var currGridRow = gridStartRow;
            var lastGridRow = gridStartRow + mappings.Count - 1;

            foreach (var widget in currentWidgets)
            {
                grid.Widgets.Remove(widget);
            }
            currentWidgets.Clear();

            foreach (var mapping in mappings.OrderBy(m => m.Order))
            {
                var currColumn = 0;

                var visibleCheck = new CheckBox
                {
                    IsChecked = mapping.IsVisible,
                    GridRow = currGridRow,
                    GridColumn = currColumn
                };
                visibleCheck.Click += (s, e) =>
                {
                    mapping.IsVisible = visibleCheck.IsChecked;
                };
                currentWidgets.Add(visibleCheck);
                currColumn++;
                var buttonLabel = new Label
                {
                    Text = mapping.Label,
                    GridRow = currGridRow,
                    GridColumn = currColumn
                };
                currentWidgets.Add(buttonLabel);
                currColumn++;

                if (showMapButton)
                {
                    var mapButton = new TextButton
                    {
                        GridRow = currGridRow,
                        GridColumn = currColumn,
                        Text = mapping.MappedButtonType.ToString(),
                        Padding = new Thickness(2),
                        Tag = mapping
                    };
                    mapButton.Click += (s, e) =>
                    {
                        if (_listeningForInput)
                        {
                            var messageBox = Dialog.CreateMessageBox("Hey you!", "Finish mapping button or hit ESC to cancel");
                            messageBox.ShowModal(_desktop);
                            return;
                        }
                        _listeningForInput = true;
                        _listeningButton = mapButton;
                        _listeningButton.Text = "...";
                        _listeningMapping = mapping;
                        _listeningGrid = grid;
                    };

                    currentWidgets.Add(mapButton);
                    currColumn++;
                }

                var colorButton = new TextButton
                {
                    GridRow = currGridRow,
                    GridColumn = currColumn,
                    Text = "Color",
                    Padding = new Thickness(2),
                    TextColor = mapping.Color,
                };
                colorButton.Click += (s, e) =>
                {
                    ChooseColor(mapping, colorButton);
                };
                currentWidgets.Add(colorButton);
                currColumn++;

                if (currGridRow > gridStartRow)
                {
                    var upButton = new TextButton
                    {
                        GridColumn = currColumn,
                        GridRow = currGridRow,
                        Width = 30,
                        Text = "↑",
                        HorizontalAlignment = HorizontalAlignment.Right
                    };

                    upButton.Click += (s, e) =>
                    {
                        mappings = UpdateOrder(mappings, mapping, goUp: true);
                        DrawButtonMappings(mappings, grid, currentWidgets, gridStartRow, showMapButton);
                    };
                    currentWidgets.Add(upButton);
                }
                currColumn++;

                if (currGridRow < lastGridRow)
                {
                    var downButton = new TextButton
                    {
                        GridColumn = currColumn,
                        GridRow = currGridRow,
                        Width = 30,
                        Text = "↓",
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    downButton.Click += (s, e) =>
                    {
                        mappings = UpdateOrder(mappings, mapping, goUp: false);
                        DrawButtonMappings(mappings, grid, currentWidgets, gridStartRow, showMapButton);
                    };
                    currentWidgets.Add(downButton);
                }
                currGridRow++;
            }

            foreach (var widget in currentWidgets)
            {
                grid.AddChild(widget);
            }
        }

        private List<GamepadButtonMapping> UpdateOrder(List<GamepadButtonMapping> mappings, GamepadButtonMapping targetMapping, bool goUp)
        {
            var inOrder = mappings.OrderBy(m => m.Order).ToList();
            var currIndex = inOrder.IndexOf(targetMapping);
            var targetIndex = 0;
            if (goUp)
            {
                targetIndex = currIndex - 1;
                if (targetIndex < 0)
                {
                    targetIndex = 0;
                }
            }
            else
            {
                targetIndex = currIndex + 1;
                if (targetIndex > mappings.Count - 1)
                {
                    targetIndex = mappings.Count - 1;
                }
            }

            inOrder.Remove(targetMapping);
            inOrder.Insert(targetIndex, targetMapping);

            for (var i = 0; i < inOrder.Count; i++)
            {
                inOrder[i].Order = i;
            }
            return inOrder;
        }

        private void ShowConfigureDisplayDialog()
        {
            var dialog = new Dialog
            {
                Title = "Display Config"
            };

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Right,
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var label1 = new Label
            {
                Text = "Line Length:",
            };
            grid.Widgets.Add(label1);

            var displayWidthText = new TextBox()
            {
                GridRow = 0,
                GridColumn = 1,
                Text = _config.DisplayConfig.LineLength.ToString(),
                Width = 50
            };
            grid.Widgets.Add(displayWidthText);

            var labelSpeed = new Label
            {
                Text = "Speed:",
                GridRow = 1
            };
            grid.Widgets.Add(labelSpeed);

            var displaySpeedSpin = new HorizontalSlider()
            {
                GridRow = 1,
                GridColumn = 1,
                Value = _config.DisplayConfig.Speed,
                Minimum = 1,
                Maximum = 11,
                Width = 150
            };
            grid.Widgets.Add(displaySpeedSpin);

            var labelTurnOffLineSpeed = new Label
            {
                Text = "Dim Line Delay:",
                GridRow = 2
            };
            grid.Widgets.Add(labelTurnOffLineSpeed);

            var turnOffLineSpeedSpin = new HorizontalSlider()
            {
                GridRow = 2,
                GridColumn = 1,
                Value = _config.DisplayConfig.TurnOffLineSpeed / 50.0f,
                Minimum = 0,
                Maximum = 100,
                Width = 150
            };
            grid.Widgets.Add(turnOffLineSpeedSpin);

            var label2 = new Label
            {
                Text = "Show Duration Min Seconds:",
                GridRow = 3
            };
            grid.Widgets.Add(label2);
            var pressThresholdText = new TextBox()
            {
                GridRow = 3,
                GridColumn = 1,
                Text = _config.DisplayConfig.MinDisplayDuration.ToString(),
                Width = 50
            };
            grid.Widgets.Add(pressThresholdText);

            var label3 = new Label
            {
                Text = "Show Frequency Min Value:",
                GridRow = 4
            };
            grid.Widgets.Add(label3);
            var frequencyThresholdText = new TextBox()
            {
                GridRow = 4,
                GridColumn = 1,
                Text = _config.DisplayConfig.MinDisplayFrequency.ToString(),
                Width = 50
            };
            grid.Widgets.Add(frequencyThresholdText);

            var label4 = new Label
            {
                Text = "Display Durations:",
                GridRow = 5
            };
            grid.Widgets.Add(label4);
            var displayDurationCheck = new CheckBox()
            {
                GridRow = 5,
                GridColumn = 1,
                IsChecked = _config.DisplayConfig.DisplayDuration,
            };
            grid.Widgets.Add(displayDurationCheck);

            var label5 = new Label
            {
                Text = "Display Frequency Last Second:",
                GridRow = 6
            };
            grid.Widgets.Add(label5);
            var displayFrequencyCheck = new CheckBox()
            {
                GridRow = 6,
                GridColumn = 1,
                IsChecked = _config.DisplayConfig.DisplayFrequency,
            };
            grid.Widgets.Add(displayFrequencyCheck);

            var label8 = new Label
            {
                Text = "Show Idle Lines:",
                GridRow = 7
            };
            grid.Widgets.Add(label8);

            var displayIdleLindesCheck = new CheckBox()
            {
                GridRow = 7,
                GridColumn = 1,
                IsChecked = _config.DisplayConfig.DrawIdleLines
            };
            grid.Widgets.Add(displayIdleLindesCheck);

            var label6 = new Label
            {
                Text = "Background:",
                GridRow = 8
            };
            grid.Widgets.Add(label6);
            var colorButton = new TextButton
            {
                GridRow = 8,
                GridColumn = 1,
                Text = "Color",
                Padding = new Thickness(2),
                TextColor = _config.DisplayConfig.BackgroundColor,
            };
            colorButton.Click += (s, e) =>
            {
                ChooseBackgroundColor(colorButton);
            };
            grid.Widgets.Add(colorButton);

            var label7 = new Label
            {
                Text = "Layout:",
                GridRow = 9,
            };
            grid.Widgets.Add(label7);
            var layoutComboBox = new ComboBox()
            {
                GridRow = 9,
                GridColumn = 1,
            };

            foreach (LayoutStyle value in Enum.GetValues(typeof(LayoutStyle)))
            {
                var item = new ListItem(value.ToString(), Color.White, value);
                layoutComboBox.Items.Add(item);
                if (_config.DisplayConfig.Layout == value)
                {
                    layoutComboBox.SelectedItem = item;
                }
            }
            grid.Widgets.Add(layoutComboBox);

            dialog.Content = grid;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                if (Int32.TryParse(displayWidthText.Text, out var displayWidth))
                {
                    _config.DisplayConfig.LineLength = displayWidth < 10 ? 1 : displayWidth;
                }
                if (Int32.TryParse(pressThresholdText.Text, out var pressThresholdSeconds))
                {
                    _config.DisplayConfig.MinDisplayDuration = pressThresholdSeconds < 1 ? 1 : pressThresholdSeconds;
                }
                if (Int32.TryParse(frequencyThresholdText.Text, out var frequencyThresholdValue))
                {
                    _config.DisplayConfig.MinDisplayFrequency = frequencyThresholdValue < 1 ? 1 : frequencyThresholdValue;
                }
                _config.DisplayConfig.Speed = displaySpeedSpin.Value;
                _config.DisplayConfig.TurnOffLineSpeed = turnOffLineSpeedSpin.Value * 50.0f;
                UpdateSpeed();
                _config.DisplayConfig.DisplayDuration = displayDurationCheck.IsChecked;
                _config.DisplayConfig.DisplayFrequency = displayFrequencyCheck.IsChecked;
                _config.DisplayConfig.DrawIdleLines = displayIdleLindesCheck.IsChecked;
                _config.DisplayConfig.BackgroundColor = colorButton.TextColor;
                _config.DisplayConfig.Layout = (LayoutStyle)layoutComboBox.SelectedItem.Tag;

                SaveConfig();
            };
            dialog.ShowModal(_desktop);
        }

        public void ChooseColor(GamepadButtonMapping mapping, TextButton colorButton)
        {
            var colorWindow = new ColorPickerDialog();
            colorWindow.Color = colorButton.TextColor;
            colorWindow.ColorPickerPanel._saveColor.Visible = false;
            colorWindow.ShowModal(_desktop);

            colorWindow.Closed += (s, a) =>
            {
                if (!colorWindow.Result)
                {
                    return;
                }
                mapping.Color = colorWindow.Color;
                colorButton.TextColor = colorWindow.Color;
            };
        }

        public void ChooseBackgroundColor(TextButton colorButton)
        {
            var colorWindow = new ColorPickerDialog();
            colorWindow.Color = colorButton.TextColor;
            colorWindow.ShowModal(_desktop);

            colorWindow.Closed += (s, a) =>
            {
                if (!colorWindow.Result)
                {
                    return;
                }
                colorButton.TextColor = colorWindow.Color;
            };
        }

        private void SetCurrentInputSource(string id)
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
            CalcMinAge();
        }

        private void UpdateSpeed()
        {
            _pixelsPerMs = 0.05f * _config.DisplayConfig.Speed;
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

            SaveConfig();
            _currentInputMode = string.Equals(_config.CurrentInputSource, "spy", StringComparison.InvariantCultureIgnoreCase) ? InputMode.RetroSpy : InputMode.Gamepad;
        }

        private void SaveConfig()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"config.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }

        private void InitInputSource()
        {
            if (_currentInputMode == InputMode.RetroSpy)
            {
                if (!string.IsNullOrEmpty(_config.RetroSpyConfig.ComPortName))
                {
                    if (_serialReader != null)
                    {
                        _serialReader.Finish();
                    }
                    _serialReader = new SerialControllerReader(_config.RetroSpyConfig.ComPortName, false, SuperNESandNES.ReadFromPacketNES);
                    _serialReader.ControllerStateChanged += Reader_ControllerStateChanged;
                }
            }
            else if (_currentInputMode == InputMode.Gamepad)
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
                                _activeGamepadConfig = gamepadConfig;
                                break;
                            }
                        }
                    }
                }
                else if (_systemGamePads.Keys.Contains(_config.CurrentInputSource))
                {
                    _activeGamepadConfig = _config.GamepadConfigs.First(c => c.Id == _config.CurrentInputSource);
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
            switch (_currentInputMode)
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

            switch (_config.RetroSpyConfig.ControllerType)
            {
                case RetroSpyControllerType.NES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.NES.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _buttonInfos.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
                        }
                        break;
                    }
                case RetroSpyControllerType.SNES:
                    {
                        foreach (var mapping in _config.RetroSpyConfig.SNES.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
                        {
                            _buttonInfos.Add(mapping.ButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
                        }
                        break;
                    }
            }
        }

        private void InitGamepadButtons()
        {
            _buttonInfos.Clear();
            foreach (var mapping in _activeGamepadConfig.ButtonMappings.Where(m => m.IsVisible).OrderBy(m => m.Order))
            {
                _buttonInfos.Add(mapping.MappedButtonType.ToString(), new ButtonStateHistory() { Color = mapping.Color, Label = mapping.Label });
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private float CalcMinAge()
        {
            var lineMs = _config.DisplayConfig.LineLength / _pixelsPerMs;
            _minAge = DateTime.Now.AddMilliseconds(-lineMs);
            return lineMs;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_listeningForInput)
            {
                CheckForListeningInput();
            }
            else
            {
                if (_currentInputMode == InputMode.Gamepad)
                {
                    ReadGamepadInputs();
                }

                foreach (var button in _buttonInfos)
                {
                    _frequencyDict[button.Key] = button.Value.GetPressedLastSecond();
                }

                var lineMs = CalcMinAge();
                _minAge = DateTime.Now.AddMilliseconds(-lineMs);
                _purgeTimer += gameTime.ElapsedGameTime;
                if (_purgeTimer.Milliseconds > 200)
                {
                    foreach (var button in _buttonInfos.Values)
                    {
                        button.RemoveOldStateChanges(lineMs + _config.DisplayConfig.TurnOffLineSpeed + 1000);
                    }
                    _purgeTimer = TimeSpan.Zero;
                }

                switch (_config.DisplayConfig.Layout)
                {
                    case LayoutStyle.Horizontal:
                        {
                            BuildRects();
                            break;
                        }
                    case LayoutStyle.Vertical:
                        {
                            BuildVerticalRects();
                            break;
                        }
                }
            }

            base.Update(gameTime);
        }

        private void CheckForListeningInput()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                _listeningForInput = false;
                _listeningCancelPressed = true;
                _listeningButton.Text = _listeningMapping.ButtonType.ToString();
                return;
            }

            var buttonDetected = ButtonType.NONE;
            if (GamePad.GetState(_currentPlayerIndex).DPad.Up == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.UP;
            }
            else if (GamePad.GetState(_currentPlayerIndex).DPad.Down == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.DOWN;
            }
            else if (GamePad.GetState(_currentPlayerIndex).DPad.Left == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.LEFT;
            }
            else if (GamePad.GetState(_currentPlayerIndex).DPad.Right == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.RIGHT;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.A == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.A;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.B == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.B;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.X == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.X;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.Y == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.Y;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.LeftShoulder == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.L;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.RightShoulder == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.R;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.Back == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.SELECT;
            }
            else if (GamePad.GetState(_currentPlayerIndex).Buttons.Start == ButtonState.Pressed)
            {
                buttonDetected = ButtonType.START;
            }

            if (buttonDetected != ButtonType.NONE)
            {
                _listeningMapping.MappedButtonType = buttonDetected;
                _listeningButton.Text = buttonDetected.ToString();

                foreach (var mapping in _activeGamepadConfig.ButtonMappings)
                {
                    if (mapping == _listeningMapping)
                    {
                        continue;
                    }
                    if (mapping.MappedButtonType == buttonDetected)
                    {
                        mapping.MappedButtonType = ButtonType.NONE;
                        var textBox = _listeningGrid.Widgets.OfType<TextButton>().FirstOrDefault(b => b.Tag == mapping);
                        if (textBox != null)
                        {
                            textBox.Text = mapping.MappedButtonType.ToString();
                        }
                    }
                }
                _listeningForInput = false;
            }
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
            var lineLength = _config.DisplayConfig.LineLength;

            var lineStart = DateTime.Now;

            foreach (var kvp in _buttonInfos)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;
                var baseX = 41;

                for (var i = info.StateChangeHistory.Count - 1; i >= 0; i--)
                {
                    if (!info.StateChangeHistory[i].IsPressed)
                    {
                        continue;
                    }

                    var endTime = info.StateChangeHistory[i].EndTime == DateTime.MinValue ? lineStart : info.StateChangeHistory[i].EndTime;

                    if (endTime < _minAge)
                    {
                        break;
                    }

                    var xOffset = (lineStart - endTime).TotalMilliseconds * _pixelsPerMs;
                    var startTime = info.StateChangeHistory[i].StartTime < _minAge ? _minAge : info.StateChangeHistory[i].StartTime;
                    var lengthInMs = (endTime - startTime).TotalMilliseconds;
                    var lengthInPixels = (lengthInMs * _pixelsPerMs);
                    if (lengthInPixels < 1)
                    {
                        lengthInPixels = 1;
                    }

                    var x = baseX + Math.Floor(xOffset);
                    var width = lengthInPixels;
                    var maxX = baseX + lineLength;

                    if (x + width >= maxX)
                    {
                        var overflow = (x + width) - maxX;
                        width -= overflow;
                    }

                    var rec = new Rectangle();
                    rec.X = (int)Math.Floor(x);
                    rec.Y = yPos - 2 - yOffset - 1;
                    rec.Width = (int)Math.Floor(width);
                    rec.Height = yOffset * 2 + 1;
                    _onRects[kvp.Key].Add(rec);
                }
                yPos += yInc;
            }
        }

        private void BuildVerticalRects()
        {
            var xPos = 18;
            var xInc = ROW_HEIGHT;
            var yOffset = 2;
            var lineLength = _config.DisplayConfig.LineLength;

            var lineStart = DateTime.Now;

            foreach (var kvp in _buttonInfos)
            {
                _onRects[kvp.Key].Clear();
                var info = kvp.Value;
                var baseY = 73;

                for (var i = info.StateChangeHistory.Count - 1; i >= 0; i--)
                {
                    if (!info.StateChangeHistory[i].IsPressed)
                    {
                        continue;
                    }

                    var endTime = info.StateChangeHistory[i].EndTime == DateTime.MinValue ? lineStart : info.StateChangeHistory[i].EndTime;

                    if (endTime < _minAge)
                    {
                        break;
                    }

                    var xOffset = (lineStart - endTime).TotalMilliseconds * _pixelsPerMs;
                    var startTime = info.StateChangeHistory[i].StartTime < _minAge ? _minAge : info.StateChangeHistory[i].StartTime;
                    var lengthInMs = (endTime - startTime).TotalMilliseconds;
                    var lengthInPixels = (lengthInMs * _pixelsPerMs);
                    if (lengthInPixels < 1)
                    {
                        lengthInPixels = 1;
                    }

                    var y = baseY + Math.Floor(xOffset);
                    var height = lengthInPixels;
                    var maxY = baseY + lineLength;

                    if (y + height >= maxY)
                    {
                        var overflow = (y + height) - maxY;
                        height -= overflow;
                    }

                    var rec = new Rectangle();
                    rec.Y = (int)Math.Floor(y);
                    rec.X = xPos - 2 - yOffset - 1;
                    rec.Height = (int)Math.Floor(height);
                    rec.Width = yOffset * 2 + 1;
                    _onRects[kvp.Key].Add(rec);
                }
                xPos += xInc;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_config.DisplayConfig.BackgroundColor);

            var matrix = Matrix.CreateScale(1.5f, 1.5f, 1.5f);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, matrix);
            switch (_config.DisplayConfig.Layout)
            {
                case LayoutStyle.Horizontal:
                    {
                        DrawButtons();
                        DrawQueues();
                        break;
                    }
                case LayoutStyle.Vertical:
                    {
                        DrawVerticalButtons();
                        break;
                    }
            }
            _spriteBatch.End();

            try
            {
                _desktop.Render();
            }
            catch (Exception)
            {

            }

            base.Draw(gameTime);
        }

        private void DrawButtons()
        {
            var yPos = 35;
            var yInc = ROW_HEIGHT;
            var rightMargin = 10;

            foreach (var kvp in _buttonInfos)
            {
                _spriteBatch.DrawString(_bitmapFont2, kvp.Value.Label, new Vector2(rightMargin, yPos), Color.White);
                yPos += yInc;
            }
        }

        private void DrawVerticalButtons()
        {
            var yPos = 35;
            var xInc = ROW_HEIGHT;
            var xPos = 10;
            var rec = Rectangle.Empty;
            var lineLength = _config.DisplayConfig.LineLength;

            foreach (var kvp in _buttonInfos)
            {
                var info = kvp.Value;
                var semiTransFactor = kvp.Value.StateChangeHistory.Any() ? 1.0f : 0.3f;
                var innerBoxSemiTransFactor = kvp.Value.StateChangeHistory.Any() ? 0.75f : 0.25f;

                _spriteBatch.DrawString(_bitmapFont2, kvp.Value.Label, new Vector2(xPos, yPos), Color.White);

                //empty button press rectangle
                rec.X = xPos - 1;
                rec.Y = yPos + 25;
                rec.Width = 13;
                rec.Height = 13;
                _spriteBatch.Draw(_pixel, rec, null, info.Color * semiTransFactor, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                rec.X = xPos;
                rec.Y = yPos + 26;
                rec.Width = 11;
                rec.Height = 11;
                _spriteBatch.Draw(_pixel, rec, null, Color.Black * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);

                //draw entire off line
                if (_config.DisplayConfig.DrawIdleLines)
                {
                    rec.X = xPos + 5;
                    rec.Y = yPos + 38;
                    rec.Height = lineLength - 1;
                    rec.Width = 1;
                    _spriteBatch.Draw(_pixel, rec, null, info.Color * semiTransFactor, _horizontalAngle, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                foreach (var rect in _onRects[kvp.Key])
                {
                    _spriteBatch.Draw(_pixel, rect, null, info.Color, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                if (info.IsPressed())
                {
                    //fill in button rect
                    rec.X = xPos - 1;
                    rec.Y = yPos + 25;
                    rec.Width = 12;
                    rec.Height = 12;
                    _spriteBatch.Draw(_pixel, rec, null, info.Color * 0.75f, 0, new Vector2(0, 0), SpriteEffects.None, 0);
                }

                xPos += xInc;
            }
        }

        private void DrawQueues()
        {
            var yPos = 52;
            var yInc = ROW_HEIGHT;
            var baseX = 41;
            var lineLength = _config.DisplayConfig.LineLength;
            var infoX = baseX + lineLength + 5;

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
                if (_config.DisplayConfig.DrawIdleLines)
                {
                    rec.X = baseX;
                    rec.Y = yPos - 3;
                    rec.Width = lineLength - 1;
                    rec.Height = 1;
                    _spriteBatch.Draw(_pixel, rec, null, info.Color * semiTransFactor, _horizontalAngle, new Vector2(0, 0), SpriteEffects.None, 0);
                }

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

                    if (_config.DisplayConfig.DisplayDuration)
                    {
                        var elapsed = info.PressedElapsed();
                        if (elapsed.TotalSeconds > _config.DisplayConfig.MinDisplayDuration)
                        {
                            _spriteBatch.DrawString(_bitmapFont2, elapsed.ToString("ss':'f"), new Vector2(infoX, yPos - 17), info.Color);
                        }
                    }
                }

                if (_config.DisplayConfig.DisplayFrequency)
                {
                    if (_frequencyDict[kvp.Key] >= _config.DisplayConfig.MinDisplayFrequency)
                    {
                        _spriteBatch.DrawString(_bitmapFont2, $"x{_frequencyDict[kvp.Key]}", new Vector2(infoX, yPos - 17), info.Color);
                    }
                }
                yPos += yInc;
            }
        }
    }
}