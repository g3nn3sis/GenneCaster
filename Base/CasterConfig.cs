// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zetori.Game;
using Zetori.GameCamera;
using Zetori.GameCamera.Vr;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zetori.Base
{
    internal static class CasterConfig
    {
        private const float SaveDelay = 1f;

        private static ConfigFile? file;
        private static bool dirty;
        private static float lastChange;

        internal static ConfigEntry<CameraMode> ActiveCameraMode = null!;
        internal static ConfigEntry<VrCameraMode> VrMode = null!;
        internal static ConfigEntry<float> FieldOfView = null!;
        internal static ConfigEntry<float> NearClipPlane = null!;
        internal static ConfigEntry<bool> AudioListener = null!;
        internal static ConfigEntry<float> HeadDistance = null!;
        internal static ConfigEntry<float> PositionSmoothing = null!;
        internal static ConfigEntry<float> RotationSmoothing = null!;
        internal static ConfigEntry<float> OrbitSpeed = null!;
        internal static ConfigEntry<bool> OrbitAutoRotate = null!;
        internal static ConfigEntry<float> BodyLerping = null!;
        internal static ConfigEntry<bool> RigExaggeration = null!;
        internal static ConfigEntry<float> RigExaggerationAmount = null!;
        internal static ConfigEntry<bool> RegisterNewRigs = null!;
        internal static ConfigEntry<bool> Microphone = null!;
        internal static ConfigEntry<Key> PushToTalkKey = null!;
        internal static ConfigEntry<Key> NextPlayerKey = null!;
        internal static ConfigEntry<Key> PreviousPlayerKey = null!;
        internal static ConfigEntry<Key> ReplayPauseKey = null!;
        internal static ConfigEntry<Key> ReplayTimelineKey = null!;
        internal static ConfigEntry<Key> OrbitLeftKey = null!;
        internal static ConfigEntry<Key> OrbitRightKey = null!;
        internal static ConfigEntry<Key> OrbitUpKey = null!;
        internal static ConfigEntry<Key> OrbitDownKey = null!;
        internal static ConfigEntry<Key> OrbitResetKey = null!;

        internal static ConfigEntry<string> WebName = null!;
        internal static ConfigEntry<bool> WebDynamic = null!;
        internal static ConfigEntry<Key> WebNextNodeKey = null!;
        internal static ConfigEntry<Key> WebPreviousNodeKey = null!;
        internal static ConfigEntry<Key> WebPlaceNodeKey = null!;
        internal static ConfigEntry<Key> WebRemoveNodeKey = null!;

        internal static ConfigEntry<bool> Wasd = null!;
        internal static ConfigEntry<bool> MouseClick = null!;
        internal static ConfigEntry<float> MovementSpeed = null!;
        internal static ConfigEntry<float> MouseSpeed = null!;
        internal static ConfigEntry<Key> FlyForwardKey = null!;
        internal static ConfigEntry<Key> FlyBackKey = null!;
        internal static ConfigEntry<Key> FlyLeftKey = null!;
        internal static ConfigEntry<Key> FlyRightKey = null!;
        internal static ConfigEntry<Key> FlyUpKey = null!;
        internal static ConfigEntry<Key> FlyDownKey = null!;
        internal static ConfigEntry<Key> NoClipKey = null!;

        internal static ConfigEntry<bool> NameTags = null!;
        internal static ConfigEntry<bool> NameTagPlayerColor = null!;
        internal static ConfigEntry<bool> NameTagFps = null!;
        internal static ConfigEntry<bool> NameTagPlatform = null!;
        internal static ConfigEntry<int> NameTagFont = null!;
        internal static ConfigEntry<bool> MiniMapEnabled = null!;
        internal static ConfigEntry<bool> MiniMapDraggable = null!;
        internal static ConfigEntry<float> MiniMapSize = null!;
        internal static ConfigEntry<float> MiniMapHeight = null!;
        internal static ConfigEntry<float> MiniMapOffsetX = null!;
        internal static ConfigEntry<float> MiniMapOffsetY = null!;
        internal static ConfigEntry<HudPosition> MiniMapPosition = null!;
        internal static ConfigEntry<bool> DiscordPresence = null!;

        internal static ConfigEntry<int> HudFont = null!;

        internal static ConfigEntry<bool> LeaderboardEnabled = null!;
        internal static ConfigEntry<float> LeaderboardScale = null!;
        internal static ConfigEntry<bool> LeaderboardPlatformIcons = null!;
        internal static ConfigEntry<bool> LeaderboardFps = null!;
        internal static ConfigEntry<float> LeaderboardOffsetX = null!;
        internal static ConfigEntry<float> LeaderboardOffsetY = null!;
        internal static ConfigEntry<HudPosition> LeaderboardPosition = null!;

        internal static ConfigEntry<bool> ClosestLavaEnabled = null!;
        internal static ConfigEntry<float> ClosestLavaScale = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetX = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetY = null!;

        internal static ConfigEntry<bool> ScoreboardEnabled = null!;
        internal static ConfigEntry<float> ScoreboardScale = null!;
        internal static ConfigEntry<float> ScoreboardOffsetX = null!;
        internal static ConfigEntry<float> ScoreboardOffsetY = null!;
        internal static ConfigEntry<HudPosition> ScoreboardPosition = null!;
        internal static ConfigEntry<string> ScoreboardTeamAName = null!;
        internal static ConfigEntry<string> ScoreboardTeamBName = null!;
        internal static ConfigEntry<int> ScoreboardTeamAScore = null!;
        internal static ConfigEntry<int> ScoreboardTeamBScore = null!;
        internal static ConfigEntry<Key> ScoreboardStartKey = null!;
        internal static ConfigEntry<Key> ScoreboardPauseKey = null!;
        internal static ConfigEntry<Key> ScoreboardResetKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamAUpKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamADownKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamBUpKey = null!;
        internal static ConfigEntry<Key> ScoreboardTeamBDownKey = null!;

        internal static ConfigEntry<bool> KillFeedEnabled = null!;
        internal static ConfigEntry<float> KillFeedScale = null!;
        internal static ConfigEntry<float> KillFeedOffsetX = null!;
        internal static ConfigEntry<float> KillFeedOffsetY = null!;
        internal static ConfigEntry<HudPosition> KillFeedPosition = null!;

        internal static ConfigEntry<string> PlayerName = null!;
        internal static ConfigEntry<float> PlayerRed = null!;
        internal static ConfigEntry<float> PlayerGreen = null!;
        internal static ConfigEntry<float> PlayerBlue = null!;

        internal static ConfigEntry<bool> DisableVSync = null!;
        internal static ConfigEntry<bool> DisableAfkKick = null!;

        internal static ConfigEntry<string> Theme = null!;
        internal const float MinimumUiScale = 0.75f;
        internal const float MaximumUiScale = 1f;
        internal const float MaximumPositionSmoothing = 0.95f;
        internal const float MaximumRotationSmoothing = 0.75f;

        internal static ConfigEntry<float> UiScale = null!;
        internal static ConfigEntry<bool> SnapPanels = null!;
        internal static ConfigEntry<Key> MenuKey = null!;
        internal static ConfigEntry<bool> AutoSave = null!;

        internal static void Bind(ConfigFile config)
        {
            file = config;
            config.SaveOnConfigSet = false;

            ActiveCameraMode = config.Bind("Camera", "Mode", CameraMode.Spectator, "Default shows your own first person view, Spectator follows the selected player, Freecam flies the camera on its own, Web watches the action from placed nodes.");
            VrMode = config.Bind("VR Camera", "Mode", VrCameraMode.Handheld, "What the in-game VR camera shows: FirstPerson, ThirdPerson, Follower or Handheld.");
            FieldOfView = Range(config, "Camera", "Field Of View", 100f, 10f, 130f);
            NearClipPlane = Range(config, "Camera", "Near Clip", 0.01f, 0.01f, 0.99f);
            AudioListener = config.Bind("Camera", "Audio Listener", true, "Hear the game from the spectator camera.");
            HeadDistance = Range(config, "Camera", "Head Distance", 0f, 0f, 0.99f, "0 is first person, anything higher orbits the player.");
            PositionSmoothing = Range(config, "Camera", "Position Smoothing", 0.05f, 0f, MaximumPositionSmoothing, "How much the orbit camera lags behind the player. 0 follows instantly.");
            RotationSmoothing = Range(config, "Camera", "Rotation Smoothing", 0f, 0f, MaximumRotationSmoothing, "How much the orbit camera's rotation lags behind. 0 follows instantly.");
            OrbitSpeed = Range(config, "Camera", "Orbit Speed", 150f, 10f, 600f, "Arrow key orbit speed in degrees per second.");
            OrbitAutoRotate = config.Bind("Camera", "Auto Orbit", false, "Slowly rotate the orbit camera around the spectated player with no input. Arrow keys still steer on top.");
            BodyLerping = Range(config, "Camera", "Body Lerping", 1f, 0.1f, 2.5f, "Multiplier on how quickly other players' rigs move to their networked position.");
            RigExaggeration = config.Bind("Camera", "Rig Exaggeration", false, "Make the spectated player's arm movements bigger on your screen only.");
            RigExaggerationAmount = Range(config, "Camera", "Rig Exaggeration Amount", 1.5f, 1f, 3f, "How much the spectated player's arm movements are multiplied.");
            RegisterNewRigs = config.Bind("Camera", "Register New Rigs", true, "Keep the player list updated as players join and leave.");
            Microphone = config.Bind("Camera", "Microphone", false, "On is all chat, off is push to talk.");
            PushToTalkKey = config.Bind("Camera", "Hold To Talk Key", Key.V, "Hold to open the mic to all chat; release returns to the Microphone setting (push to talk by default).");
            NextPlayerKey = config.Bind("Camera", "Next Player Key", Key.RightBracket, "Spectate the next player in the list.");
            PreviousPlayerKey = config.Bind("Camera", "Previous Player Key", Key.LeftBracket, "Spectate the previous player in the list.");
            OrbitLeftKey = config.Bind("Camera", "Orbit Left Key", Key.LeftArrow, "Hold to orbit the spectator camera left.");
            OrbitRightKey = config.Bind("Camera", "Orbit Right Key", Key.RightArrow, "Hold to orbit the spectator camera right.");
            OrbitUpKey = config.Bind("Camera", "Orbit Up Key", Key.UpArrow, "Hold to orbit the spectator camera up.");
            OrbitDownKey = config.Bind("Camera", "Orbit Down Key", Key.DownArrow, "Hold to orbit the spectator camera down.");
            OrbitResetKey = config.Bind("Camera", "Reset Orbit Key", Key.R, "Put the orbit camera back behind the spectated player.");
            ReplayPauseKey = config.Bind("Replay", "Pause Key", Key.P, "Pause and unpause the replay while in replay mode.");
            ReplayTimelineKey = config.Bind("Replay", "Timeline Key", Key.F1, "Show and hide the replay timeline while in replay mode.");

            WebName = config.Bind("Web", "Name", string.Empty, "Name of the saved camera web to use, from the Webs folder.");
            WebDynamic = config.Bind("Web", "Dynamic", true, "On picks the node nearest the action by itself, off steps through nodes with the node keys.");
            WebNextNodeKey = config.Bind("Web", "Next Node Key", Key.RightArrow, "Move the web camera to the next node in manual selection.");
            WebPreviousNodeKey = config.Bind("Web", "Previous Node Key", Key.LeftArrow, "Move the web camera to the previous node in manual selection.");
            WebPlaceNodeKey = config.Bind("Web", "Place Node Key", Key.F2, "Add a node where the camera is while editing the web.");
            WebRemoveNodeKey = config.Bind("Web", "Remove Node Key", Key.F3, "Remove the node nearest the camera while editing the web.");

            Wasd = config.Bind("Freecam", "Enabled", false, "Hold right mouse to fly your own player with WASD. Turned off while the camera is in Freecam mode.");
            MouseClick = config.Bind("Freecam", "Mouse Click", false, "Left click presses whatever is under the cursor with the right hand.");
            MovementSpeed = Range(config, "Freecam", "Movement Speed", 10f, 5f, 35f);
            MouseSpeed = Range(config, "Freecam", "Mouse Sensitivity", 0.3f, 0.05f, 2f);
            FlyForwardKey = config.Bind("Freecam", "Forward Key", Key.W, "Fly forward while holding right mouse.");
            FlyBackKey = config.Bind("Freecam", "Back Key", Key.S, "Fly back while holding right mouse.");
            FlyLeftKey = config.Bind("Freecam", "Left Key", Key.A, "Fly left while holding right mouse.");
            FlyRightKey = config.Bind("Freecam", "Right Key", Key.D, "Fly right while holding right mouse.");
            FlyUpKey = config.Bind("Freecam", "Up Key", Key.Space, "Fly up while holding right mouse.");
            FlyDownKey = config.Bind("Freecam", "Down Key", Key.LeftShift, "Fly down while holding right mouse.");
            NoClipKey = config.Bind("Freecam", "No Clip Key", Key.E, "Hold while flying your own player to pass through walls.");

            NameTags = config.Bind("Nametags", "Enabled", false);
            NameTagPlayerColor = config.Bind("Nametags", "Player Color", false);
            NameTagFps = config.Bind("Nametags", "Player FPS", false);
            NameTagPlatform = config.Bind("Nametags", "Platform Indicator", false);
            NameTagFont = config.Bind("Nametags", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game.", new AcceptableValueRange<int>(0, 255)));

            MiniMapEnabled = config.Bind("MiniMap", "Enabled", false);
            MiniMapDraggable = config.Bind("MiniMap", "Draggable", false);
            MiniMapSize = Range(config, "MiniMap", "Size", 520f, 100f, 520f, "Width and height at 1080p, scaled with the interface.");
            MiniMapHeight = Range(config, "MiniMap", "Camera Height", 8f, 1f, 50f);
            MiniMapOffsetX = Range(config, "MiniMap", "Offset X", 10f, 0f, 8000f, "Distance from the side edge of its corner at 1080p.");
            MiniMapOffsetY = Range(config, "MiniMap", "Offset Y", 10f, 0f, 8000f, "Distance from the top or bottom edge of its corner at 1080p.");
            MiniMapPosition = config.Bind("MiniMap", "Position", HudPosition.TopRight, "Corner the minimap sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            DiscordPresence = config.Bind("Discord", "Rich Presence", true, "Show what you are doing on your Discord profile.");

            HudFont = config.Bind("HUD", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game, shared by the leaderboard and closest-lava overlays.", new AcceptableValueRange<int>(0, 255)));

            LeaderboardEnabled = config.Bind("Leaderboard", "Enabled", false, "Show the player leaderboard.");
            LeaderboardScale = Range(config, "Leaderboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            LeaderboardPlatformIcons = config.Bind("Leaderboard", "Platform Icons", true, "Show each player's Steam or Meta icon.");
            LeaderboardFps = config.Bind("Leaderboard", "Show FPS", false, "Show each player's frame rate next to their name.");
            LeaderboardOffsetX = Range(config, "Leaderboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            LeaderboardOffsetY = Range(config, "Leaderboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            LeaderboardPosition = config.Bind("Leaderboard", "Position", HudPosition.BottomLeft, "Corner the leaderboard sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            ClosestLavaEnabled = config.Bind("ClosestLava", "Enabled", false, "Show the bottom-center distance to the nearest tagged (lava) player from the spectated player.");
            ClosestLavaScale = Range(config, "ClosestLava", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ClosestLavaOffsetX = Range(config, "ClosestLava", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ClosestLavaOffsetY = Range(config, "ClosestLava", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");

            ScoreboardEnabled = config.Bind("Scoreboard", "Enabled", false, "Show the two-team scoreboard with a timer.");
            ScoreboardScale = Range(config, "Scoreboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ScoreboardOffsetX = Range(config, "Scoreboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ScoreboardOffsetY = Range(config, "Scoreboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            ScoreboardPosition = config.Bind("Scoreboard", "Position", HudPosition.TopCenter, "Where the scoreboard sits: TopCenter, BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");
            ScoreboardTeamAName = config.Bind("Scoreboard", "Team A Name", "TEAM A", "Left team name.");
            ScoreboardTeamBName = config.Bind("Scoreboard", "Team B Name", "TEAM B", "Right team name.");
            ScoreboardTeamAScore = config.Bind("Scoreboard", "Team A Score", 0, new ConfigDescription("Left team score.", new AcceptableValueRange<int>(0, 999)));
            ScoreboardTeamBScore = config.Bind("Scoreboard", "Team B Score", 0, new ConfigDescription("Right team score.", new AcceptableValueRange<int>(0, 999)));
            ScoreboardStartKey = config.Bind("Scoreboard", "Start Key", Key.B, "Start the match timer.");
            ScoreboardPauseKey = config.Bind("Scoreboard", "Pause Key", Key.N, "Pause the match timer.");
            ScoreboardResetKey = config.Bind("Scoreboard", "Reset Key", Key.M, "Reset the match timer to the pre-roll.");
            ScoreboardTeamAUpKey = config.Bind("Scoreboard", "Team A Score Up Key", Key.H, "Add a point to team A.");
            ScoreboardTeamADownKey = config.Bind("Scoreboard", "Team A Score Down Key", Key.J, "Remove a point from team A.");
            ScoreboardTeamBUpKey = config.Bind("Scoreboard", "Team B Score Up Key", Key.K, "Add a point to team B.");
            ScoreboardTeamBDownKey = config.Bind("Scoreboard", "Team B Score Down Key", Key.L, "Remove a point from team B.");

            KillFeedEnabled = config.Bind("KillFeed", "Enabled", false, "Show the feed of players as they get tagged.");
            KillFeedScale = Range(config, "KillFeed", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            KillFeedOffsetX = Range(config, "KillFeed", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            KillFeedOffsetY = Range(config, "KillFeed", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
            KillFeedPosition = config.Bind("KillFeed", "Position", HudPosition.TopRight, "Corner the kill feed sits in: BottomLeft, BottomRight or TopRight. Only one overlay can use a corner.");

            PlayerName = config.Bind("Player", "Name", "ZETORI", "Up to 12 letters or digits.");
            PlayerRed = Range(config, "Player", "Red", 0f, 0f, 1f);
            PlayerGreen = Range(config, "Player", "Green", 0f, 0f, 1f);
            PlayerBlue = Range(config, "Player", "Blue", 0f, 0f, 1f);

            DisableVSync = config.Bind("Game", "Disable VSync", true, "Turn off VSync so the spectator view runs at the highest frame rate.");
            DisableAfkKick = config.Bind("Game", "Disable AFK Kick", true, "Stop the game from kicking you for being idle while spectating.");

            Theme = config.Bind("Interface", "Theme Name", "Red", "Name of a theme from the UI Control panel's list.");
            UiScale = Range(config, "Interface", "UI Scale", MinimumUiScale, MinimumUiScale, MaximumUiScale, "Multiplier on top of automatic resolution scaling.");
            SnapPanels = config.Bind("Interface", "Snap Panels", true, "Stack panels into columns from the top of the screen. When off, panels stay where they are dropped.");
            MenuKey = config.Bind("Interface", "Menu Key", Key.Escape, "Key that shows and hides the panels.");
            AutoSave = config.Bind("Interface", "Autosave", true, "Save setting changes automatically. When off, save with the Presets panel's Save Settings button or by saving a preset.");

            try { Write(config); }
            catch (Exception e) { Log.Exception(e, "Saving the config failed"); }

            config.SettingChanged += (_, _) =>
            {
                dirty = true;
                lastChange = Time.unscaledTime;
            };
        }

        private static ConfigEntry<float> Range(ConfigFile config, string section, string key, float value, float min, float max, string description = "") =>
            config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

        internal static ConfigEntry<string> BindPanelPosition(string panel)
        {
            if (file == null) throw new InvalidOperationException("CasterConfig.Bind must run before panels are created.");
            return file.Bind("Layout", $"{panel} Position", string.Empty, "Saved x,y at 1080p. Empty uses the default layout.");
        }

        internal static string? ConfigFilePath => file?.ConfigFilePath;

        internal static void Tick()
        {
            if (AutoSave.Value && dirty && Time.unscaledTime - lastChange >= SaveDelay) Save();
        }

        internal static void SaveOnExit()
        {
            if (AutoSave.Value) Save();
        }

        internal static void Save()
        {
            if (dirty) ForceSave();
        }

        internal static void ForceSave()
        {
            if (file == null) return;
            dirty = false;
            try { Write(file); }
            catch (Exception e) { Log.Exception(e, "Saving the config failed"); }
        }

        private static void Write(ConfigFile config)
        {
            config.Save();
            string path = config.ConfigFilePath;
            File.WriteAllText(path, ConfigFormat.StripComments(File.ReadAllText(path)), new UTF8Encoding(false));
        }

        internal static void Reload()
        {
            if (file == null) return;
            try { file.Reload(); }
            catch (Exception e) { Log.Exception(e, "Reloading the config failed"); }
        }

        internal static bool KeyPressed(ConfigEntry<Key> entry) => KeyPressed(entry.Value);

        internal static bool KeyPressed(Key key)
        {
            Keyboard? keyboard = Keyboard.current;
            if (key == Key.None || keyboard == null || KeyBinds.Capturing) return false;
            try { return keyboard[key].wasPressedThisFrame; }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        internal static bool KeyHeld(ConfigEntry<Key> entry) => KeyHeld(entry.Value);

        internal static bool KeyHeld(Key key)
        {
            Keyboard? keyboard = Keyboard.current;
            if (key == Key.None || keyboard == null || KeyBinds.Capturing) return false;
            try { return keyboard[key].isPressed; }
            catch (ArgumentOutOfRangeException) { return false; }
        }
    }
}
