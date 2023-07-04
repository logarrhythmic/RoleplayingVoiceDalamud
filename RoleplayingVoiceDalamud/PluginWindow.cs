﻿using Dalamud.Game.ClientState;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using RoleplayingVoiceCore;
using System;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
namespace RoleplayingVoice
{
    public class PluginWindow : Window
    {
        private Configuration configuration;
        RoleplayingVoiceManager _manager = null;
        BetterComboBox voiceComboBox;
        private string apiKey = "";
        private string characterVoice = "";
        private string serverIP = "";
        private string serverIPErrorMessage = string.Empty;
        private string apiKeyErrorMessage = string.Empty;
        private string managerNullMessage = string.Empty;
        private string[] _voiceList = new string[1] { "" };
        private bool isServerIPValid = false;
        private bool isapiKeyValid = false;
        private bool characterVoiceActive = false;
        private bool apiKeyValidated = false;
        private bool SizeYChanged = false;
        private bool runOnLaunch = true;
        private bool save = false;
        private bool managerNull;
        private bool voiceComboBoxVisible;
        private Vector2? initialSize;
        private Vector2? changedSize;
        private ClientState clientState;
        private bool _loggedIn;
        private float _playerCharacterVolume;
        private float _otherCharacterVolume;
        private float _unfocusedCharacterVolume;
        private bool _aggressiveCaching;

        public event EventHandler RequestingReconnect;

        public PluginWindow() : base("Roleplaying Voice Config")
        {
            IsOpen = true;
            Size = new Vector2(295, 550);
            initialSize = Size;
            SizeCondition = ImGuiCond.Always;
            Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize;
            voiceComboBox = new BetterComboBox("Voice List", _voiceList, 810);
            voiceComboBox.OnSelectedIndexChanged += VoiceComboBox_OnSelectedIndexChanged;
            voiceComboBox.SelectedIndex = 0;
        }

        private void VoiceComboBox_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (voiceComboBox != null && _voiceList != null)
            {
                characterVoice = _voiceList[voiceComboBox.SelectedIndex];
            }
        }

        public Configuration Configuration
        {
            get => configuration;
            set
            {
                configuration = value;
                if (configuration != null)
                {
                    serverIP = configuration.ConnectionIP != null ? configuration.ConnectionIP.ToString() : "";
                    apiKey = configuration.ApiKey != null &&
                    configuration.ApiKey.All(c => char.IsAsciiLetterOrDigit(c)) ? configuration.ApiKey : "";
                    characterVoiceActive = configuration.IsActive;
                    _playerCharacterVolume = configuration.PlayerCharacterVolume;
                    _otherCharacterVolume = configuration.OtherCharacterVolume;
                    _unfocusedCharacterVolume = configuration.UnfocusedCharacterVolume;
                    _aggressiveCaching = configuration.UseAggressiveSplicing;
                }
            }
        }

        public DalamudPluginInterface PluginInterface { get; internal set; }
        public RoleplayingVoiceManager Manager
        {
            get => _manager; set
            {
                _manager = value;
                if (_manager != null)
                {
                    managerNullMessage = string.Empty;
                    managerNull = false;
                    _manager.OnApiValidationComplete += _manager_OnApiValidationComplete;
                }
            }
        }

        internal ClientState ClientState
        {
            get => clientState;
            set
            {
                clientState = value;
                clientState.Login += ClientState_Login;
                clientState.Logout += ClientState_Logout;
            }
        }

        public Plugin PluginReference { get; internal set; }

        private void ClientState_Logout(object sender, EventArgs e)
        {
            characterVoice = "None";
            _loggedIn = false;
        }

        private void ClientState_Login(object sender, EventArgs e)
        {
            if (configuration.Characters != null)
            {
                if (configuration.Characters.ContainsKey(clientState.LocalPlayer.Name.TextValue))
                {
                    characterVoice = configuration.Characters[clientState.LocalPlayer.Name.TextValue]
                        != null ? configuration.Characters[clientState.LocalPlayer.Name.TextValue] : "";
                }
                else
                {
                    characterVoice = "None";
                }
            }
            else
            {
                characterVoice = "None";
            }
            _loggedIn = true;
            RefreshVoices();
        }
        public override void Draw()
        {
            //// UI REWORK

            if (ImGui.BeginTabBar("ConfigTabs"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    DrawGeneral();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Volume"))
                {
                    DrawVolume();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Server"))
                {
                    DrawServer();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ////
            /*ImGui.Text("Server IP");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.InputText("##serverIP", ref serverIP, 2000);

            ImGui.Text("Elevenlabs API Key");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.InputText("##apiKey", ref apiKey, 2000, ImGuiInputTextFlags.Password);

            if (!string.IsNullOrEmpty(configuration.ApiKey) &&
                configuration.ApiKey.All(c => char.IsAsciiLetterOrDigit(c)) && isapiKeyValid && clientState.LocalPlayer != null) {
                if (voiceComboBox != null && _voiceList != null) {
                    if (_voiceList.Length > 0) {
                        ImGui.Text("Voice");
                        voiceComboBox.Draw();
                        voiceComboBoxVisible = true;
                    } else {
                        voiceComboBoxVisible = false;
                    }
                } else {
                    voiceComboBoxVisible = false;
                }
                ImGui.Text("Current Player Voice Volume");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                ImGui.SliderFloat("##playerSlider", ref _playerCharacterVolume, 0, 1);
                ImGui.Text("Other Player Voice Volume");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                ImGui.SliderFloat("##otherSlider", ref _otherCharacterVolume, 0, 1);
                ImGui.Text("Unfocused Player Voice Volume");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                ImGui.SliderFloat("##unfocusedVolume", ref _unfocusedCharacterVolume, 0, 1);

                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                ImGui.Checkbox("##characterVoiceActive", ref characterVoiceActive);
                ImGui.SameLine();
                ImGui.Text("Voice Enabled");
                ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                ImGui.Checkbox("##aggressiveCachingActive", ref _aggressiveCaching);
                ImGui.SameLine();
                ImGui.Text("Use Aggressive Caching");

                if (_manager != null && _manager.Info != null) {
                    ImGui.LabelText("##usage", $"You have used {_manager.Info.CharacterCount}/{_manager.Info.CharacterLimit} characters.");
                    ImGui.TextWrapped($"Once this caps you will either need to upgrade subscription tiers or wait until the next month");
                }
            } else {
                voiceComboBoxVisible = false;
            }
            if (voiceComboBox.Contents.Length == 1 && voiceComboBoxVisible) {
                foreach (string voice in voiceComboBox.Contents) {
                    if (voice.Equals("") || voice.Equals("None")) {
                        RefreshVoices();
                        voiceComboBoxVisible = false;
                    }
                }
            }
            if (ImGui.Button("Reconnect")) {
                RequestingReconnect?.Invoke(this, EventArgs.Empty);
            }
            */
            var originPos = ImGui.GetCursorPos();
            // Place save button in bottom left + some padding / extra space
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMax().X + 10f);
            ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - 10f);
            if (ImGui.Button("Save"))
            {
                if (InputValidation())
                {
                    if (configuration != null && !string.IsNullOrEmpty(apiKey))
                    {
                        apiKeyValidated = false;
                        save = true;
                        if (_manager == null)
                        {
                            managerNullMessage = "Somehow, the manager went missing. Contact a developer!";
                            managerNull = true;
                            PluginReference.InitialzeManager();
                        }
                        if (_manager != null)
                        {
                            managerNullMessage = string.Empty;
                            managerNull = false;
                            Task.Run(() => _manager.ApiValidation(apiKey));
                        }
                    }
                    else if (string.IsNullOrEmpty(apiKey))
                    {
                        isapiKeyValid = false;
                        apiKeyErrorMessage = "API Key is empty! Please check the input.";
                    }
                    SizeYChanged = false;
                    changedSize = null;
                    Size = initialSize;
                }
            }
            ImGui.SetCursorPos(originPos);
            // Place close button in bottom right + some padding / extra space
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize("Close").X - 20f);
            ImGui.SetCursorPosY(ImGui.GetWindowContentRegionMax().Y - ImGui.GetFrameHeight() - 10f);
            if (ImGui.Button("Close"))
            {
                // Because we don't trust the user
                if (InputValidation())
                {
                    if (configuration != null && !string.IsNullOrEmpty(apiKey))
                    {
                        apiKeyValidated = false;
                        save = true;
                        if (_manager == null)
                        {
                            managerNullMessage = "Somehow, the manager went missing. Contact a developer!";
                            managerNull = true;
                            PluginReference.InitialzeManager();
                        }
                        if (_manager != null)
                        {
                            managerNullMessage = string.Empty;
                            managerNull = false;
                            Task.Run(() => _manager.ApiValidation(apiKey));
                        }
                    }
                    else if (string.IsNullOrEmpty(apiKey))
                    {
                        isapiKeyValid = false;
                        apiKeyErrorMessage = "API Key is empty! Please check the input.";
                    }
                }
                SizeYChanged = false;
                changedSize = null;
                Size = initialSize;
                IsOpen = false;
            }
            ImGui.SetCursorPos(originPos);
            if (!string.IsNullOrEmpty(apiKey) && runOnLaunch)
            {
                Task.Run(() => _manager.ApiValidation(apiKey));
                InputValidation();
                runOnLaunch = false;
            }
            else if (string.IsNullOrEmpty(apiKey))
            {
                if (runOnLaunch)
                {
                    InputValidation();
                }
                apiKeyErrorMessage = "API Key is empty! Please check the input.";
                runOnLaunch = false;
            }
            ImGui.BeginChild("ErrorRegion", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - 40f), false);
            if (!isServerIPValid)
            {
                ErrorMessage(serverIPErrorMessage);
            }
            if (!isapiKeyValid || string.IsNullOrEmpty(apiKey))
            {
                ErrorMessage(apiKeyErrorMessage);
            }
            if (managerNull)
            {
                ErrorMessage(managerNullMessage);
            }
            ImGui.EndChild();
        }

        private bool InputValidation()
        {
            if (!IPAddress.TryParse(serverIP, out _))
            {
                serverIPErrorMessage = "Invalid Server IP! Please check the input.";
                isServerIPValid = false;
            }
            else
            {
                serverIPErrorMessage = string.Empty;
                isServerIPValid = true;
            }
            if (isServerIPValid)
            {
                return true;
            }
            return false;
        }

        private void _manager_OnApiValidationComplete(object sender, ValidationResult e)
        {
            if (e.ValidationSuceeded && !apiKeyValidated)
            {
                apiKeyErrorMessage = string.Empty;
                isapiKeyValid = true;
            }
            else if (!e.ValidationSuceeded && !apiKeyValidated)
            {
                apiKeyErrorMessage = "Invalid API Key! Please check the input.";
                isapiKeyValid = false;
            }
            apiKeyValidated = true;

            // If the api key was validated, is valid, and the request was sent via the Save or Close button, the settings are saved.
            if (isapiKeyValid && save && apiKeyValidated)
            {
                configuration.ConnectionIP = serverIP;
                configuration.ApiKey = apiKey;
                if (clientState.LocalPlayer != null)
                {
                    if (configuration.Characters == null)
                    {
                        configuration.Characters = new System.Collections.Generic.Dictionary<string, string>();
                    }
                    configuration.Characters[clientState.LocalPlayer.Name.TextValue] = characterVoice != null ? characterVoice : "";
                }
                configuration.PlayerCharacterVolume = _playerCharacterVolume;
                configuration.OtherCharacterVolume = _otherCharacterVolume;
                configuration.UnfocusedCharacterVolume = _unfocusedCharacterVolume;
                configuration.IsActive = characterVoiceActive;
                configuration.UseAggressiveSplicing = _aggressiveCaching;
                PluginInterface.SavePluginConfig(configuration);
                configuration.Save();
                RefreshVoices();
                save = false;
            }
        }

        private Vector2? GetSizeChange(float requiredY, float availableY, int Lines, Vector2? initial)
        {
            // Height
            if (availableY - requiredY * Lines < 1)
            {
                Vector2? newHeight = new Vector2(initial.Value.X, initial.Value.Y + requiredY * Lines);
                return newHeight;
            }
            return initial;
        }

        private void ErrorMessage(string message)
        {
            var requiredY = ImGui.CalcTextSize(message).Y + 1f;
            var availableY = ImGui.GetContentRegionAvail().Y;
            var initialH = ImGui.GetCursorPos().Y;
            ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);
            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), message);
            ImGui.PopTextWrapPos();
            var changedH = ImGui.GetCursorPos().Y;
            float textHeight = changedH - initialH;
            int textLines = (int)(textHeight / ImGui.GetTextLineHeight());

            // Check height and increase if necessarry
            if (availableY - requiredY * textLines < 1 && !SizeYChanged)
            {
                SizeYChanged = true;
                changedSize = GetSizeChange(requiredY, availableY, textLines, initialSize);
                Size = changedSize;
            }
        }

        public async void RefreshVoices()
        {
            if (_manager != null)
            {
                _voiceList = await _manager.GetVoiceList();
                _manager.RefreshElevenlabsSubscriptionInfo();
            }
            if (clientState.LocalPlayer != null)
            {
                if (configuration.Characters == null)
                {
                    configuration.Characters = new System.Collections.Generic.Dictionary<string, string>();
                }
                if (configuration.Characters.ContainsKey(clientState.LocalPlayer.Name.TextValue))
                {
                    if (voiceComboBox != null)
                    {
                        if (_voiceList != null)
                        {
                            voiceComboBox.Contents = _voiceList;
                            for (int i = 0; i < voiceComboBox.Contents.Length; i++)
                            {
                                if (voiceComboBox.Contents[i].Contains(configuration.Characters[clientState.LocalPlayer.Name.TextValue]))
                                {
                                    voiceComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        internal class BetterComboBox
        {
            string _label = "";
            int _width = 0;
            int index = -1;
            int _lastIndex = 0;
            bool _enabled = true;
            string[] _contents = new string[1] { "" };
            public event EventHandler OnSelectedIndexChanged;
            public string Text { get { return index > -1 ? _contents[index] : ""; } }
            public BetterComboBox(string _label, string[] contents, int index, int width = 100)
            {
                if (Label != null)
                {
                    this._label = _label;
                }
                this._width = width;
                this.index = index;
                if (contents != null)
                {
                    this._contents = contents;
                }
            }

            public string[] Contents { get => _contents; set => _contents = value; }
            public int SelectedIndex { get => index; set => index = value; }
            public int Width { get => (_enabled ? _width : 0); set => _width = value; }
            public string Label { get => _label; set => _label = value; }
            public bool Enabled { get => _enabled; set => _enabled = value; }

            public void Draw()
            {
                if (_enabled)
                {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
                    if (_label != null && _contents != null)
                    {
                        if (_contents.Length > 0)
                        {
                            ImGui.Combo("##" + _label, ref index, _contents, _contents.Length);
                        }
                    }
                }
                if (index != _lastIndex)
                {
                    if (OnSelectedIndexChanged != null)
                    {
                        OnSelectedIndexChanged.Invoke(this, EventArgs.Empty);
                    }
                }
                _lastIndex = index;
            }
        }

        private void DrawTest()
        {
            ImGui.Text("This is a test.");

        }

        private void DrawGeneral()
        {
            // Needs changes for new behaviour: show combo at all times
            // if api isn't valid / validated the only options should be something like "API not initialized"
            if (isapiKeyValid && clientState.LocalPlayer != null)
            {
                if (voiceComboBox != null && _voiceList != null)
                {
                    if (_voiceList.Length > 0)
                    {
                        ImGui.Text("Voice");
                        voiceComboBox.Draw();
                    }
                }
                else if (voiceComboBox.Contents.Length == 1 &&
                    voiceComboBox.Contents[0].Contains("None", StringComparison.OrdinalIgnoreCase) ||
                    voiceComboBox.Contents[0].Contains("", StringComparison.OrdinalIgnoreCase))
                {
                    RefreshVoices();
                }
            }
            else if (voiceComboBox.Contents.Length == 1 && voiceComboBox != null && !isapiKeyValid || clientState.LocalPlayer == null && !isapiKeyValid)
            {
                voiceComboBox.Contents[0] = "API not initialized";
                if (_voiceList.Length > 0)
                {
                    ImGui.Text("Voice");
                    voiceComboBox.Draw();
                }
            }
            else if (clientState.LocalPlayer == null && isapiKeyValid)
            {
                voiceComboBox.Contents[0] = "Not logged in";
                if (_voiceList.Length > 0)
                {
                    ImGui.Text("Voice");
                    voiceComboBox.Draw();
                }
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.Checkbox("##characterVoiceActive", ref characterVoiceActive);
            ImGui.SameLine();
            ImGui.Text("Voice Enabled");

            if (_manager != null && _manager.Info != null && isapiKeyValid)
            {
                ImGui.LabelText("##usage", $"You have used {_manager.Info.CharacterCount}/{_manager.Info.CharacterLimit} characters.");
                ImGui.TextWrapped($"Once this caps you will either need to upgrade subscription tiers or wait until the next month");
            }
        }

        private void DrawVolume()
        {
            ////TODO?
            ImGui.Text("Current Player Voice Volume");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.SliderFloat("##playerSlider", ref _playerCharacterVolume, 0, 1);
            ImGui.Text("Other Player Voice Volume");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.SliderFloat("##otherSlider", ref _otherCharacterVolume, 0, 1);
            ImGui.Text("Unfocused Player Voice Volume");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.SliderFloat("##unfocusedVolume", ref _unfocusedCharacterVolume, 0, 1);
        }

        private void DrawServer()
        {
            ImGui.Text("Server IP");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.InputText("##serverIP", ref serverIP, 2000);

            ImGui.Text("Elevenlabs API Key");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.InputText("##apiKey", ref apiKey, 2000, ImGuiInputTextFlags.Password);

            ImGui.SetNextItemWidth(ImGui.GetContentRegionMax().X);
            ImGui.Checkbox("##aggressiveCachingActive", ref _aggressiveCaching);
            ImGui.SameLine();
            ImGui.Text("Use Aggressive Caching");

            if (ImGui.Button("Reconnect"))
            {
                RequestingReconnect?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
