﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using RoleplayingMediaCore.AudioRecycler;
using RoleplayingVoiceCore;
using System;
using System.Collections.Generic;
using System.IO;

namespace RoleplayingVoice {
    #region could be separated into a UserNpcVoicePreference.cs
    [Flags]
    public enum UserNpcVoicePreferenceFlags
    {
        PreferElevenlabs = VoiceLinePriority.Elevenlabs,
        PreferAlternative = VoiceLinePriority.Alternative,
        PreferXTTS = VoiceLinePriority.XTTS,
        None = VoiceLinePriority.None,
        Force = 0x80,
        ForceElevenlabs = VoiceLinePriority.Elevenlabs | Force,
        ForceAlternative = VoiceLinePriority.Alternative | Force,
        ForceXTTS = VoiceLinePriority.XTTS | Force
    }

    public enum UserNpcVoicePreferenceOption
    {
        None,
        PreferElevenlabs,
        PreferAlternative,
        PreferXTTS,
        ForceElevenlabs,
        ForceAlternative,
        ForceXTTS
    }

    public static class UserNpcVoicePreference
    {
        public static string[] Labels = new[] {
            "Default",
            "Request Elevenlabs when other rules don't apply",
            "Request XIVV when other rules don't apply",
            "Request XTTS when other rules don't apply (Generated on demand by ARK Server)",
            "Always request Elevenlabs",
            "Always request XIVV",
            "Always request XTTS (Generated on demand by ARK Server)"
        };
        public static UserNpcVoicePreferenceFlags Map(UserNpcVoicePreferenceOption value) => value switch
        {
            UserNpcVoicePreferenceOption.None => UserNpcVoicePreferenceFlags.None,
            UserNpcVoicePreferenceOption.PreferElevenlabs => UserNpcVoicePreferenceFlags.PreferElevenlabs,
            UserNpcVoicePreferenceOption.PreferAlternative => UserNpcVoicePreferenceFlags.PreferAlternative,
            UserNpcVoicePreferenceOption.PreferXTTS => UserNpcVoicePreferenceFlags.PreferXTTS,
            UserNpcVoicePreferenceOption.ForceElevenlabs => UserNpcVoicePreferenceFlags.ForceElevenlabs,
            UserNpcVoicePreferenceOption.ForceAlternative => UserNpcVoicePreferenceFlags.ForceAlternative,
            UserNpcVoicePreferenceOption.ForceXTTS => UserNpcVoicePreferenceFlags.ForceXTTS,
            _ => throw new Exception("Invalid value")
        };
        public static UserNpcVoicePreferenceOption Map(UserNpcVoicePreferenceFlags value) => value switch
        {
            UserNpcVoicePreferenceFlags.None => UserNpcVoicePreferenceOption.None,
            UserNpcVoicePreferenceFlags.PreferElevenlabs => UserNpcVoicePreferenceOption.PreferElevenlabs,
            UserNpcVoicePreferenceFlags.PreferAlternative => UserNpcVoicePreferenceOption.PreferAlternative,
            UserNpcVoicePreferenceFlags.PreferXTTS => UserNpcVoicePreferenceOption.PreferXTTS,
            UserNpcVoicePreferenceFlags.ForceElevenlabs => UserNpcVoicePreferenceOption.ForceElevenlabs,
            UserNpcVoicePreferenceFlags.ForceAlternative => UserNpcVoicePreferenceOption.ForceAlternative,
            UserNpcVoicePreferenceFlags.ForceXTTS => UserNpcVoicePreferenceOption.ForceXTTS,
            _ => throw new Exception("Invalid value")
        };
        public static VoiceLinePriority FlagsToVoiceLinePriority(UserNpcVoicePreferenceFlags value) => (VoiceLinePriority)((int)value & (0xFFFFFFFF ^ (int)UserNpcVoicePreferenceFlags.Force));
    }
    #endregion

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
        private float _npcVolume = 0.7f;
        bool useAggressiveCaching = true;
        private string cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPVoiceCache");
        private bool usePlayerSync = false;
        private bool tuneIntoTwitchStreams = true;
        private bool ignoreWhitelist = true;
        private bool performEmotesBasedOnWrittenText;
        private bool seperateSCDModsFromNativeSoundSystem;
        private bool _npcSpeechGenerationDisabled;
        private List<string> whitelist = new List<string>();
        private string streamPath = "";
        private bool _autoTextAdvance = true;
        private bool _replaceVoicedARRCutscenes = true;
        private int _audioOutputType = 0;
        private bool _qualityAssuranceMode;
        private float _npcSpeechSpeed = 1;
        private UserNpcVoicePreferenceOption _userNpcVoicePreference = UserNpcVoicePreferenceOption.None;
        private int _defaultTwitchOpen;
        private bool _twitchStreamTriggersIfShouter;
        private bool _dontVoiceRetainers;
        private bool _dontDoLipsync;
        private bool _tuneIntoTwitchStreamPrompt = true;
        private bool _readQuestObjectives = true;
        private bool _readLocationsAndToastNotifications = false;
        private bool _readNarratorLines = true;
        private bool _lowPerformanceMode;
        private int _spatialAudioAccuracy = 100;

        List<CustomNpcCharacter> _customNpcCharacters = new List<CustomNpcCharacter>();
        private int _playerVoiceEngine;
        private bool _ignoreSpatialAudioForTTS;
        private bool _allowDialogueQueuingOutsideCutscenes;

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
        public bool IsActive {
            set {
                AiVoiceActive = value;
            }
        }
        public bool AiVoiceActive { get; set; }
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
        public bool MoveSCDBasedModsToPerformanceSlider { get; set; }
        public string CacheFolder {
            get {
                if (Directory.Exists(cacheFolder)) {
                    return cacheFolder;
                } else {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RPVoiceCache");
                }
            }
            set => cacheFolder = value;
        }
        public List<string> Whitelist { get => whitelist; set => whitelist = value; }
        public float LoopingSFXVolume {
            get => _loopingSFXVolume; set {
                if (value == 0) {
                    _loopingSFXVolume = 1;
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
        public bool SeperateSCDModsFromNativeSoundSystem { get => seperateSCDModsFromNativeSoundSystem; set => seperateSCDModsFromNativeSoundSystem = value; }
        public bool NpcSpeechGenerationDisabled { get => _npcSpeechGenerationDisabled; set => _npcSpeechGenerationDisabled = value; }
        public float NpcVolume {
            get => _npcVolume; set {
                _npcVolume = value;
            }
        }

        public bool AutoTextAdvance { get => _autoTextAdvance; set => _autoTextAdvance = value; }
        public bool ReplaceVoicedARRCutscenes { get => _replaceVoicedARRCutscenes; set => _replaceVoicedARRCutscenes = value; }
        public int AudioOutputType { get => _audioOutputType; set => _audioOutputType = value; }
        public bool QualityAssuranceMode { get => _qualityAssuranceMode; set => _qualityAssuranceMode = value; }
        public int DefaultTwitchOpen { get => _defaultTwitchOpen; set => _defaultTwitchOpen = value; }
        public bool TwitchStreamTriggersIfShouter { get => _twitchStreamTriggersIfShouter; set => _twitchStreamTriggersIfShouter = value; }
        public float NPCSpeechSpeed { get { return _npcSpeechSpeed; } set { _npcSpeechSpeed = value; } }
        public UserNpcVoicePreferenceOption UserNpcVoicePreference { get { return _userNpcVoicePreference; } set { _userNpcVoicePreference = value; } }
        public bool DebugMode { get; set; }
        public bool DontVoiceRetainers { get => _dontVoiceRetainers; set => _dontVoiceRetainers = value; }
        public bool DontDoLipsync { get => _dontDoLipsync; set => _dontDoLipsync = value; }
        public bool TuneIntoTwitchStreamPrompt { get => _tuneIntoTwitchStreamPrompt; set => _tuneIntoTwitchStreamPrompt = value; }
        public bool ReadQuestObjectives { get => _readQuestObjectives; set => _readQuestObjectives = value; }
        public bool ReadLocationsAndToastNotifications { get => _readLocationsAndToastNotifications; set => _readLocationsAndToastNotifications = value; }
        public bool ReadNarratorLines { get => _readNarratorLines; set => _readNarratorLines = value; }
        public bool LowPerformanceMode { get => _lowPerformanceMode; set => _lowPerformanceMode = value; }
        public int SpatialAudioAccuracy { get => _spatialAudioAccuracy; set => _spatialAudioAccuracy = value; }
        public List<CustomNpcCharacter> CustomNpcCharacters { get => _customNpcCharacters; set => _customNpcCharacters = value; }
        public int PlayerVoiceEngine { get => _playerVoiceEngine; set => _playerVoiceEngine = value; }
        public bool IgnoreSpatialAudioForTTS { get => _ignoreSpatialAudioForTTS; set => _ignoreSpatialAudioForTTS = value; }
        public bool AllowDialogueQueuingOutsideCutscenes { get => _allowDialogueQueuingOutsideCutscenes; set => _allowDialogueQueuingOutsideCutscenes = value; }

        #endregion

        private readonly IDalamudPluginInterface pluginInterface;

        public Configuration(IDalamudPluginInterface pi) {
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
