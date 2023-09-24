﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using RoleplayingMediaCore.AudioRecycler;
using System;
using System.Collections.Generic;
using System.IO;

namespace RoleplayingVoice {
    public class Configuration : IPluginConfiguration {
        static bool configAlreadyLoaded = true;
        public event EventHandler OnConfigurationChanged;
        private string connectionIP = "24.77.70.65";
        private bool _hasMigrated = false;
        private float _playerCharacterVolume = 1;
        private float _otherCharacterVolume = 1;
        private float _unfocusedCharacterVolume = 0.5f;
        private float _loopingSFXVolume = 1;
        private float _livestreamVolume = 1;
        bool useAggressiveCaching = true;
        private string cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPVoiceCache");
        private bool usePlayerSync = false;
        private bool tuneIntoTwitchStreams = true;
        private bool ignoreWhitelist = true;
        private bool performEmotesBasedOnWrittenText;
        private List<string> whitelist = new List<string>();
        private string streamPath = "";

        int IPluginConfiguration.Version { get; set; }

        #region Saved configuration values
        public string ConnectionIP {
            get {
                if (connectionIP.Contains("50.70.229.19")) {
                    connectionIP = "24.77.70.65";
                    return connectionIP;
                }
                return connectionIP;
            }
            set {
                if (connectionIP.Contains("50.70.229.19")) {
                    connectionIP = "24.77.70.65";
                } else {
                    connectionIP = value;
                }
            }
        }
        public string ApiKey { get; set; }
        public bool IsActive { get; set; }
        public bool VoicePackIsActive { get; set; }

        public CharacterVoices CharacterVoices { get; set; }

        public Dictionary<string, string> Characters { get; set; }
        public Dictionary<string, string> CharacterVoicePacks { get; set; }
        public float PlayerCharacterVolume { get => _playerCharacterVolume; set => _playerCharacterVolume = value; }
        public float OtherCharacterVolume { get => _otherCharacterVolume; set => _otherCharacterVolume = value; }
        public float UnfocusedCharacterVolume { get => _unfocusedCharacterVolume; set => _unfocusedCharacterVolume = value; }
        public bool UseAggressiveSplicing { get => useAggressiveCaching; set => useAggressiveCaching = value; }
        public bool UsePlayerSync { get => usePlayerSync; set => usePlayerSync = value; }
        public bool IgnoreWhitelist { get => ignoreWhitelist; set => ignoreWhitelist = value; }
        public string CacheFolder { get => cacheFolder; set => cacheFolder = value; }
        public List<string> Whitelist { get => whitelist; set => whitelist = value; }
        public float LoopingSFXVolume {
            get => _loopingSFXVolume; set {
                if (value == 0) {
                    LoopingSFXVolume = 1;
                } else {
                    _loopingSFXVolume = value;
                }
            }
        }

        public string StreamPath { get => streamPath; set => streamPath = value; }
        public float LivestreamVolume { get => _livestreamVolume; set => _livestreamVolume = value; }
        public bool TuneIntoTwitchStreams { get => tuneIntoTwitchStreams; set => tuneIntoTwitchStreams = value; }
        public bool HasMigrated { get => _hasMigrated; set => _hasMigrated = value; }
        public bool PerformEmotesBasedOnWrittenText { get => performEmotesBasedOnWrittenText; set => performEmotesBasedOnWrittenText = value; }
        #endregion

        private readonly DalamudPluginInterface pluginInterface;

        public Configuration(DalamudPluginInterface pi) {
            this.pluginInterface = pi;
        }

        public void Save() {
            if (this.pluginInterface != null) {
                this.pluginInterface.SavePluginConfig(this);
            }
            OnConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}