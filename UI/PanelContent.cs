using GenneCaster.Base;
using GenneCaster.Game;
using GenneCaster.GameCamera;
using GenneCaster.GameCamera.Modes;
using GenneCaster.Player;
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
            DrawContent = new Action<Panel>[] { DrawMain, DrawUniversal, DrawWasd, DrawCamera, DrawPlayerSettings, DrawTime, DrawMapLoader, DrawPlayer, DrawNametag, DrawTheme, DrawMiniMap, DrawLeaderboard, DrawClosestLava, DrawScoreboard, DrawKillFeed, DrawPresets };
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

        private static string OnOff(bool value) => value ? "On" : "Off";

        private void DrawMain(Panel panel)
        {
            panel.Button($"Panels: {OnOff(owner.IsVisible(PanelId.Universal))}", () => owner.Toggle(PanelId.Universal));
            panel.Button($"WASD Panel: {OnOff(owner.IsVisible(PanelId.Wasd))}", () => owner.Toggle(PanelId.Wasd));
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
            panel.Button("Camera Manager", () => owner.Toggle(PanelId.Camera));
            panel.Button("Player Settings", () => owner.Toggle(PanelId.PlayerSettings));
            panel.Button("Time Manager", () => owner.Toggle(PanelId.Time));
            panel.Button("Map Loader", () => owner.Toggle(PanelId.MapLoader));
            panel.Button("Player Manager", () => owner.Toggle(PanelId.Player));
            panel.Button("NameTag Manager", () => owner.Toggle(PanelId.Nametag));
            panel.Button("Theme Loader", () => owner.Toggle(PanelId.Theme));
            panel.Button("MiniMap Manager", () => owner.Toggle(PanelId.MiniMap));
            panel.Button("Leaderboard", () => owner.Toggle(PanelId.Leaderboard));
            panel.Button("Closest Lava", () => owner.Toggle(PanelId.ClosestLava));
            panel.Button("Scoreboard", () => owner.Toggle(PanelId.Scoreboard));
            panel.Button("Kill Feed", () => owner.Toggle(PanelId.KillFeed));
            panel.Button("Presets", () => owner.Toggle(PanelId.Presets));
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
            panel.Button("Set Morning", TimeManager.SetMorning);
            panel.Button("Set Day", TimeManager.SetDay);
            panel.Button("Set Evening", TimeManager.SetEvening);
            panel.Button("Set Night", TimeManager.SetNight);
            panel.Button("Start Rain", TimeManager.StartRain);
            panel.Button("Stop Rain", TimeManager.StopRain);
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
            panel.Label($"Font: {AssetManager.FontName(NameTag.currentFontIndex)}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous Font", () => ShiftFont(-1));
                panel.Button("Next Font", () => ShiftFont(1));
            });
            NameTag.enabled = panel.Toggle("NameTags", NameTag.enabled);
            NameTag.usePlayerColor = panel.Toggle("Player Color", NameTag.usePlayerColor);
            NameTag.hzDisplay = panel.Toggle("Player FPS", NameTag.hzDisplay);
            NameTag.platformIndicator = panel.Toggle("Platform Indicator", NameTag.platformIndicator);
        }

        private static void ShiftFont(int amount)
        {
            int count = AssetManager.discoveredFonts.Length;
            if (count == 0) return;
            int current = Mathf.Clamp(NameTag.currentFontIndex, 0, count - 1);
            NameTag.currentFontIndex = (current + count + amount) % count;
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
            CasterConfig.LeaderboardScale.Value = panel.Slider("Scale", CasterConfig.LeaderboardScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.LeaderboardOffsetX.Value = panel.Slider("Offset X", CasterConfig.LeaderboardOffsetX.Value, -800f, 800f, "F0", "px");
            CasterConfig.LeaderboardOffsetY.Value = panel.Slider("Offset Y", CasterConfig.LeaderboardOffsetY.Value, -500f, 500f, "F0", "px");
            DrawHudFont(panel);
        }

        private void DrawClosestLava(Panel panel)
        {
            ClosestLava.enabled = panel.Toggle("Closest Lava", ClosestLava.enabled);
            CasterConfig.ClosestLavaScale.Value = panel.Slider("Scale", CasterConfig.ClosestLavaScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.ClosestLavaOffsetX.Value = panel.Slider("Offset X", CasterConfig.ClosestLavaOffsetX.Value, -800f, 800f, "F0", "px");
            CasterConfig.ClosestLavaOffsetY.Value = panel.Slider("Offset Y", CasterConfig.ClosestLavaOffsetY.Value, -500f, 500f, "F0", "px");
            DrawHudFont(panel);
        }

        private void DrawScoreboard(Panel panel)
        {
            Scoreboard.enabled = panel.Toggle("Scoreboard", Scoreboard.enabled);
            CasterConfig.ScoreboardScale.Value = panel.Slider("Scale", CasterConfig.ScoreboardScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.ScoreboardOffsetX.Value = panel.Slider("Offset X", CasterConfig.ScoreboardOffsetX.Value, -800f, 800f, "F0", "px");
            CasterConfig.ScoreboardOffsetY.Value = panel.Slider("Offset Y", CasterConfig.ScoreboardOffsetY.Value, -500f, 500f, "F0", "px");

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
            CasterConfig.KillFeedScale.Value = panel.Slider("Scale", CasterConfig.KillFeedScale.Value, 0.5f, 2f, "F2", "x");
            CasterConfig.KillFeedOffsetX.Value = panel.Slider("Offset X", CasterConfig.KillFeedOffsetX.Value, -800f, 800f, "F0", "px");
            CasterConfig.KillFeedOffsetY.Value = panel.Slider("Offset Y", CasterConfig.KillFeedOffsetY.Value, -500f, 500f, "F0", "px");
            DrawHudFont(panel);
        }

        private void DrawHudFont(Panel panel)
        {
            panel.Label($"Font: {AssetManager.FontName(CasterConfig.HudFont.Value)}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Previous Font", () => ShiftHudFont(-1));
                panel.Button("Next Font", () => ShiftHudFont(1));
            });
        }

        private static void ShiftHudFont(int amount)
        {
            int count = AssetManager.discoveredFonts.Length;
            if (count == 0) return;
            int current = Mathf.Clamp(CasterConfig.HudFont.Value, 0, count - 1);
            CasterConfig.HudFont.Value = (current + count + amount) % count;
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
            panel.Button("Save Preset", SaveCurrentPreset, presetName.Trim().Length > 0);

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
