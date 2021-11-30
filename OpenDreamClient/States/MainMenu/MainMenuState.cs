using System;
using System.Text.RegularExpressions;
using OpenDreamClient.States.MainMenu;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.AuthLib;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace OpenDreamClient.States.MainMenu
{
    public class MainMenuState : State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IBaseClient _client = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IGameController _controllerProxy = default!;

        private MainMenuControl _mainMenuControl = default!;
        private bool _isConnecting;

        // ReSharper disable once InconsistentNaming
        private static readonly Regex IPv6Regex = new(@"\[(.*:.*:.*)](?::(\d+))?");

        public override void Startup()
        {

            _mainMenuControl = new MainMenuControl(_resourceCache, _configurationManager);
            _userInterfaceManager.StateRoot.AddChild(_mainMenuControl);

            _mainMenuControl.QuitButton.OnPressed += QuitButtonPressed;
            _mainMenuControl.ConnectButton.OnPressed += ConnectButtonPressed;
            _mainMenuControl.AddressBox.OnTextEntered += AddressBoxEntered;
            _mainMenuControl.UserNameBox.OnTextChanged += UsernameBoxChanged;

            _client.RunLevelChanged += RunLevelChanged;

        }

        public override void Shutdown()
        {
            _client.RunLevelChanged -= RunLevelChanged;
            _netManager.ConnectFailed -= _onConnectFailed;

            _mainMenuControl.Dispose();
        }

        private void UsernameBoxChanged(LineEdit.LineEditEventArgs args)
        {
            _configurationManager.SetCVar(CVars.PlayerName, _mainMenuControl.UserNameBox.Text);
        }

        private void RunLevelChanged(object? obj, RunLevelChangedEventArgs args)
        {
            if (args.NewLevel == ClientRunLevel.Initialize)
            {
                _setConnectingState(false);
                _netManager.ConnectFailed -= _onConnectFailed;
            }
        }

        private void QuitButtonPressed(BaseButton.ButtonEventArgs args)
        {
            _controllerProxy.Shutdown();
        }

        private void ConnectButtonPressed(BaseButton.ButtonEventArgs args)
        {
            var input = _mainMenuControl.AddressBox;
            TryConnect(input.Text);
        }

        private void AddressBoxEntered(LineEdit.LineEditEventArgs args)
        {
            if (_isConnecting)
            {
                return;
            }

            TryConnect(args.Text);
        }

        private void TryConnect(string address)
        {
            var inputName = _mainMenuControl.UserNameBox.Text.Trim();
            if (!UsernameHelpers.IsNameValid(inputName, out var reason))
            {
                var invalidReason = Loc.GetString(reason.ToText());
                _userInterfaceManager.Popup(
                    Loc.GetString("main-menu-invalid-username-with-reason", ("invalidReason", invalidReason)),
                    Loc.GetString("main-menu-invalid-username"));
                return;
            }

            var configName = _configurationManager.GetCVar(CVars.PlayerName);
            if (_mainMenuControl.UserNameBox.Text != configName)
            {
                _configurationManager.SetCVar(CVars.PlayerName, inputName);
                _configurationManager.SaveToFile();
            }

            _setConnectingState(true);
            _netManager.ConnectFailed += _onConnectFailed;
            try
            {
                ParseAddress(address, out var ip, out var port);
                _client.ConnectToServer(ip, port);
            }
            catch (ArgumentException e)
            {
                _userInterfaceManager.Popup($"Unable to connect: {e.Message}", "Connection error.");
                Logger.Warning(e.ToString());
                _netManager.ConnectFailed -= _onConnectFailed;
                _setConnectingState(false);
            }
        }

        private void ParseAddress(string address, out string ip, out ushort port)
        {
            var match6 = IPv6Regex.Match(address);
            if (match6 != Match.Empty)
            {
                ip = match6.Groups[1].Value;
                if (!match6.Groups[2].Success)
                {
                    port = _client.DefaultPort;
                }
                else if (!ushort.TryParse(match6.Groups[2].Value, out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }

                return;
            }

            // See if the IP includes a port.
            var split = address.Split(':');
            ip = address;
            port = _client.DefaultPort;
            if (split.Length > 2)
            {
                throw new ArgumentException("Not a valid Address.");
            }

            // IP:port format.
            if (split.Length == 2)
            {
                ip = split[0];
                if (!ushort.TryParse(split[1], out port))
                {
                    throw new ArgumentException("Not a valid port.");
                }
            }
        }


        private void _onConnectFailed(object? _, NetConnectFailArgs args)
        {
            _userInterfaceManager.Popup($"Failed to connect: {args.Reason}");
            _netManager.ConnectFailed -= _onConnectFailed;
            _setConnectingState(false);
        }

        private void _setConnectingState(bool state)
        {
            _isConnecting = state;
            _mainMenuControl.ConnectButton.Disabled = state;
        }
    }
}