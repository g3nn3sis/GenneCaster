// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.Player;
using Zetori.Replay;
using UnityEngine;

namespace Zetori.UI.Panels
{
    internal sealed class SetupPanels
    {
        private const int PresetNameLength = 24;
        private const string NoDefaultPreset = "None";

        private readonly PanelOrchestrator owner;
        private readonly Action[] themeButtons;
        private readonly Action nextDesktopDefault;
        private readonly Action previousDesktopDefault;
        private readonly Action nextVrDefault;
        private readonly Action previousVrDefault;
        private Vector2 themeScroll;
        private Vector2 presetScroll;
        private string presetName = string.Empty;
        private string[] presetNames = Array.Empty<string>();
        private Action[] presetLoad = Array.Empty<Action>();
        private Action[] presetDelete = Array.Empty<Action>();
        private Action[] presetRename = Array.Empty<Action>();
        private bool presetsDirty = true;
        private string? renaming;
        private string renameText = string.Empty;

        internal SetupPanels(PanelOrchestrator owner)
        {
            this.owner = owner;
            themeButtons = new Action[ColorSpace.Themes.Length];
            for (int i = 0; i < themeButtons.Length; i++)
            {
                int index = i;
                themeButtons[i] = () => owner.SetTheme(index);
            }
            nextDesktopDefault = () => StepDefaultPreset(PresetPlatform.Desktop, 1);
            previousDesktopDefault = () => StepDefaultPreset(PresetPlatform.Desktop, -1);
            nextVrDefault = () => StepDefaultPreset(PresetPlatform.Vr, 1);
            previousVrDefault = () => StepDefaultPreset(PresetPlatform.Vr, -1);
        }

        internal static void DrawPlayer(Panel panel)
        {
            string playerName = panel.TextField(CasterConfig.PlayerName.Value, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName);
            CasterConfig.PlayerName.Value = playerName;
            float cooldown = LocalPlayerManager.CooldownRemaining;
            string wait = cooldown > 0f ? $" ({Mathf.CeilToInt(cooldown)}s)" : string.Empty;
            panel.Button($"Set Name{wait}", () => LocalPlayerManager.ChangeName(playerName), playerName.Length > 0 && cooldown <= 0f);
            LocalPlayerManager.Red = panel.Slider("Red", LocalPlayerManager.Red, 0f, 1f);
            LocalPlayerManager.Green = panel.Slider("Green", LocalPlayerManager.Green, 0f, 1f);
            LocalPlayerManager.Blue = panel.Slider("Blue", LocalPlayerManager.Blue, 0f, 1f);
            panel.ColorSwatch(new Color(LocalPlayerManager.Red, LocalPlayerManager.Green, LocalPlayerManager.Blue));
            panel.Button($"Apply Color{wait}", LocalPlayerManager.ChangeColor, cooldown <= 0f);
        }

        internal static void DrawSettings(Panel panel)
        {
            LocalPlayerManager.microphoneStatus = panel.Toggle("Microphone", LocalPlayerManager.microphoneStatus, "All Chat", "Push To Talk");
            CameraManager.audioListener = panel.Toggle("Audio Listener", CameraManager.audioListener);
            NetworkedPlayerManager.registerNewRigs = panel.Toggle("Register New Rigs", NetworkedPlayerManager.registerNewRigs);
            CasterConfig.DiscordPresence.Value = panel.Toggle("Discord Presence", CasterConfig.DiscordPresence.Value);
            CasterConfig.DisableVSync.Value = panel.Toggle("Disable VSync", CasterConfig.DisableVSync.Value);
            CasterConfig.DisableAfkKick.Value = panel.Toggle("Disable AFK Kick", CasterConfig.DisableAfkKick.Value);
            NetworkedPlayerManager.bodyLerping = panel.Slider("Rig Interpolation", NetworkedPlayerManager.bodyLerping, 0.1f, 2.5f, "F2", "x");
            DrawRigExaggeration(panel);
        }

        private static void DrawRigExaggeration(Panel panel)
        {
            ArmExaggeration.Enabled = panel.Toggle("Rig Exaggeration", ArmExaggeration.Enabled);
            if (!ArmExaggeration.Enabled) return;
            ArmExaggeration.Amount = panel.Slider("Arm Movement", ArmExaggeration.Amount, ArmExaggeration.MinimumAmount, ArmExaggeration.MaximumAmount, "F2", "x");
            if (ReplaySession.IsRecording) panel.Label("Paused while recording");
        }

        internal void DrawUiControl(Panel panel)
        {
            DrawUiScale(panel);
            CasterConfig.SnapPanels.Value = panel.Toggle("Panel Snapping", CasterConfig.SnapPanels.Value);
            DrawThemes(panel);
        }

        private static void DrawUiScale(Panel panel)
        {
            panel.Label($"UI Scale: {UiScale.Multiplier:F2}x");
            panel.HorizontalArea(() =>
            {
                panel.Button("-", UiScale.Decrease, UiScale.CanDecrease);
                panel.Button("+", UiScale.Increase, UiScale.CanIncrease);
            });
        }

        private void DrawThemes(Panel panel)
        {
            panel.Label($"Current: {ColorSpace.Current.Name}");
            themeScroll = panel.ScrollArea(themeScroll, Panel.ListHeight, ColorSpace.Themes.Length, () =>
            {
                for (int i = 0; i < ColorSpace.Themes.Length; i++)
                    panel.Button(ColorSpace.Themes[i].Name, themeButtons[i]);
            });
        }

        internal void DrawPresets(Panel panel)
        {
            if (presetsDirty) RefreshPresets();

            bool autoSave = panel.Toggle("Autosave", CasterConfig.AutoSave.Value);
            if (autoSave != CasterConfig.AutoSave.Value)
            {
                CasterConfig.AutoSave.Value = autoSave;
                CasterConfig.ForceSave();
            }
            panel.Button("Save Settings", CasterConfig.ForceSave);

            presetName = panel.TextField(presetName, PresetNameLength, Presets.Sanitize);
            panel.Button("Save Preset", SaveCurrentPreset, presetName.Trim().Length > 0 && !Presets.IsReserved(presetName.Trim()));

            if (presetNames.Length == 0)
            {
                panel.Label("No presets saved.");
                return;
            }
            panel.Button($"Desktop Default: {DefaultPresetName(PresetPlatform.Desktop)}", nextDesktopDefault, previousDesktopDefault);
            panel.Button($"VR Default: {DefaultPresetName(PresetPlatform.Vr)}", nextVrDefault, previousVrDefault);
            presetScroll =panel.ScrollArea(presetScroll, Panel.ListHeight, presetNames.Length, () =>
            {
                for (int i = 0; i < presetNames.Length; i++)
                {
                    int index = i;
                    if (presetNames[index] == renaming)
                    {
                        DrawRenameRow(panel, presetNames[index]);
                        continue;
                    }
                    panel.HorizontalArea(() =>
                    {
                        panel.Button(presetNames[index], presetLoad[index], presetRename[index]);
                        panel.Button("x", presetDelete[index]);
                    });
                }
            });
        }

        private static string DefaultPresetName(PresetPlatform platform) => PresetDefaults.Get(platform) ?? NoDefaultPreset;

        private void StepDefaultPreset(PresetPlatform platform, int step)
        {
            string? current = PresetDefaults.Get(platform);
            int index = current == null ? -1 : Array.FindIndex(presetNames, name => string.Equals(name, current, StringComparison.OrdinalIgnoreCase));
            int choices = presetNames.Length + 1;
            int next = (index + 1 + step + choices) % choices - 1;
            PresetDefaults.Set(platform, next < 0 ? null : presetNames[next]);
        }

        private void DrawRenameRow(Panel panel, string name)
        {
            panel.HorizontalArea(() =>
            {
                renameText = panel.TextField(renameText, PresetNameLength, Presets.Sanitize);
                panel.Button("Save", () => CommitRename(name), renameText.Trim().Length > 0 && !Presets.IsReserved(renameText.Trim()));
                panel.Button("x", () => renaming = null);
            });
        }

        private void BeginRename(string name)
        {
            renaming = name;
            renameText = name;
        }

        private void CommitRename(string name)
        {
            if (!Presets.Rename(name, renameText.Trim())) return;
            renaming = null;
            presetsDirty = true;
        }

        private void SaveCurrentPreset()
        {
            if (Presets.Save(presetName)) presetsDirty = true;
        }

        private void RefreshPresets()
        {
            presetsDirty = false;
            presetNames = Presets.Names();
            presetLoad = new Action[presetNames.Length];
            presetDelete = new Action[presetNames.Length];
            presetRename = new Action[presetNames.Length];
            for (int i = 0; i < presetNames.Length; i++)
            {
                string name = presetNames[i];
                presetLoad[i] = () => { if (Presets.Load(name)) owner.ReapplyFromConfig(); };
                presetDelete[i] = () => { Presets.Delete(name); presetsDirty = true; };
                presetRename[i] = () => BeginRename(name);
            }
        }
    }
}
