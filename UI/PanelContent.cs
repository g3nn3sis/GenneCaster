// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using GenneCaster.Base;
using GenneCaster.Game;
using GenneCaster.GameCamera;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
using GenneCaster.Replay;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace GenneCaster.UI
{
    internal sealed class PanelContent
    {
        private const float ListHeight = 160f;
        private static readonly HashSet<string> NonZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "none", "count", "all", "max", "unknown" };

        private readonly PanelOrchestrator owner;
        private readonly GTZone[] zones;
        private readonly string[] zoneNames;
        private readonly Action[] zoneButtons;
        private readonly Action[] themeButtons;
        private Vector2 mapScroll;
        private Vector2 themeScroll;
        private Vector2 presetScroll;
        private Vector2 replayScroll;
        private Vector2 replayListScroll;
        private string roomToJoin = string.Empty;
        private string playerName;
        private string presetName = string.Empty;
        private string[] presetNames = Array.Empty<string>();
        private Action[] presetLoad = Array.Empty<Action>();
        private Action[] presetDelete = Array.Empty<Action>();
        private bool presetsDirty = true;

        internal readonly Action<Panel>[] DrawContent;

        internal PanelContent(PanelOrchestrator owner)
        {
            this.owner = owner;
            DrawContent = new Action<Panel>[] { DrawMain, DrawUniversal, DrawWasd, DrawCamera, DrawPlayerSettings, DrawTime, DrawMapLoader, DrawPlayer, DrawNametag, DrawTheme, DrawMiniMap, DrawLeaderboard, DrawClosestLava, DrawScoreboard, DrawKillFeed, DrawPresets, DrawReplay };
            zones = ((GTZone[])Enum.GetValues(typeof(GTZone))).Distinct().Where(z => !NonZones.Contains(z.ToString())).ToArray();
            zoneNames = zones.Select(z => DisplayNames.Humanize(z.ToString())).ToArray();
            zoneButtons = new Action[zones.Length];
            for (int i = 0; i < zones.Length; i++)
            {
                GTZone zone = zones[i];
                zoneButtons[i] = () => LoadZone(zone);
            }
            themeButtons = new Action[ColorSpace.Themes.Length];
            for (int i = 0; i < themeButtons.Length; i++)
            {
                int index = i;
                themeButtons[i] = () => owner.SetTheme(index);
            }
            playerName = LocalPlayerManager.NormalizeName(CasterConfig.PlayerName.Value);
        }

        private void DrawMain(Panel panel)
        {
            owner.DrawSwitch(panel, PanelId.Universal);
            owner.DrawSwitch(panel, PanelId.Wasd);
            panel.Label(RoomStatus());
            panel.Label("Room Code");
            roomToJoin = panel.TextField(roomToJoin, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName);
            if (PhotonNetwork.InRoom) panel.Button("Leave Room", LocalPlayerManager.LeaveRoom);
            else panel.Button("Join Room", () => LocalPlayerManager.AttemptRoomJoin(roomToJoin, JoinType.Solo), roomToJoin.Length > 0);
            panel.Button("Join Public", () => LocalPlayerManager.AttemptPublicRoomJoin(JoinType.Solo, true), !PhotonNetwork.InRoom);
        }

        private static string RoomStatus()
        {
            var room = PhotonNetwork.CurrentRoom;
            return PhotonNetwork.InRoom && room != null
                ? $"In room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})"
                : "Not in a room";
        }

        private void DrawUniversal(Panel panel)
        {
            owner.DrawSwitches(panel);
            panel.Button("Reset Layout", owner.ResetLayout);
        }

        private void DrawWasd(Panel panel)
        {
            if (CameraManager.mode == CameraMode.Freecam) panel.Label("WASD is off in Freecam mode");
            else GorillaController.wasd = panel.Toggle("WASD", GorillaController.wasd);
            GorillaController.mouseClick = panel.Toggle("Mouse Click", GorillaController.mouseClick);
            DrawFlightSpeeds(panel);
        }

        private static void DrawFlightSpeeds(Panel panel)
        {
            GorillaController.MovementSpeed = panel.Slider("Movement Speed", GorillaController.MovementSpeed, 5f, 35f, "F1", " m/s");
            GorillaController.MouseSensitivity = panel.Slider("Mouse Sensitivity", GorillaController.MouseSensitivity, 0.05f, 2f);
        }

        private void DrawCamera(Panel panel)
        {
            panel.Label($"Mode: {CameraManager.mode}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Default", () => CameraManager.mode = CameraMode.Default, CameraManager.mode != CameraMode.Default);
                panel.Button("Spectator", () => CameraManager.mode = CameraMode.Spectator, CameraManager.mode != CameraMode.Spectator);
                panel.Button("Freecam", () => CameraManager.mode = CameraMode.Freecam, CameraManager.mode != CameraMode.Freecam);
            });
            if (CameraManager.mode == CameraMode.Freecam) DrawFlightSpeeds(panel);
            CameraManager.fieldOfView = panel.Slider("Field Of View", CameraManager.fieldOfView, 10f, 130f, "F0", "°");
            CameraManager.nearClipPlane = panel.Slider("Near Clip", CameraManager.nearClipPlane, 0.01f, 0.99f);
            CasterConfig.HeadDistance.Value = panel.Slider("Head Distance", CasterConfig.HeadDistance.Value, 0f, 0.99f);
            CasterConfig.OrbitSpeed.Value = panel.Slider("Orbit Speed", CasterConfig.OrbitSpeed.Value, 10f, 600f, "F0", "°/s");
            CasterConfig.PositionSmoothing.Value = panel.Slider("Position Smoothing", CasterConfig.PositionSmoothing.Value, 0f, 0.95f);
            CasterConfig.RotationSmoothing.Value = panel.Slider("Rotation Smoothing", CasterConfig.RotationSmoothing.Value, 0f, 0.75f);
            CasterConfig.OrbitAutoRotate.Value = panel.Toggle("Auto Orbit", CasterConfig.OrbitAutoRotate.Value);
            panel.HorizontalArea(() =>
            {
                panel.Button("FOV 60", () => CameraManager.fieldOfView = 60f);
                panel.Button("FOV 90", () => CameraManager.fieldOfView = 90f);
                panel.Button("FOV 110", () => CameraManager.fieldOfView = 110f);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("First Person", () => CasterConfig.HeadDistance.Value = 0f);
                panel.Button("Orbit", () => CasterConfig.HeadDistance.Value = Mathf.Max(0.5f, CasterConfig.HeadDistance.Value));
            });
            panel.Button("Reset Orbit", SpectatorCamera.ResetOrbit);
        }

        private void DrawPlayerSettings(Panel panel)
        {
            LocalPlayerManager.microphoneStatus = panel.Toggle("Microphone", LocalPlayerManager.microphoneStatus, "All Chat", "Push To Talk");
            CameraManager.audioListener = panel.Toggle("Audio Listener", CameraManager.audioListener);
            NetworkedPlayerManager.registerNewRigs = panel.Toggle("Register New Rigs", NetworkedPlayerManager.registerNewRigs);
            CasterConfig.DiscordPresence.Value = panel.Toggle("Discord Presence", CasterConfig.DiscordPresence.Value);
            CasterConfig.DisableVSync.Value = panel.Toggle("Disable VSync", CasterConfig.DisableVSync.Value);
            CasterConfig.DisableAfkKick.Value = panel.Toggle("Disable AFK Kick", CasterConfig.DisableAfkKick.Value);
            NetworkedPlayerManager.bodyLerping = panel.Slider("Rig Interpolation", NetworkedPlayerManager.bodyLerping, 0.1f, 2.5f, "F2", "x");
        }

        private void DrawTime(Panel panel)
        {
            panel.HorizontalArea(() =>
            {
                panel.Button("Morning", TimeManager.SetMorning);
                panel.Button("Day", TimeManager.SetDay);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Evening", TimeManager.SetEvening);
                panel.Button("Night", TimeManager.SetNight);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Start Rain", TimeManager.StartRain);
                panel.Button("Stop Rain", TimeManager.StopRain);
            });
        }

        private void DrawMapLoader(Panel panel)
        {
            mapScroll = panel.ScrollArea(mapScroll, ListHeight, zones.Length, () =>
            {
                for (int i = 0; i < zones.Length; i++)
                    panel.Button(zoneNames[i], zoneButtons[i]);
            });
        }

        private static void LoadZone(GTZone zone)
        {
            try { ZoneManagement.SetActiveZone(zone); }
            catch (Exception e) { Log.Exception(e, $"Loading {zone} failed"); }
        }

        private void DrawPlayer(Panel panel)
        {
            playerName = panel.TextField(playerName, LocalPlayerManager.MaxNameLength, LocalPlayerManager.NormalizeName);
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

        private void DrawNametag(Panel panel)
        {
            NameTag.enabled = panel.Toggle("NameTags", NameTag.enabled);
            NameTag.usePlayerColor = panel.Toggle("Player Color", NameTag.usePlayerColor);
            NameTag.hzDisplay = panel.Toggle("Player FPS", NameTag.hzDisplay);
            NameTag.platformIndicator = panel.Toggle("Platform Indicator", NameTag.platformIndicator);
            NameTag.currentFontIndex = FontPicker(panel, NameTag.currentFontIndex);
        }

        private void DrawTheme(Panel panel)
        {
            panel.Label($"Current: {ColorSpace.Current.Name}");
            themeScroll = panel.ScrollArea(themeScroll, ListHeight, ColorSpace.Themes.Length, () =>
            {
                for (int i = 0; i < ColorSpace.Themes.Length; i++)
                    panel.Button(ColorSpace.Themes[i].Name, themeButtons[i]);
            });
        }

        private void DrawMiniMap(Panel panel)
        {
            MiniMap.enabled = panel.Toggle("MiniMap", MiniMap.enabled);
            MiniMap.draggable = panel.Toggle("Draggable", MiniMap.draggable);
            MiniMap.miniMapSize = Mathf.Round(panel.Slider("Size", MiniMap.miniMapSize, 100f, 520f, "F0", " px"));
            MiniMap.cameraHeight = panel.Slider("Camera Height", MiniMap.cameraHeight, 1f, 50f, "F0", " m");
            panel.Button("Reset Position", MiniMap.ResetPosition);
        }

        private void DrawLeaderboard(Panel panel)
        {
            Leaderboard.enabled = panel.Toggle("Leaderboard", Leaderboard.enabled);
            CasterConfig.LeaderboardPlatformIcons.Value = panel.Toggle("Platform Icons", CasterConfig.LeaderboardPlatformIcons.Value);
            CasterConfig.LeaderboardFps.Value = panel.Toggle("Show FPS", CasterConfig.LeaderboardFps.Value);
            HudPlacement(panel, CasterConfig.LeaderboardScale, CasterConfig.LeaderboardOffsetX, CasterConfig.LeaderboardOffsetY);
            DrawHudFont(panel);
        }

        private void DrawClosestLava(Panel panel)
        {
            ClosestLava.enabled = panel.Toggle("Closest Lava", ClosestLava.enabled);
            HudPlacement(panel, CasterConfig.ClosestLavaScale, CasterConfig.ClosestLavaOffsetX, CasterConfig.ClosestLavaOffsetY);
            DrawHudFont(panel);
        }

        private void DrawScoreboard(Panel panel)
        {
            Scoreboard.enabled = panel.Toggle("Scoreboard", Scoreboard.enabled);
            HudPlacement(panel, CasterConfig.ScoreboardScale, CasterConfig.ScoreboardOffsetX, CasterConfig.ScoreboardOffsetY);

            CasterConfig.ScoreboardTeamAName.Value = panel.TextField(CasterConfig.ScoreboardTeamAName.Value, 16);
            panel.HorizontalArea(() =>
            {
                panel.Button($"- {CasterConfig.ScoreboardTeamAScore.Value}", () => Scoreboard.AddScore(Scoreboard.ConfigEntryRef.A, -1));
                panel.Button("+", () => Scoreboard.AddScore(Scoreboard.ConfigEntryRef.A, 1));
            });

            CasterConfig.ScoreboardTeamBName.Value = panel.TextField(CasterConfig.ScoreboardTeamBName.Value, 16);
            panel.HorizontalArea(() =>
            {
                panel.Button($"- {CasterConfig.ScoreboardTeamBScore.Value}", () => Scoreboard.AddScore(Scoreboard.ConfigEntryRef.B, -1));
                panel.Button("+", () => Scoreboard.AddScore(Scoreboard.ConfigEntryRef.B, 1));
            });

            panel.Label($"Timer: {Scoreboard.TimeString}");
            panel.HorizontalArea(() =>
            {
                panel.Button(Scoreboard.Running ? "Pause" : "Start", Scoreboard.ToggleRun);
                panel.Button("Reset", Scoreboard.ResetTimer);
            });
            panel.Label($"Timer: {CasterConfig.ScoreboardStartKey.Value} start / {CasterConfig.ScoreboardPauseKey.Value} pause / {CasterConfig.ScoreboardResetKey.Value} reset");
            panel.Label($"Score: {CasterConfig.ScoreboardTeamAUpKey.Value}/{CasterConfig.ScoreboardTeamADownKey.Value} team A, {CasterConfig.ScoreboardTeamBUpKey.Value}/{CasterConfig.ScoreboardTeamBDownKey.Value} team B");
            DrawHudFont(panel);
        }

        private void DrawKillFeed(Panel panel)
        {
            KillFeed.enabled = panel.Toggle("Kill Feed", KillFeed.enabled);
            HudPlacement(panel, CasterConfig.KillFeedScale, CasterConfig.KillFeedOffsetX, CasterConfig.KillFeedOffsetY);
            DrawHudFont(panel);
        }

        private static void HudPlacement(Panel panel, ConfigEntry<float> scale, ConfigEntry<float> offsetX, ConfigEntry<float> offsetY)
        {
            scale.Value = panel.Slider("Scale", scale.Value, 0.5f, 2f, "F2", "x");
            offsetX.Value = panel.Slider("Offset X", offsetX.Value, -800f, 800f, "F0", "px");
            offsetY.Value = panel.Slider("Offset Y", offsetY.Value, -500f, 500f, "F0", "px");
        }

        private static void DrawHudFont(Panel panel) => CasterConfig.HudFont.Value = FontPicker(panel, CasterConfig.HudFont.Value);

        private static int FontPicker(Panel panel, int fontIndex)
        {
            int result = fontIndex;
            panel.Label($"Font: {AssetManager.FontName(fontIndex)}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous Font", () => result = ShiftFont(fontIndex, -1));
                panel.Button("Next Font", () => result = ShiftFont(fontIndex, 1));
            });
            return result;
        }

        private static int ShiftFont(int fontIndex, int amount)
        {
            int count = AssetManager.discoveredFonts.Length;
            if (count == 0) return fontIndex;
            int current = Mathf.Clamp(fontIndex, 0, count - 1);
            return (current + count + amount) % count;
        }

        private void DrawPresets(Panel panel)
        {
            if (presetsDirty) RefreshPresets();

            bool autoSave = panel.Toggle("Autosave", CasterConfig.AutoSave.Value);
            if (autoSave != CasterConfig.AutoSave.Value)
            {
                CasterConfig.AutoSave.Value = autoSave;
                CasterConfig.ForceSave();
            }
            panel.Button("Save Settings", CasterConfig.ForceSave);

            presetName = panel.TextField(presetName, 24, Presets.Sanitize);
            panel.Button("Save Preset", SaveCurrentPreset, presetName.Trim().Length > 0 && !Presets.IsReserved(presetName.Trim()));

            if (presetNames.Length == 0)
            {
                panel.Label("No presets saved.");
                return;
            }
            presetScroll = panel.ScrollArea(presetScroll, ListHeight, presetNames.Length, () =>
            {
                for (int i = 0; i < presetNames.Length; i++)
                {
                    int index = i;
                    panel.HorizontalArea(() =>
                    {
                        panel.Button(presetNames[index], presetLoad[index]);
                        panel.Button("x", presetDelete[index]);
                    });
                }
            });
        }

        private void DrawReplay(Panel panel)
        {
            if (ReplaySession.Player is ReplayPlayer replay)
            {
                DrawReplayPlayback(panel, replay);
                return;
            }
            if (ReplaySession.IsRecording)
            {
                panel.Label($"Recording {ReplayTimeline.Format(ReplaySession.RecordedSeconds)} / {ReplayTimeline.Format(ReplaySession.MaxSeconds)}");
                panel.Button("Stop Recording", ReplaySession.StopRecording);
                return;
            }
            panel.Button("Record", ReplaySession.StartRecording, !ReplaySession.IsLoading);
            if (ReplaySession.IsSaving) panel.Label("Saving replay...");
            if (ReplaySession.IsLoading) panel.Label($"Loading {ReplaySession.LoadingName}...");
            DrawSavedReplays(panel);
        }

        private void DrawSavedReplays(Panel panel)
        {
            string[] names = ReplaySession.SavedReplays;
            if (names.Length == 0)
            {
                panel.Label("No saved replays.");
                return;
            }
            replayListScroll = panel.ScrollArea(replayListScroll, ListHeight, names.Length, () =>
            {
                foreach (string name in names)
                {
                    panel.HorizontalArea(() =>
                    {
                        panel.Button(name, () => ReplaySession.Play(name), !ReplaySession.IsLoading);
                        panel.Button("x", () => ReplaySession.Delete(name));
                    });
                }
            });
        }

        private void DrawReplayPlayback(Panel panel, ReplayPlayer replay)
        {
            panel.Label($"{ReplayTimeline.Format(replay.Cursor)} / {ReplayTimeline.Format(replay.Recording.Duration)}");
            panel.HorizontalArea(() =>
            {
                panel.Button(replay.Playing ? "Pause" : "Play", replay.TogglePlaying);
                panel.Button("Stop", ReplaySession.ExitReplay);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Spectate", () => replay.Freecam = false, replay.Freecam);
                panel.Button("FreeCam", () => replay.Freecam = true, !replay.Freecam);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous", () => replay.SpectateNext(-1));
                panel.Button("Next", () => replay.SpectateNext(1));
            });
            replay.RigLerp = panel.Slider("Rig Lerp", replay.RigLerp, ReplayPlayer.MinRigLerp, ReplayPlayer.MaxRigLerp);
            panel.Label(replay.Freecam ? "FreeCam: hold right mouse, WASD to fly" : $"Spectating: {replay.Spectated?.Actor.Name ?? "nobody"}");
            IReadOnlyList<ReplayPuppet> puppets = replay.Puppets;
            replayScroll = panel.ScrollArea(replayScroll, ListHeight, puppets.Count, () =>
            {
                for (int i = 0; i < puppets.Count; i++)
                {
                    int index = i;
                    panel.Button(puppets[i].Actor.Name, () => replay.Spectate(index), puppets[i].Visible && !replay.IsSpectating(puppets[i]));
                }
            });
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
            for (int i = 0; i < presetNames.Length; i++)
            {
                string name = presetNames[i];
                presetLoad[i] = () => { if (Presets.Load(name)) owner.ReapplyFromConfig(); };
                presetDelete[i] = () => { Presets.Delete(name); presetsDirty = true; };
            }
        }
    }
}
