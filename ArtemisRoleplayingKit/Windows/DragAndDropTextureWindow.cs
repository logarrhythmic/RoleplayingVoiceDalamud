﻿using Dalamud.Interface.DragDrop;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Math;
using ImGuiNET;
using ImGuiScene;
using RoleplayingMediaCore;
using RoleplayingMediaCore.Twitch;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Penumbra.Api.Ipc;
using Vector2 = System.Numerics.Vector2;
using FFXIVLooseTextureCompiler;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVLooseTextureCompiler.PathOrganization;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using Penumbra.Api;
using FFXIVLooseTextureCompiler.Export;
using FFXIVLooseTextureCompiler.Racial;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using RoleplayingVoiceDalamud.Glamourer;
using Penumbra.GameData.Enums;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonJobHudRDM0.BalanceGauge;
using System.Threading;

namespace RoleplayingVoice {
    internal class DragAndDropTextureWindow : Window {
        IDalamudTextureWrap textureWrap;
        private DalamudPluginInterface _pluginInterface;
        private readonly IDragDropManager _dragDropManager;
        private readonly MemoryStream _blank;
        Plugin plugin;
        private ImGuiWindowFlags _defaultFlags;
        private ImGuiWindowFlags _dragAndDropFlags;
        private TextureProcessor _textureProcessor;
        private string _exportStatus;
        private bool _lockDuplicateGeneration;
        private object _currentMod;
        private CharacterCustomization _currentCustomization;
        private string[] _choiceTypes;
        private string[] _bodyNames;
        private string[] _bodyNamesSimplified;
        private string[] _genders;
        private string[] _races;
        private string[] _subRaces;
        private string[] _faceTypes;
        private string[] _faceParts;
        private string[] _faceScales;

        public Plugin Plugin { get => plugin; set => plugin = value; }
        public DragAndDropTextureWindow(DalamudPluginInterface pluginInterface, IDragDropManager dragDropManager) :
            base("DragAndDropTexture", ImGuiWindowFlags.NoFocusOnAppearing
                | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar, true) {
            _defaultFlags = ImGuiWindowFlags.NoFocusOnAppearing
                | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoInputs
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar;
            _dragAndDropFlags = ImGuiWindowFlags.NoFocusOnAppearing
                | ImGuiWindowFlags.NoBringToFrontOnFocus
                | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoTitleBar;
            IsOpen = true;
            Size = new Vector2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            SizeCondition = ImGuiCond.None;
            _pluginInterface = pluginInterface;
            Position = new Vector2(0, 0);
            AllowClickthrough = true;
            _dragDropManager = dragDropManager;
            _blank = new MemoryStream();
            Bitmap none = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics graphics = Graphics.FromImage(none);
            graphics.Clear(Color.Transparent);
            none.Save(_blank, ImageFormat.Png);
            _blank.Position = 0;
            // This will be used for underlay textures.
            // The user will need to download a mod pack with the following path until there is a better way to acquire underlay assets.
            string underlayTexturePath = Path.Combine(Ipc.GetModDirectory.Subscriber(pluginInterface).Invoke(), @"\LooseTextureCompilerDLC\");
            // This should reference the xNormal install no matter where its been installed.
            // If this path is not found xNormal reliant functions will be disabled until xNormal is installed.
            _xNormalPath = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\xNormal\3.19.3\xNormal (x64).lnk";
            _textureProcessor = new TextureProcessor(underlayTexturePath);
            _textureProcessor.OnStartedProcessing += TextureProcessor_OnStartedProcessing;
            _textureProcessor.OnLaunchedXnormal += TextureProcessor_OnLaunchedXnormal;
            _choiceTypes = new string[] { "Detailed", "Simple", "Dropdown", "Group Is Checkbox" };
            _bodyNames = new string[] { "Vanilla and Gen2", "BIBO+", "EVE", "Gen3 and T&F3", "SCALES+", "TBSE and HRBODY", "TAIL", "Otopop" };
            _bodyNamesSimplified = new string[] { "BIBO+ Based", "Gen3 Based", "TBSE and HRBODY", "Otopop" };
            _genders = new string[] { "Masculine", "Feminine" };
            _races = new string[] { "Midlander", "Highlander", "Elezen", "Miqo'te", "Roegadyn", "Lalafell", "Raen", "Xaela", "Hrothgar", "Viera" };
            _subRaces = new string[] { "Midlander", "Highlander", "Wildwood", "Duskwight", "Seeker", "Keeper", "Sea Wolf", "Hellsguard",
        "Plainsfolk", "Dunesfolk", "Raen", "Xaela", "Helions", "The Lost", "Rava", "Veena" };
            _faceTypes = new string[] { "Face 1", "Face 2", "Face 3", "Face 4", "Face 5", "Face 6", "Face 7", "Face 8", "Face 9" };
            _faceParts = new string[] { "Face", "Eyebrows", "Eyes", "Ears", "Face Paint", "Hair", "Face B", "Etc B" };
            _faceScales = new string[] { "Vanilla Scales", "Scaleless Vanilla", "Scaleless Varied" };
        }

        private void TextureProcessor_OnLaunchedXnormal(object? sender, EventArgs e) {
            _exportStatus = "Waiting For XNormal To Generate Assets For";
        }

        private void TextureProcessor_OnStartedProcessing(object? sender, EventArgs e) {
            _exportStatus = "Compiling Assets For";
        }

        public override void Draw() {
            if (IsOpen) {
                if (!_lockDuplicateGeneration) {
                    _dragDropManager.CreateImGuiSource("TextureDragDrop", m => m.Extensions.Any(e => ValidTextureExtensions.Contains(e.ToLowerInvariant())), m => {
                        ImGui.TextUnformatted($"Dragging texture onto desired body part:\n\t{string.Join("\n\t", m.Files.Select(Path.GetFileName))}");
                        AllowClickthrough = false;
                        return true;
                    });

                    if (!AllowClickthrough) {
                        Flags = _dragAndDropFlags;
                        textureWrap = _pluginInterface.UiBuilder.LoadImage(_blank.ToArray());
                        ImGui.Image(textureWrap.ImGuiHandle, new Vector2(ImGui.GetMainViewport().Size.X, ImGui.GetMainViewport().Size.Y));
                    } else {
                        Flags = _defaultFlags;
                    }

                    if (_dragDropManager.CreateImGuiTarget("TextureDragDrop", out var files, out _)) {
                        List<TextureSet> textureSets = new List<TextureSet>();
                        string modName = plugin.ClientState.LocalPlayer.Name.TextValue.Split(' ')[0] + " Texture Mod";
                        foreach (var file in files) {
                            if (ValidTextureExtensions.Contains(Path.GetExtension(file))) {
                                string filePath = file;
                                string fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
                                _currentCustomization = plugin.GetCustomization(plugin.ClientState.LocalPlayer);
                                if (fileName.Contains("mata") || fileName.Contains("amat")
                                    || fileName.Contains("materiala") || fileName.Contains("gen2")) {
                                    var item = AddBody(_currentCustomization.Customize.Gender.Value, 0,
                                    RaceInfo.SubRaceToMainRace(_currentCustomization.Customize.Clan.Value - 1),
                                    _currentCustomization.Customize.TailShape.Value - 1, false);
                                    item.Diffuse = file;
                                    textureSets.Add(item);
                                    modName += " Body";
                                } else if (fileName.Contains("bibo")) {
                                    var item = AddBody(_currentCustomization.Customize.Gender.Value, 1,
                                    RaceInfo.SubRaceToMainRace(_currentCustomization.Customize.Clan.Value - 1),
                                    _currentCustomization.Customize.TailShape.Value - 1, false);
                                    item.Diffuse = file;
                                    textureSets.Add(item);
                                    modName += " Body";
                                } else if (fileName.Contains("gen3")) {
                                    var item = AddBody(_currentCustomization.Customize.Gender.Value, 3,
                                    RaceInfo.SubRaceToMainRace(_currentCustomization.Customize.Clan.Value - 1),
                                    _currentCustomization.Customize.TailShape.Value - 1, false);
                                    item.Diffuse = file;
                                    textureSets.Add(item);
                                    modName += " Body";
                                } else if (fileName.Contains("face") || fileName.Contains("makeup")) {
                                    var item = AddFace(_currentCustomization.Customize.Face.Value - 1, 0, 0,
                                    _currentCustomization.Customize.Gender.Value,
                                    RaceInfo.SubRaceToMainRace(_currentCustomization.Customize.Clan.Value - 1),
                                    _currentCustomization.Customize.Clan.Value - 1, 0, false);
                                    item.Diffuse = file;
                                    textureSets.Add(item);
                                    modName += " Face";
                                } else if (fileName.Contains("eye")) {
                                    var item = AddFace(_currentCustomization.Customize.Face.Value - 1, 2, 0,
                                    _currentCustomization.Customize.Gender.Value,
                                    RaceInfo.SubRaceToMainRace(_currentCustomization.Customize.Clan.Value - 1),
                                    _currentCustomization.Customize.Clan.Value - 1, 0, false);
                                    item.Normal = file;
                                    textureSets.Add(item);
                                    modName += " Eyes";
                                }
                            }
                        }
                        string fullModPath = Path.Combine(Ipc.GetModDirectory.Subscriber(_pluginInterface).Invoke(), modName);
                        if (textureSets.Count > 0) {
                            Task.Run(() => Export(true, textureSets, fullModPath, modName));
                        }
                    }
                    AllowClickthrough = true;
                } else {
                    AllowClickthrough = true;
                    Flags = _defaultFlags;
                }
            }
        }
        private void ExportJson(string jsonFilePath) {
            string jsonText = @"{
  ""Name"": """",
  ""Priority"": 0,
  ""Files"": { },
  ""FileSwaps"": { },
  ""Manipulations"": []
}";
            if (jsonFilePath != null) {
                using (StreamWriter writer = new StreamWriter(jsonFilePath)) {
                    writer.WriteLine(jsonText);
                }
            }
        }
        private void ExportMeta(string metaFilePath, string name, string author = "Loose Texture Compiler",
            string description = "Exported By Loose Texture Compiler", string modVersion = "0.0.0",
            string modWebsite = @"https://github.com/Sebane1/FFXIVLooseTextureCompiler") {
            string metaText = @"{
  ""FileVersion"": 3,
  ""Name"": """ + (!string.IsNullOrEmpty(name) ? name : "") + @""",
  ""Author"": """ + (!string.IsNullOrEmpty(author) ? author :
    "FFXIV Loose Texture Compiler") + @""",
  ""Description"": """ + (!string.IsNullOrEmpty(description) ? description :
    "Exported by FFXIV Loose Texture Compiler") + @""",
  ""Version"": """ + modVersion + @""",
  ""Website"": """ + modWebsite + @""",
  ""ModTags"": []
}";
            if (metaFilePath != null) {
                using (StreamWriter writer = new StreamWriter(metaFilePath)) {
                    writer.WriteLine(metaText);
                }
            }
        }
        private TextureSet AddBody(int gender, int baseBody, int race, int tail, bool uniqueAuRa = false) {
            TextureSet textureSet = new TextureSet();
            textureSet.TextureSetName = _bodyNames[baseBody] + (_bodyNames[baseBody].ToLower().Contains("tail") ? " " +
                (tail + 1) : "") + ", " + (race == 5 ? "Unisex" : gender)
                + ", " + _races[race];
            AddBodyPaths(textureSet, gender, baseBody, race, tail, uniqueAuRa);
            return textureSet;
        }

        private TextureSet AddFace(int faceType, int facePart, int faceExtra, int gender, int race, int subRace, int auraScales, bool asym) {
            TextureSet textureSet = new TextureSet();
            textureSet.TextureSetName = _faceParts[facePart] + (facePart == 4 ? " "
                + (faceExtra + 1) : "") + ", " + (facePart != 4 ? _genders[gender] : "Unisex")
                + ", " + (facePart != 4 ? _subRaces[subRace] : "Multi Race") + ", "
                + (facePart != 4 ? _faceTypes[faceType] : "Multi Face");
            switch (facePart) {
                default:
                    AddFacePaths(textureSet, subRace, facePart, faceType, gender, auraScales, asym);
                    break;
                case 2:
                    AddEyePaths(textureSet, subRace, faceType, gender, auraScales, asym);
                    break;
                case 4:
                    AddDecalPath(textureSet, faceExtra);
                    break;
                case 5:
                    AddHairPaths(textureSet, gender, facePart, faceExtra, race, subRace);
                    break;
            }
            textureSet.IgnoreMultiGeneration = true;
            textureSet.BackupTexturePaths = null;
            return textureSet;
        }
        private void AddBodyPaths(TextureSet textureSet, int gender, int baseBody, int race, int tail, bool uniqueAuRa = false) {
            if (race != 3 || baseBody != 6) {
                textureSet.InternalDiffusePath = RacePaths.GetBodyTexturePath(0, gender,
                  baseBody, race, tail, uniqueAuRa);
            }
            textureSet.InternalNormalPath = RacePaths.GetBodyTexturePath(1, gender,
                  baseBody, race, tail, uniqueAuRa);

            textureSet.InternalMultiPath = RacePaths.GetBodyTexturePath(2, gender,
                  baseBody, race, tail, uniqueAuRa);
            BackupTexturePaths.AddBackupPaths(gender, race, textureSet);
        }

        private void AddDecalPath(TextureSet textureSet, int faceExtra) {
            textureSet.InternalDiffusePath = RacePaths.GetFaceTexturePath(faceExtra);
        }

        private void AddHairPaths(TextureSet textureSet, int gender, int facePart, int faceExtra, int race, int subrace) {
            textureSet.TextureSetName = _faceParts[facePart] + " " + (faceExtra + 1)
                + ", " + _genders[gender] + ", " + _races[race];

            textureSet.InternalNormalPath = RacePaths.GetHairTexturePath(1, faceExtra,
                gender, race, subrace);

            textureSet.InternalMultiPath = RacePaths.GetHairTexturePath(2, faceExtra,
                gender, race, subrace);
        }

        private void AddEyePaths(TextureSet textureSet, int subrace, int faceType, int gender, int auraScales, bool asym) {
            textureSet.InternalDiffusePath = RacePaths.GetFaceTexturePath(1, gender, subrace,
            2, faceType, auraScales, asym);

            textureSet.InternalNormalPath = RacePaths.GetFaceTexturePath(2, gender, subrace,
            2, faceType, auraScales, asym);

            textureSet.InternalMultiPath = RacePaths.GetFaceTexturePath(3, gender, subrace,
            2, faceType, auraScales, asym);
        }

        private void AddFacePaths(TextureSet textureSet, int subrace, int facePart, int faceType, int gender, int auraScales, bool asym) {
            if (facePart != 1) {
                textureSet.InternalDiffusePath = RacePaths.GetFaceTexturePath(0, gender, subrace,
                    facePart, faceType, auraScales, asym);
            }

            textureSet.InternalNormalPath = RacePaths.GetFaceTexturePath(1, gender, subrace,
            facePart, faceType, auraScales, asym);

            textureSet.InternalMultiPath = RacePaths.GetFaceTexturePath(2, gender, subrace,
            facePart, faceType, auraScales, asym);

            if (facePart == 0) {
                if (subrace == 10 || subrace == 11) {
                    if (auraScales > 0) {
                        if (faceType < 4) {
                            if (asym) {
                                textureSet.NormalCorrection = Path.Combine(_textureProcessor.BasePath,
                                      @"res\textures\s" + (gender == 0 ? "m" : "f") + faceType + "a.png");
                            } else {
                                textureSet.NormalCorrection = Path.Combine(_textureProcessor.BasePath,
                                    @"res\textures\s" + (gender == 0 ? "m" : "f") + faceType + ".png");
                            }
                        }
                    }
                }
            }
        }
        public async Task<bool> Export(bool finalize, List<TextureSet> exportTextureSets, string path, string name) {
            if (!_lockDuplicateGeneration) {
                _exportStatus = "Initializing";
                _lockDuplicateGeneration = true;
                List<TextureSet> textureSets = new List<TextureSet>();
                string jsonFilepath = Path.Combine(path, "default_mod.json");
                string metaFilePath = Path.Combine(path, "meta.json");
                foreach (TextureSet item in exportTextureSets) {
                    if (item.OmniExportMode) {
                        UniversalTextureSetCreator.ConfigureOmniConfiguration(item);
                    }
                    textureSets.Add(item);
                }
                Directory.CreateDirectory(path);
                _textureProcessor.CleanGeneratedAssets(path);
                await _textureProcessor.Export(textureSets, new Dictionary<string, int>(), path, 1, false, false, File.Exists(_xNormalPath) && finalize, _xNormalPath);
                ExportJson(jsonFilepath);
                ExportMeta(metaFilePath, name);
                Thread.Sleep(100);
                Ipc.AddMod.Subscriber(_pluginInterface).Invoke(name);
                Ipc.ReloadMod.Subscriber(_pluginInterface).Invoke(path, name);
                string collection = Ipc.GetCollectionForObject.Subscriber(_pluginInterface).Invoke(0).Item3;
                Ipc.TrySetMod.Subscriber(_pluginInterface).Invoke(collection, path, name, true);
                Ipc.TrySetModPriority.Subscriber(_pluginInterface).Invoke(collection, path, name, 100);
                var settings = Ipc.GetCurrentModSettings.Subscriber(_pluginInterface).Invoke(collection, path, name, true);
                foreach (var group in settings.Item2.Value.Item3) {
                    Ipc.TrySetModSetting.Subscriber(_pluginInterface).Invoke(collection, path, name, group.Key, "Enable");
                }
                Thread.Sleep(300);
                Ipc.RedrawObject.Subscriber(_pluginInterface).Invoke(plugin.ClientState.LocalPlayer, Penumbra.Api.Enums.RedrawType.Redraw);
                Thread.Sleep(200);
                Ipc.RedrawObject.Subscriber(_pluginInterface).Invoke(plugin.ClientState.LocalPlayer, Penumbra.Api.Enums.RedrawType.Redraw);
                _lockDuplicateGeneration = false;
            }
            return true;
        }
        private static readonly string[] ValidTextureExtensions = new[]
{
          ".png",
          ".dds",
          ".bmp",
          ".tex",
        };
        private readonly string _xNormalPath;
    }
}