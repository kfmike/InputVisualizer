using Myra.Graphics2D.UI;
using Myra;
using System;
using System.Collections.Generic;
using System.Linq;
using Myra.Graphics2D;
using Microsoft.Xna.Framework;
using InputVisualizer.Config;
using Myra.Graphics2D.UI.ColorPicker;
using System.IO.Ports;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.Brushes;
using InputVisualizer.RetroSpy;
using System.Timers;

namespace InputVisualizer.UI
{
    public class GameUI
    {
        private Desktop _desktop;
        private ViewerConfig _config;
        private GameState _gameState;

        private bool _listeningForInput = false;
        private bool _listeningForInputEnabled = true;
        private bool _listeningCancelPressed = false;
        private ButtonMapping _listeningMapping;
        private TextButton _listeningButton;
        private Grid _listeningGrid;
        private string _originalListeningButtonText;
        private const int MAX_MAP_BUTTON_LENGTH = 20;

        private TextBox _misterHostnameTextBox;
        private TextBox _misterUsernameTextBox;
        private TextBox _misterPasswordTextBox;
        private ComboBox _misterControllerComboBox;
        private TextButton _misterConnectButton;

        private HorizontalStackPanel _mainMenuContainer;
        private Dialog _waitMessageBox = null;
        private Timer _listeningCooldownTimer;

        public bool ListeningForInput => _listeningForInput;
        private JoystickState _initialJoystickState;

        public event EventHandler<InputSourceChangedEventArgs> InputSourceChanged;
        public event EventHandler GamepadSettingsUpdated;
        public event EventHandler RetroSpySettingsUpdated;
        public event EventHandler Usb2SnesSettingsUpdated;
        public event EventHandler MisterSettingsUpdated;
        public event EventHandler MisterConnectionRequested;
        public event EventHandler DisplaySettingsUpdated;
        public event EventHandler GeneralSettingsUpdated;
        public event EventHandler<Usb2SnesGameChangedEventArgs> Usb2SnesGameChanged;
        public event EventHandler RefreshInputSources;

        public GameUI(Game game, ViewerConfig config, GameState gameState)
        {
            MyraEnvironment.Game = game;
            _config = config;
            _gameState = gameState;

            _listeningCooldownTimer = new Timer();
            _listeningCooldownTimer.Elapsed += ListeningCooldownTimerElapsed;
            _listeningCooldownTimer.Interval = 200;
            _listeningCooldownTimer.AutoReset = false;
            _listeningCooldownTimer.Enabled = false;
        }

        private void ListeningCooldownTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _listeningForInputEnabled = true;
        }

        public void Init(Dictionary<string, SystemGamePadInfo> systemGamepads, Dictionary<string, SystemJoyStickInfo> systemJoysticks, List<string> usb2SnesDevices, Usb2SnesGameList usb2SnesGameList)
        {
            _mainMenuContainer = new HorizontalStackPanel();

            UpdateMainMenu(systemGamepads, systemJoysticks, usb2SnesDevices, usb2SnesGameList);

            _desktop = new Desktop
            {
                Root = _mainMenuContainer
            };
            _desktop.Root.VerticalAlignment = VerticalAlignment.Top;
            _desktop.Root.HorizontalAlignment = HorizontalAlignment.Left;
        }

        public void UpdateMainMenu(Dictionary<string, SystemGamePadInfo> systemGamepads, Dictionary<string, SystemJoyStickInfo> systemJoysticks, List<string> usb2SnesDevices, Usb2SnesGameList usb2SnesGameList)
        {
            _mainMenuContainer.Widgets.Clear();

            var usb2SnesGameListCombo = new ComboBox
            {
                Padding = new Thickness(3),
                AcceptsKeyboardFocus = false,
                Border = new SolidBrush(Color.DarkGray),
                BorderThickness = new Thickness(1),

            };
            foreach (var game in usb2SnesGameList.Games)
            {
                var name = game.Name.Length > 32 ? game.Name.Substring(0, 32) : game.Name;
                usb2SnesGameListCombo.Items.Add(new ListItem(name, Color.White));
            }
            usb2SnesGameListCombo.SelectedItem = usb2SnesGameListCombo.Items[0];

            usb2SnesGameListCombo.SelectedIndexChanged += (s, a) =>
            {
                if (Usb2SnesGameChanged != null)
                {
                    var args = new Usb2SnesGameChangedEventArgs() { Game = usb2SnesGameListCombo.SelectedItem.Text };
                    Usb2SnesGameChanged(this, args);
                }
            };

            usb2SnesGameListCombo.Visible = !string.IsNullOrEmpty(_config.CurrentInputSource) && _config.CurrentInputSource.Contains("usb2snes");

            var inputSourceCombo = new ComboBox
            {
                Padding = new Thickness(3),
                AcceptsKeyboardFocus = false,
                Border = new SolidBrush(Color.DarkGray),
                BorderThickness = new Thickness(1),
            };

            foreach (var kvp in systemGamepads)
            {
                var name = kvp.Value.Name.Length > 32 ? kvp.Value.Name.Substring(0, 32) : kvp.Value.Name;
                inputSourceCombo.Items.Add(new ListItem(name, Color.White, kvp.Key));
            }
            foreach (var kvp in systemJoysticks)
            {
                var name = kvp.Value.Name.Length > 32 ? kvp.Value.Name.Substring(0, 32) : kvp.Value.Name;
                inputSourceCombo.Items.Add(new ListItem(name, Color.White, kvp.Key));
            }
            inputSourceCombo.Items.Add(new ListItem("RetroSpy", Color.White, "spy"));
            inputSourceCombo.Items.Add(new ListItem("RetroSpy MiSTer", Color.White, "mister"));
            inputSourceCombo.Items.Add(new ListItem("Keyboard/Mouse", Color.White, "keyboard"));

            foreach (var device in usb2SnesDevices)
            {
                inputSourceCombo.Items.Add(new ListItem($"USB2SNES: '{device}'", Color.White, $"usb2snes||{device}"));
            }

            foreach (var item in inputSourceCombo.Items)
            {
                if (_config.CurrentInputSource == (string)item.Tag)
                {
                    inputSourceCombo.SelectedItem = item;
                }
            }

            inputSourceCombo.SelectedIndexChanged += (s, a) =>
            {
                if (InputSourceChanged != null)
                {
                    var args = new InputSourceChangedEventArgs() { InputSourceId = (string)inputSourceCombo.SelectedItem.Tag };
                    usb2SnesGameListCombo.Visible = args.InputSourceId.Contains("usb2snes");
                    InputSourceChanged(this, args);
                }
            };

            var menuBar = new HorizontalMenu()
            {
                Padding = new Thickness(3),
                AcceptsKeyboardFocus = false,
                Border = new SolidBrush(Color.DarkGray),
                BorderThickness = new Thickness(1),
            };

            var menuItemGeneral = new MenuItem();
            menuItemGeneral.Text = "General Settings";
            menuItemGeneral.Id = "menuItemGeneral";

            menuItemGeneral.Selected += (s, a) =>
            {
                ShowGeneralSettingsDialog();
            };

            var menuItemInputs = new MenuItem();
            menuItemInputs.Text = "Configure Current Input";
            menuItemInputs.Id = "menuItemInputs";
            menuItemInputs.Selected += (s, a) =>
            {
                if (_gameState.CurrentInputMode == InputMode.RetroSpy)
                {
                    ShowConfigureRetroSpyDialog();
                }
                else if (_gameState.CurrentInputMode == InputMode.MiSTer)
                {
                    ShowConfigureMisterDialog();
                }
                else if (_gameState.CurrentInputMode == InputMode.XInputOrKeyboard)
                {
                    ShowConfigureGamePadDialog(systemGamepads);
                }
                else if (_gameState.CurrentInputMode == InputMode.DirectInput)
                {
                    ShowConfigureJoystickDialog(systemJoysticks);
                }
                else
                {
                    ShowConfigureUsb2SnesDialog();
                }
            };

            var menuItemDisplay = new MenuItem();
            menuItemDisplay.Text = "Configure Display";
            menuItemDisplay.Id = "menuItemDisplay";

            menuItemDisplay.Selected += (s, a) =>
            {
                ShowConfigureDisplayDialog();
            };

            var menuItemRefresh = new MenuItem();
            menuItemRefresh.Text = "Refresh Input Sources";
            menuItemRefresh.Id = "menuItemRefresh";

            menuItemRefresh.Selected += (s, a) =>
            {
                RefreshInputSources?.Invoke(this, new EventArgs());
            };

            var menuItemAbout = new MenuItem();
            menuItemAbout.Text = "About";
            menuItemAbout.Id = "menuItemAbout";
            menuItemAbout.Selected += (s, a) =>
            {
                ShowAboutDialog();
            };

            var menuItemActions = new MenuItem();
            menuItemActions.Text = "Menu";
            menuItemActions.Id = "menuItemActions";
            menuItemActions.Items.Add(menuItemGeneral);
            menuItemActions.Items.Add(menuItemInputs);
            menuItemActions.Items.Add(menuItemDisplay);
            menuItemActions.Items.Add(menuItemRefresh);
            menuItemActions.Items.Add(menuItemAbout);

            menuBar.Items.Add(menuItemActions);

            _mainMenuContainer.Widgets.Add(inputSourceCombo);
            _mainMenuContainer.Widgets.Add(usb2SnesGameListCombo);
            _mainMenuContainer.Widgets.Add(menuBar);
        }

        public void UpdateMisterConfigureInputUI()
        {
            if (_misterConnectButton == null)
            {
                return;
            }

            if (_gameState.ConnectedToMister)
            {
                _misterConnectButton.Text = "Disconnect";
                _misterConnectButton.Background = new SolidBrush(Color.DarkGreen);
            }
            else
            {
                _misterConnectButton.Text = "Connect to MiSTer";
                _misterConnectButton.Background = null;
            }

            _misterHostnameTextBox.Enabled = !_gameState.ConnectedToMister;
            _misterUsernameTextBox.Enabled = !_gameState.ConnectedToMister;
            _misterPasswordTextBox.Enabled = !_gameState.ConnectedToMister;
            _misterControllerComboBox.Enabled = !_gameState.ConnectedToMister;
        }

        public void Render()
        {
            try
            {
                _desktop.Render();
            }
            catch (Exception)
            {

            }
        }

        public void CheckForListeningInput()
        {
            if (_gameState.CurrentInputMode == InputMode.DirectInput)
            {
                CheckForDirectInputListeningInput();
                return;
            }
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                _listeningForInput = false;
                _listeningCancelPressed = true;
                _listeningButton.Text = _originalListeningButtonText;
                return;
            }

            if (_gameState.CurrentInputMode != InputMode.XInputOrKeyboard)
            {
                return;
            }

            var keyDetected = Keys.None;
            var buttonDetected = ButtonType.NONE;
            var mouseButtonDetected = MouseButtonType.None;
            var state = GamePad.GetState(_gameState.CurrentPlayerIndex, GamePadDeadZone.Circular);
            var activeConfig = _gameState.ActiveGamepadConfig;

            var pressedKeys = keyboardState.GetPressedKeys();
            if (pressedKeys.Length > 0)
            {
                keyDetected = pressedKeys[0];
            }
            var mouseState = Mouse.GetState();
            if (mouseState.ToString() != "None")
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    mouseButtonDetected = MouseButtonType.LeftButton;
                }
                else if (mouseState.RightButton == ButtonState.Pressed)
                {
                    mouseButtonDetected = MouseButtonType.RightButton;
                }
                else if (mouseState.MiddleButton == ButtonState.Pressed)
                {
                    mouseButtonDetected = MouseButtonType.MiddleButton;
                }
                else if (mouseState.XButton1 == ButtonState.Pressed)
                {
                    mouseButtonDetected = MouseButtonType.XButton1;
                }
                else if (mouseState.XButton2 == ButtonState.Pressed)
                {
                    mouseButtonDetected = MouseButtonType.XButton2;
                }
            }

            if (keyDetected == Keys.None && mouseButtonDetected == MouseButtonType.None && !activeConfig.IsKeyboard)
            {
                if (activeConfig.UseLStickForDpad)
                {
                    var result = InputHelper.GetAnalogDpadMovement(state, _gameState.AnalogStickDeadZoneTolerance);
                    buttonDetected = result.LeftRight != ButtonType.NONE ? result.LeftRight : result.UpDown;
                }
                else
                {
                    if (state.DPad.Up == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.UP;
                    }
                    else if (state.DPad.Down == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.DOWN;
                    }
                    else if (state.DPad.Left == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.LEFT;
                    }
                    else if (state.DPad.Right == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.RIGHT;
                    }
                }

                if (buttonDetected == ButtonType.NONE)
                {
                    if (state.Buttons.A == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.CROSS : ButtonType.A;
                    }
                    else if (state.Buttons.B == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.CIRCLE : ButtonType.B;
                    }
                    else if (state.Buttons.X == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.SQUARE : ButtonType.X;
                    }
                    else if (state.Buttons.Y == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.TRIANGLE : ButtonType.Y;
                    }
                    else if (state.Buttons.LeftShoulder == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.L1 : ButtonType.L;
                    }
                    else if (state.Buttons.RightShoulder == ButtonState.Pressed)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.R1 : ButtonType.R;
                    }
                    else if (state.Triggers.Left > 0.0f)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.L2 : ButtonType.LT;
                    }
                    else if (state.Triggers.Right > 0.0f)
                    {
                        buttonDetected = activeConfig.Style == GamepadStyle.Playstation ? ButtonType.R2 : ButtonType.RT;
                    }
                    else if (state.Buttons.Back == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.SELECT;
                    }
                    else if (state.Buttons.Start == ButtonState.Pressed)
                    {
                        buttonDetected = ButtonType.START;
                    }
                }
            }

            if (buttonDetected != ButtonType.NONE)
            {
                _listeningMapping.MappingType = ButtonMappingType.Button;
                _listeningMapping.MappedButtonType = buttonDetected;
                _listeningMapping.MappedKey = Keys.None;
                _listeningMapping.MappedMouseButton = MouseButtonType.None;
                var buttonText = buttonDetected.ToString() + " Button";
                buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                _listeningButton.Text = buttonText;

                foreach (var mapping in activeConfig.ButtonMappingSet.ButtonMappings)
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
            else if (keyDetected != Keys.None)
            {
                _listeningMapping.MappingType = ButtonMappingType.Key;
                _listeningMapping.MappedButtonType = ButtonType.NONE;
                _listeningMapping.MappedMouseButton = MouseButtonType.None;
                _listeningMapping.MappedKey = keyDetected;
                var buttonText = keyDetected.ToString() + " Key";
                buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                _listeningButton.Text = buttonText;

                foreach (var mapping in activeConfig.ButtonMappingSet.ButtonMappings)
                {
                    if (mapping == _listeningMapping)
                    {
                        continue;
                    }
                    if (mapping.MappedKey == keyDetected)
                    {
                        mapping.MappedKey = Keys.None;
                        mapping.MappingType = ButtonMappingType.Button;
                        var textBox = _listeningGrid.Widgets.OfType<TextButton>().FirstOrDefault(b => b.Tag == mapping);
                        if (textBox != null)
                        {
                            textBox.Text = mapping.MappedKey.ToString();
                        }
                    }
                }
                _listeningForInput = false;
            }
            else if (mouseButtonDetected != MouseButtonType.None)
            {
                _listeningMapping.MappingType = ButtonMappingType.Mouse;
                _listeningMapping.MappedButtonType = ButtonType.NONE;
                _listeningMapping.MappedMouseButton = mouseButtonDetected;
                _listeningMapping.MappedKey = Keys.None;
                var buttonText = "Mouse " + mouseButtonDetected.ToString();
                buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                _listeningButton.Text = buttonText;

                foreach (var mapping in activeConfig.ButtonMappingSet.ButtonMappings)
                {
                    if (mapping == _listeningMapping)
                    {
                        continue;
                    }
                    if (mapping.MappedMouseButton == mouseButtonDetected)
                    {
                        mapping.MappedMouseButton = MouseButtonType.None;
                        mapping.MappingType = ButtonMappingType.Button;
                        var textBox = _listeningGrid.Widgets.OfType<TextButton>().FirstOrDefault(b => b.Tag == mapping);
                        if (textBox != null)
                        {
                            textBox.Text = mapping.MappedMouseButton.ToString();
                        }
                    }
                }
                _listeningForInput = false;
            }

            if (!_listeningForInput)
            {
                _listeningForInputEnabled = false;
                _listeningCooldownTimer.Start();
            }
        }

        public void CheckForDirectInputListeningInput()
        {
            var buttonDetected = ButtonType.NONE;
            var state = Joystick.GetState(_gameState.CurrentJoystickIndex);
            var activeConfig = _gameState.ActiveJoystickConfig;

            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                _listeningForInput = false;
                _listeningCancelPressed = true;
                _listeningButton.Text = _originalListeningButtonText;
                return;
            }

            var hatIndex = -1;
            for (var i = 0; i < state.Hats.Length; i++)
            {
                if (state.Hats[i].Up == ButtonState.Pressed)
                {
                    buttonDetected = ButtonType.UP;
                }
                else if (state.Hats[i].Down == ButtonState.Pressed)
                {
                    buttonDetected = ButtonType.DOWN;
                }
                else if (state.Hats[i].Left == ButtonState.Pressed)
                {
                    buttonDetected = ButtonType.LEFT;
                }
                else if (state.Hats[i].Right == ButtonState.Pressed)
                {
                    buttonDetected = ButtonType.RIGHT;
                }

                if (buttonDetected != ButtonType.NONE)
                {
                    hatIndex = i;
                    break;
                }
            }

            var axisIndex = -1;
            var axisValueIsNegative = false;
            if (buttonDetected == ButtonType.NONE)
            {
                for (var i = 0; i < state.Axes.Length; i++)
                {
                    var delta = Math.Abs(state.Axes[i] - _initialJoystickState.Axes[i]);
                    if (delta > _gameState.DirectInputDeadZoneTolerance)
                    {
                        buttonDetected = _listeningMapping.ButtonType;
                        axisIndex = i;
                        axisValueIsNegative = state.Axes[i] < 0;
                        break;
                    }
                }
            }

            if (buttonDetected == ButtonType.NONE)
            {
                for (var i = 0; i < state.Buttons.Length; i++)
                {
                    if (state.Buttons[i] == ButtonState.Pressed)
                    {
                        buttonDetected = (ButtonType)Enum.Parse(typeof(ButtonType), $"B{i}");
                        break;
                    }
                }
            }

            if (buttonDetected != ButtonType.NONE)
            {
                _listeningMapping.MappingType = ButtonMappingType.Button;
                _listeningMapping.MappedButtonType = buttonDetected;
                _listeningMapping.MappedKey = Keys.None;
                _listeningMapping.MappedMouseButton = MouseButtonType.None;
                _listeningMapping.JoystickHatIndex = hatIndex;
                _listeningMapping.JoystickAxisIndex = axisIndex;
                _listeningMapping.JoystickAxisDirectionIsNegative = axisValueIsNegative;

                var buttonText = buttonDetected.ToString() + " Button";
                buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                _listeningButton.Text = buttonText;

                foreach (var mapping in activeConfig.ButtonMappingSet.ButtonMappings)
                {
                    if (mapping == _listeningMapping)
                    {
                        continue;
                    }
                    if (mapping.MappedButtonType == buttonDetected)
                    {
                        mapping.MappedButtonType = ButtonType.NONE;
                        mapping.JoystickHatIndex = -1;
                        mapping.JoystickAxisIndex = -1;
                        mapping.JoystickAxisDirectionIsNegative = false;
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

        public void CheckForMisterListeningInput(ControllerStateEventArgs e)
        {
            if (_gameState.CurrentInputMode != InputMode.MiSTer)
            {
                return;
            }

            var buttonDetected = ButtonType.NONE;
            if (_config.MisterConfig.UseLStickForDpad)
            {
                /* TODO */
            }
            else
            {
                foreach (var button in e.Buttons)
                {
                    if (button.Value)
                    {
                        if (Enum.IsDefined(typeof(ButtonType), button.Key))
                        {
                            buttonDetected = (ButtonType)Enum.Parse(typeof(ButtonType), button.Key);
                            break;
                        }
                    }
                }
            }

            if (buttonDetected != ButtonType.NONE)
            {
                _listeningForInput = false;
                _listeningMapping.MappingType = ButtonMappingType.Button;
                _listeningMapping.MappedButtonType = buttonDetected;
                _listeningMapping.MappedKey = Keys.None;
                _listeningMapping.MappedMouseButton = MouseButtonType.None;
                var buttonText = buttonDetected.ToString() + " Button";
                buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                _listeningButton.Text = buttonText;

                foreach (var mapping in _config.MisterConfig.GetCurrentMappingSet().ButtonMappings)
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
            }
        }

        public void ShowConfigureMisterDialog()
        {
            var buttonMapWidgets = new List<Widget>();

            var dialog = new Dialog
            {
                Title = "MiSTer Config",
                TitleTextColor = Color.DarkSeaGreen
            };

            var buttonConfigGrid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            buttonConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            buttonConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            buttonConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            buttonConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            buttonConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var visibleLabel = CreateLabel("Visible", 0, 0, 1, 1);
            var buttonLabel = CreateLabel("Button", 0, 1, 1, 1);
            var mappedToLabel = CreateLabel("Mapped To", 0, 2, 1, 1);
            var colorLabel = CreateLabel("Color", 0, 3, 1, 1);
            var orderLabel = CreateLabel("Order", 0, 4, 1, 2);
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;

            buttonConfigGrid.Widgets.Add(visibleLabel);
            buttonConfigGrid.Widgets.Add(buttonLabel);
            buttonConfigGrid.Widgets.Add(mappedToLabel);
            buttonConfigGrid.Widgets.Add(colorLabel);
            buttonConfigGrid.Widgets.Add(orderLabel);

            DrawButtonMappings(_config.MisterConfig.GetCurrentMappingSet().ButtonMappings, buttonConfigGrid, buttonMapWidgets, 1, showMapButton: true);

            var mainConfigGrid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            mainConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            mainConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            mainConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            mainConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            mainConfigGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var hostNameLabel = CreateLabel("Hostname", 0, 0, 1, 1, null, HorizontalAlignment.Right);
            mainConfigGrid.Widgets.Add(hostNameLabel);
            _misterHostnameTextBox = CreateTextBox(_config.MisterConfig.Hostname, 0, 1, 1, 3);
            _misterHostnameTextBox.Width = 250;
            _misterHostnameTextBox.TextChanged += (o, e) =>
            {
                _config.MisterConfig.Hostname = _misterHostnameTextBox.Text;
            };
            mainConfigGrid.Widgets.Add(_misterHostnameTextBox);
            var userNameLabel = CreateLabel("Username", 1, 0, 1, 1, null, HorizontalAlignment.Right);
            mainConfigGrid.Widgets.Add(userNameLabel);
            _misterUsernameTextBox = CreateTextBox(_config.MisterConfig.Username, 1, 1, 1, 1);
            _misterUsernameTextBox.Width = 150;
            _misterUsernameTextBox.TextChanged += (o, e) =>
            {
                _config.MisterConfig.Username = _misterUsernameTextBox.Text;
            };
            mainConfigGrid.Widgets.Add(_misterUsernameTextBox);
            var passwordLabel = CreateLabel("Password", 1, 2, 1, 1, null, HorizontalAlignment.Right);
            mainConfigGrid.Widgets.Add(passwordLabel);
            _misterPasswordTextBox = CreateTextBox(_config.MisterConfig.Password, 1, 3, 1, 1);
            _misterPasswordTextBox.Width = 150;
            _misterPasswordTextBox.TextChanged += (o, e) =>
            {
                _config.MisterConfig.Password = _misterPasswordTextBox.Text;
            };
            _misterPasswordTextBox.PasswordField = true;
            mainConfigGrid.Widgets.Add(_misterPasswordTextBox);

            var styleLabel = CreateLabel("Style", 2, 0, 1, 1, null, HorizontalAlignment.Right);
            mainConfigGrid.Widgets.Add(styleLabel);
            var styleCombo = CreateComboBox(2, 1, 1, 1);
            foreach (var value in _config.MisterConfig.ButtonMappingSets.Keys)
            {
                var item = new ListItem(value.GetDescription(), Color.White, value);
                styleCombo.Items.Add(item);
                if (_config.MisterConfig.Style == value)
                {
                    styleCombo.SelectedItem = item;
                }
            }
            styleCombo.SelectedIndexChanged += (o, e) =>
            {
                _config.MisterConfig.Style = (GamepadStyle)styleCombo.SelectedItem.Tag;
                DrawButtonMappings(_config.MisterConfig.GetCurrentMappingSet().ButtonMappings, buttonConfigGrid, buttonMapWidgets, 1, showMapButton: true);
            };
            mainConfigGrid.Widgets.Add(styleCombo);

            var controllerNumberLabel = CreateLabel("Controller", 2, 2, 1, 1, null, HorizontalAlignment.Right);
            mainConfigGrid.Widgets.Add(controllerNumberLabel);

            _misterControllerComboBox = CreateComboBox(2, 3, 1, 1);
            for (var i = 0; i < 10; i++)
            {
                var item = new ListItem(i.ToString(), Color.White, i);
                _misterControllerComboBox.Items.Add(item);
                if (_config.MisterConfig.Controller == i)
                {
                    _misterControllerComboBox.SelectedItem = item;
                }
            }
            _misterControllerComboBox.SelectedIndexChanged += (o, e) =>
            {
                _config.MisterConfig.Controller = (int)_misterControllerComboBox.SelectedItem.Tag;
            };
            mainConfigGrid.Widgets.Add(_misterControllerComboBox);

            _misterConnectButton = CreateButton("Connect to MiSTer", 3, 0, 1, 4);
            _misterConnectButton.HorizontalAlignment = HorizontalAlignment.Center;
            _misterConnectButton.Click += (s, a) =>
            {
                if (string.IsNullOrEmpty(_misterHostnameTextBox.Text))
                {
                    ShowMessage("MiSTer", "Hostname required");
                    return;
                }
                MisterConnectionRequested?.Invoke(this, new EventArgs());
            };
            mainConfigGrid.Widgets.Add(_misterConnectButton);

            UpdateMisterConfigureInputUI();

            var container = new VerticalStackPanel();
            container.Widgets.Add(mainConfigGrid);
            container.Widgets.Add(buttonConfigGrid);

            dialog.Content = container;
            dialog.ConfirmKey = Keys.None;
            dialog.CloseKey = Keys.None;
            dialog.ButtonCancel.Visible = false;
            dialog.Closed += (s, a) =>
            {
                MisterSettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureGamePadDialog(Dictionary<string, SystemGamePadInfo> systemGamepads)
        {
            var buttonMapWidgets = new List<Widget>();

            var gamePadName = _gameState.ActiveGamepadConfig.IsKeyboard ? "Keyboard" : systemGamepads[_gameState.ActiveGamepadConfig.Id].Name;
            var name = gamePadName.Length > 32 ? gamePadName.Substring(0, 32) : gamePadName;
            var dialog = new Dialog
            {
                Title = $"{name} Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var styleLabel = CreateLabel("Style:", 0, 0, 1, 3);
            grid.Widgets.Add(styleLabel);

            var styleCombo = CreateComboBox(0, 3, 1, 3);
            foreach (GamepadStyle value in Enum.GetValues(typeof(GamepadStyle)))
            {
                var item = new ListItem(value.GetDescription(), Color.White, value);
                styleCombo.Items.Add(item);
                if (_gameState.ActiveGamepadConfig.Style == value)
                {
                    styleCombo.SelectedItem = item;
                }
            }
            styleCombo.SelectedIndexChanged += (o, e) =>
            {
                _gameState.ActiveGamepadConfig.Style = (GamepadStyle)styleCombo.SelectedItem.Tag;
                _gameState.ActiveGamepadConfig.GenerateButtonMappings();
                DrawButtonMappings(_gameState.ActiveGamepadConfig.ButtonMappingSet.ButtonMappings, grid, buttonMapWidgets, 3, showMapButton: true);
            };
            grid.Widgets.Add(styleCombo);

            if (!_gameState.ActiveGamepadConfig.IsKeyboard)
            {
                var lStickAsDpadLabel = CreateLabel("Use Left Stick for Dpad:", 1, 0, 1, 3);
                grid.Widgets.Add(lStickAsDpadLabel);

                var lStockAsDpadCheck = new CheckBox
                {
                    IsChecked = _gameState.ActiveGamepadConfig.UseLStickForDpad,
                    GridRow = 1,
                    GridColumn = 3,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                lStockAsDpadCheck.Click += (s, e) =>
                {
                    _gameState.ActiveGamepadConfig.UseLStickForDpad = lStockAsDpadCheck.IsChecked;
                };
                grid.Widgets.Add(lStockAsDpadCheck);
            }

            var visibleLabel = CreateLabel("Visible", 2, 0, 1, 1);
            var buttonLabel = CreateLabel("Button", 2, 1, 1, 1);
            var mappedToLabel = CreateLabel("Mapped To", 2, 2, 1, 1);
            var colorLabel = CreateLabel("Color", 2, 3, 1, 1);
            var orderLabel = CreateLabel("Order", 2, 4, 1, 2);
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;

            grid.Widgets.Add(visibleLabel);
            grid.Widgets.Add(buttonLabel);
            grid.Widgets.Add(mappedToLabel);
            grid.Widgets.Add(colorLabel);
            grid.Widgets.Add(orderLabel);

            DrawButtonMappings(_gameState.ActiveGamepadConfig.ButtonMappingSet.ButtonMappings, grid, buttonMapWidgets, 3, showMapButton: true);

            dialog.Content = grid;
            dialog.ConfirmKey = Keys.None;
            dialog.CloseKey = Keys.None;
            dialog.ButtonCancel.Visible = false;
            dialog.Closing += (s, a) =>
            {
                if (_listeningForInput)
                {
                    var messageBox = Dialog.CreateMessageBox("Button Mapping", "Finish mapping button or hit ESC to cancel");
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

                GamepadSettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureJoystickDialog(Dictionary<string, SystemJoyStickInfo> systemJoysticks)
        {
            var buttonMapWidgets = new List<Widget>();

            var joystickName = systemJoysticks[_gameState.ActiveJoystickConfig.Id].Name;
            var name = joystickName.Length > 32 ? joystickName.Substring(0, 32) : joystickName;
            var dialog = new Dialog
            {
                Title = $"{name} Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var styleLabel = CreateLabel("Style:", 0, 0, 1, 3);
            grid.Widgets.Add(styleLabel);

            var styleCombo = CreateComboBox(0, 3, 1, 3);
            foreach (GamepadStyle value in Enum.GetValues(typeof(GamepadStyle)))
            {
                var item = new ListItem(value.GetDescription(), Color.White, value);
                styleCombo.Items.Add(item);
                if (_gameState.ActiveJoystickConfig.Style == value)
                {
                    styleCombo.SelectedItem = item;
                }
            }
            styleCombo.SelectedIndexChanged += (o, e) =>
            {
                _gameState.ActiveJoystickConfig.Style = (GamepadStyle)styleCombo.SelectedItem.Tag;
                _gameState.ActiveJoystickConfig.GenerateButtonMappings();
                DrawButtonMappings(_gameState.ActiveJoystickConfig.ButtonMappingSet.ButtonMappings, grid, buttonMapWidgets, 3, showMapButton: true);
            };
            grid.Widgets.Add(styleCombo);

            var visibleLabel = CreateLabel("Visible", 2, 0, 1, 1);
            var buttonLabel = CreateLabel("Button", 2, 1, 1, 1);
            var mappedToLabel = CreateLabel("Mapped To", 2, 2, 1, 1);
            var colorLabel = CreateLabel("Color", 2, 3, 1, 1);
            var orderLabel = CreateLabel("Order", 2, 4, 1, 2);
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;

            grid.Widgets.Add(visibleLabel);
            grid.Widgets.Add(buttonLabel);
            grid.Widgets.Add(mappedToLabel);
            grid.Widgets.Add(colorLabel);
            grid.Widgets.Add(orderLabel);

            DrawButtonMappings(_gameState.ActiveJoystickConfig.ButtonMappingSet.ButtonMappings, grid, buttonMapWidgets, 3, showMapButton: true);

            dialog.Content = grid;
            dialog.ConfirmKey = Keys.None;
            dialog.CloseKey = Keys.None;
            dialog.ButtonCancel.Visible = false;
            dialog.Closing += (s, a) =>
            {
                if (_listeningForInput)
                {
                    var messageBox = Dialog.CreateMessageBox("Button Mapping", "Finish mapping button or hit ESC to cancel");
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

                GamepadSettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureRetroSpyDialog()
        {
            var buttonMapWidgets = new List<Widget>();

            var dialog = new Dialog
            {
                Title = "RetroSpy Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var comPortLabel = CreateLabel("COM Port:", 0, 0, 1, 1);
            grid.Widgets.Add(comPortLabel);

            var comPortCombo = CreateComboBox(0, 1, 1, 4);
            comPortCombo.HorizontalAlignment = HorizontalAlignment.Right;
            foreach (var name in SerialPort.GetPortNames())
            {
                var item = new ListItem(name, Color.White, name);
                comPortCombo.Items.Add(item);
                if (string.Equals(_config.RetroSpyConfig.ComPortName, name, StringComparison.OrdinalIgnoreCase))
                {
                    comPortCombo.SelectedItem = item;
                }
            }
            grid.Widgets.Add(comPortCombo);

            var styleLabel = CreateLabel("Style:", 1, 0, 1, 1);
            grid.Widgets.Add(styleLabel);

            var styleCombo = CreateComboBox(1, 1, 1, 4);
            styleCombo.HorizontalAlignment = HorizontalAlignment.Right;
            foreach (RetroSpyControllerType value in Enum.GetValues(typeof(RetroSpyControllerType)))
            {
                var item = new ListItem(value.GetDescription(), Color.White, value);
                styleCombo.Items.Add(item);
                if (_config.RetroSpyConfig.ControllerType == value)
                {
                    styleCombo.SelectedItem = item;
                }
            }
            styleCombo.SelectedIndexChanged += (o, e) =>
            {
                _config.RetroSpyConfig.ControllerType = (RetroSpyControllerType)styleCombo.SelectedItem.Tag;
                DrawButtonMappings(_config.RetroSpyConfig.GetMappingSet(_config.RetroSpyConfig.ControllerType).ButtonMappings, grid, buttonMapWidgets, 3);
            };
            grid.Widgets.Add(styleCombo);

            var visibleLabel = CreateLabel("Visible", 2, 0, 1, 1);
            var buttonLabel = CreateLabel("Button", 2, 1, 1, 1);
            var colorLabel = CreateLabel("Color", 2, 2, 1, 1);
            var orderLabel = CreateLabel("Order", 2, 3, 1, 2);
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;

            grid.Widgets.Add(visibleLabel);
            grid.Widgets.Add(buttonLabel);
            grid.Widgets.Add(colorLabel);
            grid.Widgets.Add(orderLabel);

            DrawButtonMappings(_config.RetroSpyConfig.GetMappingSet(_config.RetroSpyConfig.ControllerType).ButtonMappings, grid, buttonMapWidgets, 3);

            dialog.Content = grid;
            dialog.ConfirmKey = Keys.None;
            dialog.CloseKey = Keys.None;
            dialog.ButtonCancel.Visible = false;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                if (comPortCombo.SelectedItem != null)
                {
                    _config.RetroSpyConfig.ComPortName = (string)comPortCombo.SelectedItem.Tag;
                }
                _config.RetroSpyConfig.ControllerType = (RetroSpyControllerType)styleCombo.SelectedItem.Tag;

                RetroSpySettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureUsb2SnesDialog()
        {
            var buttonMapWidgets = new List<Widget>();

            var dialog = new Dialog
            {
                Title = "USB2SNES Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var visibleLabel = CreateLabel("Visible", 0, 0, 1, 1);
            var buttonLabel = CreateLabel("Button", 0, 1, 1, 1);
            var colorLabel = CreateLabel("Color", 0, 2, 1, 1);
            var orderLabel = CreateLabel("Order", 0, 3, 1, 2);
            orderLabel.HorizontalAlignment = HorizontalAlignment.Center;

            grid.Widgets.Add(visibleLabel);
            grid.Widgets.Add(buttonLabel);
            grid.Widgets.Add(colorLabel);
            grid.Widgets.Add(orderLabel);

            DrawButtonMappings(_config.Usb2SnesConfig.ButtonMappingSet.ButtonMappings, grid, buttonMapWidgets, 1);

            dialog.Content = grid;
            dialog.ConfirmKey = Keys.None;
            dialog.CloseKey = Keys.None;
            dialog.ButtonCancel.Visible = false;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                Usb2SnesSettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void DrawButtonMappings(List<ButtonMapping> mappings, Grid grid, List<Widget> currentWidgets, int gridStartRow, bool showMapButton = false)
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
                    GridColumn = currColumn,
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                visibleCheck.Click += (s, e) =>
                {
                    mapping.IsVisible = visibleCheck.IsChecked;
                };
                currentWidgets.Add(visibleCheck);
                currColumn++;

                var buttonLabel = CreateLabel(mapping.ButtonType.ToString(), currGridRow, currColumn, 1, 1);
                buttonLabel.Padding = new Thickness(2);
                currentWidgets.Add(buttonLabel);
                currColumn++;

                if (showMapButton)
                {
                    var listenPrompt = "Press Button";
                    if (_gameState.CurrentInputMode == InputMode.XInputOrKeyboard && _gameState.ActiveGamepadConfig != null)
                    {
                        listenPrompt = _gameState.ActiveGamepadConfig.IsKeyboard ? "Press Key/Mouse..." : "Press Button/Key/Mouse...";
                    }
                    var buttonText = mapping.MappingType == ButtonMappingType.Button ?
                        mapping.MappedButtonType.ToString() + " Button" :
                        mapping.MappingType == ButtonMappingType.Mouse ?
                        "Mouse " + mapping.MappedMouseButton.ToString() :
                        mapping.MappedKey.ToString() + " Key";
                    buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                    var mapButton = CreateButton(buttonText, currGridRow, currColumn, 1, 1);

                    mapButton.Width = 225;
                    mapButton.Tag = mapping;
                    mapButton.Click += (s, e) =>
                    {
                        if (_gameState.CurrentInputMode == InputMode.MiSTer && !_gameState.ConnectedToMister)
                        {
                            var messageBox = Dialog.CreateMessageBox("Button Mapping", "Connect to MiSTer before mapping buttons");
                            messageBox.ShowModal(_desktop);
                            return;
                        }
                        if (_listeningForInput)
                        {
                            var messageBox = Dialog.CreateMessageBox("Button Mapping", "Finish mapping button or hit ESC to cancel");
                            messageBox.ShowModal(_desktop);
                            return;
                        }

                        if (!_listeningForInputEnabled)
                        {
                            return;
                        }

                        if (_gameState.CurrentInputMode == InputMode.DirectInput && _gameState.ActiveJoystickConfig != null)
                        {
                            _initialJoystickState = Joystick.GetState(_gameState.CurrentJoystickIndex);
                        }
                        _listeningForInput = true;
                        _listeningButton = mapButton;
                        _originalListeningButtonText = _listeningButton.Text;
                        _listeningButton.Text = listenPrompt;
                        _listeningMapping = mapping;
                        _listeningGrid = grid;
                    };

                    currentWidgets.Add(mapButton);
                    currColumn++;
                }

                var colorButton = CreateButton("Color", currGridRow, currColumn, 1, 1);
                colorButton.TextColor = mapping.Color;
                colorButton.Click += (s, e) =>
                {
                    ChooseColor(mapping, colorButton);
                };
                currentWidgets.Add(colorButton);
                currColumn++;

                if (currGridRow > gridStartRow)
                {
                    var upButton = CreateButton("↑", currGridRow, currColumn, 1, 1);
                    upButton.Width = 30;
                    upButton.HorizontalAlignment = HorizontalAlignment.Right;
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
                    var downButton = CreateButton("↓", currGridRow, currColumn, 1, 1);
                    downButton.Width = 30;
                    downButton.HorizontalAlignment = HorizontalAlignment.Left;
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

        private static List<ButtonMapping> UpdateOrder(List<ButtonMapping> mappings, ButtonMapping targetMapping, bool goUp)
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

        private void ShowAboutDialog()
        {
            var dialog = new Dialog
            {
                Title = "About",
                TitleTextColor = Color.DarkSeaGreen
            };

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8,
                Padding = new Thickness(3),
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var aboutLabels = new List<Label>
            {
                CreateLabel("Version:", 0, 0, 1, 1),
                CreateLabel("1.6.5", 0, 1, 1, 1),
                CreateLabel("Author:", 1, 0, 1, 1),
                CreateLabel("KungFusedMike", 1, 1, 1, 1),
                CreateLabel("Email:", 2, 0, 1, 1),
                CreateLabel("kungfusedmike@gmail.com", 2, 1, 1, 1)
            };

            foreach (var label in aboutLabels)
            {
                label.HorizontalAlignment = HorizontalAlignment.Left;
                grid.Widgets.Add(label);
            }

            dialog.Content = grid;
            dialog.ButtonCancel.Visible = false;
            dialog.ShowModal(_desktop);
        }

        private void ShowGeneralSettingsDialog()
        {
            var dialog = new Dialog
            {
                Title = "General Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var serverLabel = CreateLabel("USB2SNES Server:", 0, 0, 1, 1);
            grid.Widgets.Add(serverLabel);
            var serverTextBox = CreateTextBox(_config.GeneralSettings.Usb2SnesServer.ToString(), 0, 1, 1, 1);
            serverTextBox.Width = 200;
            grid.Widgets.Add(serverTextBox);

            var serverPort = CreateLabel("Port:", 0, 2, 1, 1);
            grid.Widgets.Add(serverPort);
            var portTextBox = CreateTextBox(_config.GeneralSettings.Usb2SnesPort.ToString(), 0, 3, 1, 1);
            portTextBox.Width = 50;
            grid.Widgets.Add(portTextBox);

            dialog.Content = grid;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                _config.GeneralSettings.Usb2SnesServer = serverTextBox.Text;
                _config.GeneralSettings.Usb2SnesPort = portTextBox.Text;

                GeneralSettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        private void ShowConfigureDisplayDialog()
        {
            var dialog = new Dialog
            {
                Title = "Display Config",
                TitleTextColor = Color.DarkSeaGreen
            };

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
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

            var layoutLabel = CreateLabel("Layout", 0, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(layoutLabel);

            var layoutStyle = CreateLabel("Style", 1, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(layoutStyle);
            var layoutCombo = CreateComboBox(1, 2, 1, 1);
            foreach (DisplayLayoutStyle value in Enum.GetValues(typeof(DisplayLayoutStyle)))
            {
                var item = new ListItem(value.GetDescription(), Color.White, value);
                layoutCombo.Items.Add(item);
                if (_config.DisplayConfig.Layout == value)
                {
                    layoutCombo.SelectedItem = item;
                }
            }
            grid.Widgets.Add(layoutCombo);
            var maxContainerLabel = CreateLabel("Max Lines", 2, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(maxContainerLabel);
            var maxContainerCombo = CreateComboBox(2, 2, 1, 1);
            for (var i = 0; i < 10; i++)
            {
                var item = new ListItem(i == 0 ? "All" : i.ToString(), Color.White, i);
                maxContainerCombo.Items.Add(item);
                if (_config.DisplayConfig.MaxContainers == i)
                {
                    maxContainerCombo.SelectedItem = item;
                }
            }
            grid.Widgets.Add(maxContainerCombo);

            var colorsLabel = CreateLabel("Colors", 3, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(colorsLabel);

            var backgroundLabel = CreateLabel("Background", 4, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(backgroundLabel);
            var colorButton = CreateButton("Color", 4, 2, 1, 1);
            colorButton.TextColor = _config.DisplayConfig.BackgroundColor;
            colorButton.Click += (s, e) =>
            {
                ChooseColor(colorButton);
            };
            grid.Widgets.Add(colorButton);

            var emptyContainerColorLabel = CreateLabel("Empty Container", 5, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(emptyContainerColorLabel);

            var emptyContainerColorButton = CreateButton("Color", 5, 2, 1, 1);
            emptyContainerColorButton.TextColor = _config.DisplayConfig.EmptyContainerColor;
            emptyContainerColorButton.Click += (s, e) =>
            {
                ChooseColor(emptyContainerColorButton);
            };
            grid.Widgets.Add(emptyContainerColorButton);

            var illegalInputColorLabel = CreateLabel("Illegal Inputs", 6, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(illegalInputColorLabel);

            var illegalInputColorButton = CreateButton("Color", 6, 2, 1, 1);
            illegalInputColorButton.TextColor = _config.DisplayConfig.IllegalInputColor;
            illegalInputColorButton.Click += (s, e) =>
            {
                ChooseColor(illegalInputColorButton);
            };
            grid.Widgets.Add(illegalInputColorButton);

            var linesLabel = CreateLabel("Lines", 7, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(linesLabel);

            var showIdleLabel = CreateLabel("Idle Lines Enabled", 8, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(showIdleLabel);
            var displayIdleLindesCheck = new CheckBox()
            {
                GridRow = 8,
                GridColumn = 2,
                IsChecked = _config.DisplayConfig.DrawIdleLines
            };
            grid.Widgets.Add(displayIdleLindesCheck);

            var lineLengthLabel = CreateLabel("Length", 9, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(lineLengthLabel);

            var displayWidthText = CreateTextBox(_config.DisplayConfig.LineLength.ToString(), 9, 2, 1, 1);
            displayWidthText.Width = 50;
            grid.Widgets.Add(displayWidthText);

            var speedLabel = CreateLabel("Speed", 10, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(speedLabel);
            var speedMinValueLabel = CreateLabel("Slow", 10, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(speedMinValueLabel);
            var displaySpeedSpin = new HorizontalSlider()
            {
                GridRow = 10,
                GridColumn = 2,
                GridColumnSpan = 1,
                Value = _config.DisplayConfig.Speed,
                Minimum = 1,
                Maximum = 11,
                Width = 150
            };
            grid.Widgets.Add(displaySpeedSpin);
            var speedMaxValueLabel = CreateLabel("Fast", 10, 3, 1, 1);
            grid.Widgets.Add(speedMaxValueLabel);

            var dimLineLabel = CreateLabel("Dim Speed", 11, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(dimLineLabel);

            var dimLineLabelMinValueLabel = CreateLabel("Instant", 11, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(dimLineLabelMinValueLabel);
            var turnOffLineSpeedSpin = new HorizontalSlider()
            {
                GridRow = 11,
                GridColumn = 2,
                GridColumnSpan = 1,
                Value = _config.DisplayConfig.TurnOffLineSpeed / 50.0f,
                Minimum = 0,
                Maximum = 100,
                Width = 150
            };
            grid.Widgets.Add(turnOffLineSpeedSpin);
            var dimLineLabelMaxValueLabel = CreateLabel("Never", 11, 3, 1, 1);
            grid.Widgets.Add(dimLineLabelMaxValueLabel);

            var metricsLabel = CreateLabel("Metrics", 12, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(metricsLabel);
            var durationLabel = CreateLabel("Pressed Durations", 13, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(durationLabel);
            var minSecondsShowDurationLabel = CreateLabel("Min", 13, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(minSecondsShowDurationLabel);
            var pressThresholdText = CreateTextBox(_config.DisplayConfig.MinDisplayDuration.ToString(), 13, 2, 1, 1);
            pressThresholdText.Width = 50;
            grid.Widgets.Add(pressThresholdText);
            var displayDurationLabel = CreateLabel("Enabled", 13, 3, 1, 1);
            grid.Widgets.Add(displayDurationLabel);

            var displayDurationCheck = new CheckBox()
            {
                GridRow = 13,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayDuration,
            };
            grid.Widgets.Add(displayDurationCheck);

            var mashLabel = CreateLabel("Mash Counts", 14, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(mashLabel);
            var minMashLabel = CreateLabel("Min", 14, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(minMashLabel);
            var mashThresholdText = CreateTextBox(_config.DisplayConfig.MinDisplayFrequency.ToString(), 14, 2, 1, 1);
            mashThresholdText.Width = 50;
            grid.Widgets.Add(mashThresholdText);
            var displayFrequencyLabel = CreateLabel("Enabled", 14, 3, 1, 1);
            grid.Widgets.Add(displayFrequencyLabel);

            var displayFrequencyCheck = new CheckBox()
            {
                GridRow = 14,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayFrequency,
            };
            grid.Widgets.Add(displayFrequencyCheck);

            var frameDurationLabel = CreateLabel("Frame Duration", 15, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(frameDurationLabel);
            var displayFrameDurationLabel = CreateLabel("Enabled", 15, 3, 1, 1);
            grid.Widgets.Add(displayDurationLabel);

            var displayFrameDurationCheck = new CheckBox()
            {
                GridRow = 15,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayFrameDuration,
            };
            grid.Widgets.Add(displayFrameDurationCheck);

            var illegalInputsLabel = CreateLabel("Illegal D-Pad Input Display", 16, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(illegalInputsLabel);
            var illegalInputsEnabledLabel = CreateLabel("Enabled", 16, 3, 1, 1);
            grid.Widgets.Add(illegalInputsEnabledLabel);

            var illegalInputCheck = new CheckBox()
            {
                GridRow = 15,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayIllegalInputs,
            };
            grid.Widgets.Add(illegalInputCheck);

            dialog.Content = grid;
            dialog.Closed += (s, a) =>
            {
                if (!dialog.Result)
                {
                    return;
                }
                if (int.TryParse(displayWidthText.Text, out var displayWidth))
                {
                    _config.DisplayConfig.LineLength = displayWidth < 10 ? 1 : displayWidth;
                }
                if (int.TryParse(pressThresholdText.Text, out var pressThresholdSeconds))
                {
                    _config.DisplayConfig.MinDisplayDuration = pressThresholdSeconds < 1 ? 1 : pressThresholdSeconds;
                }
                if (int.TryParse(mashThresholdText.Text, out var frequencyThresholdValue))
                {
                    _config.DisplayConfig.MinDisplayFrequency = frequencyThresholdValue < 1 ? 1 : frequencyThresholdValue;
                }
                if (int.TryParse(maxContainerCombo.SelectedItem.Tag.ToString(), out var maxContainers))
                {
                    _config.DisplayConfig.MaxContainers = maxContainers;
                }
                _config.DisplayConfig.Speed = (float)Math.Round(displaySpeedSpin.Value);
                _config.DisplayConfig.TurnOffLineSpeed = turnOffLineSpeedSpin.Value * 50.0f;
                _config.DisplayConfig.DisplayDuration = displayDurationCheck.IsChecked;
                _config.DisplayConfig.DisplayFrequency = displayFrequencyCheck.IsChecked;
                _config.DisplayConfig.DrawIdleLines = displayIdleLindesCheck.IsChecked;
                _config.DisplayConfig.BackgroundColor = colorButton.TextColor;
                _config.DisplayConfig.Layout = (DisplayLayoutStyle)layoutCombo.SelectedItem.Tag;
                _config.DisplayConfig.EmptyContainerColor = emptyContainerColorButton.TextColor;
                _config.DisplayConfig.IllegalInputColor = illegalInputColorButton.TextColor;
                _config.DisplayConfig.DisplayIllegalInputs = illegalInputCheck.IsChecked;
                _config.DisplayConfig.DisplayFrameDuration = displayFrameDurationCheck.IsChecked;

                DisplaySettingsUpdated?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowModal(_desktop);
        }

        public void ShowMessage(string title, string message)
        {
            var messageBox = Dialog.CreateMessageBox(title, message);
            messageBox.ShowModal(_desktop);
        }

        public void ShowWaitMessage(string title, string message)
        {
            _waitMessageBox = Dialog.CreateMessageBox(title, message);
            _waitMessageBox.TitleTextColor = Color.DarkSeaGreen;
            _waitMessageBox.CloseKey = Keys.None;
            _waitMessageBox.CloseButton.Visible = false;
            _waitMessageBox.ButtonOk.Visible = false;
            _waitMessageBox.ButtonCancel.Visible = false;
            _waitMessageBox.ShowModal(_desktop);
        }

        public void HideWaitMessage()
        {
            if (_waitMessageBox == null)
            {
                return;
            }
            _waitMessageBox.Close();
            _waitMessageBox = null;
        }
        private void ChooseColor(ButtonMapping mapping, TextButton colorButton)
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

        private void ChooseColor(TextButton colorButton)
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

        private Label CreateLabel(string text, int gridRow, int gridCol, int rowSpan, int colSpan, Color? textColor = null, HorizontalAlignment alignment = HorizontalAlignment.Left)
        {
            var label = new Label()
            {
                Text = text,
                GridRow = gridRow,
                GridColumn = gridCol,
                GridRowSpan = rowSpan,
                GridColumnSpan = colSpan,
                HorizontalAlignment = alignment
            };
            if (textColor != null)
            {
                label.TextColor = textColor.Value;
            }
            return label;
        }

        private TextBox CreateTextBox(string text, int gridRow, int gridCol, int rowSpan, int colSpan, Color? textColor = null)
        {
            var textBox = new TextBox()
            {
                Text = text,
                GridRow = gridRow,
                GridColumn = gridCol,
                GridRowSpan = rowSpan,
                GridColumnSpan = colSpan,
            };
            if (textColor != null)
            {
                textBox.TextColor = textColor.Value;
            }
            return textBox;
        }

        private ComboBox CreateComboBox(int gridRow, int gridCol, int rowSpan, int colSpan)
        {
            var combo = new ComboBox()
            {
                GridRow = gridRow,
                GridColumn = gridCol,
                GridRowSpan = rowSpan,
                GridColumnSpan = colSpan,
                Padding = new Thickness(2)
            };
            return combo;
        }

        private TextButton CreateButton(string text, int gridRow, int gridCol, int rowSpan, int colSpan)
        {
            var button = new TextButton()
            {
                Text = text,
                GridRow = gridRow,
                GridColumn = gridCol,
                GridRowSpan = rowSpan,
                GridColumnSpan = colSpan,
                Padding = new Thickness(3),
                Border = new SolidBrush(Color.DarkGray),
                BorderThickness = new Thickness(1),
            };
            return button;
        }
    }
}
