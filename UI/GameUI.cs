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

namespace InputVisualizer.UI
{
    public class GameUI
    {
        private Desktop _desktop;
        private ViewerConfig _config;
        private GameState _gameState;

        private bool _listeningForInput = false;
        private bool _listeningCancelPressed = false;
        private ButtonMapping _listeningMapping;
        private TextButton _listeningButton;
        private Grid _listeningGrid;
        private const int MAX_MAP_BUTTON_LENGTH = 20;

        private HorizontalStackPanel _mainMenuContainer;
        private Dialog _waitMessageBox = null;

        public bool ListeningForInput => _listeningForInput;

        public event EventHandler<InputSourceChangedEventArgs> InputSourceChanged;
        public event EventHandler GamepadSettingsUpdated;
        public event EventHandler RetroSpySettingsUpdated;
        public event EventHandler Usb2SnesSettingsUpdated;
        public event EventHandler DisplaySettingsUpdated;
        public event EventHandler<Usb2SnesGameChangedEventArgs> Usb2SnesGameChanged;
        public event EventHandler RefreshInputSources;

        public GameUI(Game game, ViewerConfig config, GameState gameState)
        {
            MyraEnvironment.Game = game;
            _config = config;
            _gameState = gameState;
        }

        public void Init(Dictionary<string, SystemGamePadInfo> systemGamepads, List<string> usb2SnesDevices, Usb2SnesGameList usb2SnesGameList)
        {
            _mainMenuContainer = new HorizontalStackPanel();

            UpdateMainMenu(systemGamepads, usb2SnesDevices, usb2SnesGameList);

            _desktop = new Desktop
            {
                Root = _mainMenuContainer
            };
            _desktop.Root.VerticalAlignment = VerticalAlignment.Top;
            _desktop.Root.HorizontalAlignment = HorizontalAlignment.Left;
        }

        public void UpdateMainMenu(Dictionary<string, SystemGamePadInfo> systemGamepads, List<string> usb2SnesDevices, Usb2SnesGameList usb2SnesGameList)
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
            inputSourceCombo.Items.Add(new ListItem("RetroSpy", Color.White, "spy"));
            inputSourceCombo.Items.Add(new ListItem("Keyboard", Color.White, "keyboard"));

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

            var menuItemInputs = new MenuItem();
            menuItemInputs.Text = "Configure Current Input";
            menuItemInputs.Id = "menuItemInputs";
            menuItemInputs.Selected += (s, a) =>
            {
                if (_gameState.CurrentInputMode == InputMode.RetroSpy)
                {
                    ShowConfigureRetroSpyDialog();
                }
                else if (_gameState.CurrentInputMode == InputMode.Gamepad)
                {
                    ShowConfigureGamePadDialog(systemGamepads);
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
            menuItemActions.Items.Add(menuItemInputs);
            menuItemActions.Items.Add(menuItemDisplay);
            menuItemActions.Items.Add(menuItemRefresh);
            menuItemActions.Items.Add(menuItemAbout);

            menuBar.Items.Add(menuItemActions);

            _mainMenuContainer.Widgets.Add(inputSourceCombo);
            _mainMenuContainer.Widgets.Add(usb2SnesGameListCombo);
            _mainMenuContainer.Widgets.Add(menuBar);
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
            var keyboardState = Keyboard.GetState();
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                _listeningForInput = false;
                _listeningCancelPressed = true;
                _listeningButton.Text = _listeningMapping.ButtonType.ToString();
                return;
            }

            var keyDetected = Keys.None;
            var buttonDetected = ButtonType.NONE;
            var state = GamePad.GetState(_gameState.CurrentPlayerIndex, GamePadDeadZone.Circular);
            var activeConfig = _gameState.ActiveGamepadConfig;

            var pressedKeys = keyboardState.GetPressedKeys();
            if (pressedKeys.Length > 0)
            {
                keyDetected = pressedKeys[0];
            }

            if (keyDetected == Keys.None && !activeConfig.IsKeyboard)
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
                    var listenPrompt = _gameState.ActiveGamepadConfig.IsKeyboard ? "Press Key..." : "Press Button/Key...";
                    var buttonText = mapping.MappingType == ButtonMappingType.Button ? mapping.MappedButtonType.ToString() + " Button" : mapping.MappedKey.ToString() + " Key";
                    buttonText = buttonText.Length > MAX_MAP_BUTTON_LENGTH ? buttonText.Substring(0, MAX_MAP_BUTTON_LENGTH) : buttonText;
                    var mapButton = CreateButton(buttonText, currGridRow, currColumn, 1, 1);

                    mapButton.Width = 225;
                    mapButton.Tag = mapping;
                    mapButton.Click += (s, e) =>
                    {
                        if (_listeningForInput)
                        {
                            var messageBox = Dialog.CreateMessageBox("Button Mapping", "Finish mapping button or hit ESC to cancel");
                            messageBox.ShowModal(_desktop);
                            return;
                        }
                        _listeningForInput = true;
                        _listeningButton = mapButton;
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
                CreateLabel("1.3", 0, 1, 1, 1),
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
                ChooseBackgroundColor(colorButton);
            };
            grid.Widgets.Add(colorButton);

            var emptyContainerColorLabel = CreateLabel("Empty Container", 5, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(emptyContainerColorLabel);

            var emptyContainerColorButton = CreateButton("Color", 5, 2, 1, 1);
            emptyContainerColorButton.TextColor = _config.DisplayConfig.EmptyContainerColor;
            emptyContainerColorButton.Click += (s, e) =>
            {
                ChooseBackgroundColor(emptyContainerColorButton);
            };
            grid.Widgets.Add(emptyContainerColorButton);


            var linesLabel = CreateLabel("Lines", 6, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(linesLabel);

            var showIdleLabel = CreateLabel("Idle Lines Enabled", 7, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(showIdleLabel);
            var displayIdleLindesCheck = new CheckBox()
            {
                GridRow = 7,
                GridColumn = 2,
                IsChecked = _config.DisplayConfig.DrawIdleLines
            };
            grid.Widgets.Add(displayIdleLindesCheck);

            var lineLengthLabel = CreateLabel("Length", 8, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(lineLengthLabel);

            var displayWidthText = CreateTextBox(_config.DisplayConfig.LineLength.ToString(), 8, 2, 1, 1);
            displayWidthText.Width = 50;
            grid.Widgets.Add(displayWidthText);

            var speedLabel = CreateLabel("Speed", 9, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(speedLabel);
            var speedMinValueLabel = CreateLabel("Slow", 9, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(speedMinValueLabel);
            var displaySpeedSpin = new HorizontalSlider()
            {
                GridRow = 9,
                GridColumn = 2,
                GridColumnSpan = 1,
                Value = _config.DisplayConfig.Speed,
                Minimum = 1,
                Maximum = 11,
                Width = 150
            };
            grid.Widgets.Add(displaySpeedSpin);
            var speedMaxValueLabel = CreateLabel("Fast", 9, 3, 1, 1);
            grid.Widgets.Add(speedMaxValueLabel);

            var dimLineLabel = CreateLabel("Dim Speed", 10, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(dimLineLabel);

            var dimLineLabelMinValueLabel = CreateLabel("Instant", 10, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(dimLineLabelMinValueLabel);
            var turnOffLineSpeedSpin = new HorizontalSlider()
            {
                GridRow = 10,
                GridColumn = 2,
                GridColumnSpan = 1,
                Value = _config.DisplayConfig.TurnOffLineSpeed / 50.0f,
                Minimum = 0,
                Maximum = 100,
                Width = 150
            };
            grid.Widgets.Add(turnOffLineSpeedSpin);
            var dimLineLabelMaxValueLabel = CreateLabel("Never", 10, 3, 1, 1);
            grid.Widgets.Add(dimLineLabelMaxValueLabel);

            var metricsLabel = CreateLabel("Metrics", 11, 0, 1, 1, Color.DarkSeaGreen);
            grid.Widgets.Add(metricsLabel);
            var durationLabel = CreateLabel("Pressed Durations", 12, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(durationLabel);
            var minSecondsShowDurationLabel = CreateLabel("Min", 12, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(minSecondsShowDurationLabel);
            var pressThresholdText = CreateTextBox(_config.DisplayConfig.MinDisplayDuration.ToString(), 12, 2, 1, 1);
            pressThresholdText.Width = 50;
            grid.Widgets.Add(pressThresholdText);
            var displayDurationLabel = CreateLabel("Enabled", 12, 3, 1, 1);
            grid.Widgets.Add(displayDurationLabel);

            var displayDurationCheck = new CheckBox()
            {
                GridRow = 12,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayDuration,
            };
            grid.Widgets.Add(displayDurationCheck);

            var mashLabel = CreateLabel("Mash Counts", 13, 0, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(mashLabel);
            var minMashLabel = CreateLabel("Min", 13, 1, 1, 1, null, HorizontalAlignment.Right);
            grid.Widgets.Add(minMashLabel);
            var mashThresholdText = CreateTextBox(_config.DisplayConfig.MinDisplayFrequency.ToString(), 13, 2, 1, 1);
            mashThresholdText.Width = 50;
            grid.Widgets.Add(mashThresholdText);
            var displayFrequencyLabel = CreateLabel("Enabled", 13, 3, 1, 1);
            grid.Widgets.Add(displayFrequencyLabel);

            var displayFrequencyCheck = new CheckBox()
            {
                GridRow = 13,
                GridColumn = 4,
                IsChecked = _config.DisplayConfig.DisplayFrequency,
            };
            grid.Widgets.Add(displayFrequencyCheck);

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
                _config.DisplayConfig.Speed = displaySpeedSpin.Value;
                _config.DisplayConfig.TurnOffLineSpeed = turnOffLineSpeedSpin.Value * 50.0f;
                _config.DisplayConfig.DisplayDuration = displayDurationCheck.IsChecked;
                _config.DisplayConfig.DisplayFrequency = displayFrequencyCheck.IsChecked;
                _config.DisplayConfig.DrawIdleLines = displayIdleLindesCheck.IsChecked;
                _config.DisplayConfig.BackgroundColor = colorButton.TextColor;
                _config.DisplayConfig.Layout = (DisplayLayoutStyle)layoutCombo.SelectedItem.Tag;
                _config.DisplayConfig.EmptyContainerColor = emptyContainerColorButton.TextColor;

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

        private void ChooseBackgroundColor(TextButton colorButton)
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
