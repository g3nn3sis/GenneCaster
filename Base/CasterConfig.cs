using BepInEx.Configuration;
using GenneCaster.GameCamera;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.Base
{
    internal static class CasterConfig
    {
        private const float SaveDelay = 1f;

        private static ConfigFile? file;
        private static bool dirty;
        private static float lastChange;

        internal static ConfigEntry<CameraMode> ActiveCameraMode = null!;
        internal static ConfigEntry<float> FieldOfView = null!;
        internal static ConfigEntry<float> NearClipPlane = null!;
        internal static ConfigEntry<bool> AudioListener = null!;
        internal static ConfigEntry<float> HeadDistance = null!;
        internal static ConfigEntry<float> PositionSmoothing = null!;
        internal static ConfigEntry<float> RotationSmoothing = null!;
        internal static ConfigEntry<float> OrbitSpeed = null!;
        internal static ConfigEntry<bool> OrbitAutoRotate = null!;
        internal static ConfigEntry<float> BodyLerping = null!;
        internal static ConfigEntry<bool> RegisterNewRigs = null!;
        internal static ConfigEntry<bool> Microphone = null!;
        internal static ConfigEntry<Key> PushToTalkKey = null!;
        internal static ConfigEntry<Key> NextPlayerKey = null!;
        internal static ConfigEntry<Key> PreviousPlayerKey = null!;

        internal static ConfigEntry<bool> Wasd = null!;
        internal static ConfigEntry<bool> MouseClick = null!;
        internal static ConfigEntry<float> MovementSpeed = null!;
        internal static ConfigEntry<float> MouseSpeed = null!;

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
        internal static ConfigEntry<bool> DiscordPresence = null!;

        internal static ConfigEntry<int> HudFont = null!;

        internal static ConfigEntry<bool> LeaderboardEnabled = null!;
        internal static ConfigEntry<float> LeaderboardScale = null!;
        internal static ConfigEntry<bool> LeaderboardPlatformIcons = null!;
        internal static ConfigEntry<bool> LeaderboardFps = null!;
        internal static ConfigEntry<float> LeaderboardOffsetX = null!;
        internal static ConfigEntry<float> LeaderboardOffsetY = null!;

        internal static ConfigEntry<bool> ClosestLavaEnabled = null!;
        internal static ConfigEntry<float> ClosestLavaScale = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetX = null!;
        internal static ConfigEntry<float> ClosestLavaOffsetY = null!;

        internal static ConfigEntry<bool> ScoreboardEnabled = null!;
        internal static ConfigEntry<float> ScoreboardScale = null!;
        internal static ConfigEntry<float> ScoreboardOffsetX = null!;
        internal static ConfigEntry<float> ScoreboardOffsetY = null!;
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

        internal static ConfigEntry<string> PlayerName = null!;
        internal static ConfigEntry<float> PlayerRed = null!;
        internal static ConfigEntry<float> PlayerGreen = null!;
        internal static ConfigEntry<float> PlayerBlue = null!;

        internal static ConfigEntry<bool> DisableVSync = null!;
        internal static ConfigEntry<bool> DisableAfkKick = null!;

        internal static ConfigEntry<string> Theme = null!;
        internal static ConfigEntry<float> UiScale = null!;
        internal static ConfigEntry<Key> MenuKey = null!;
        internal static ConfigEntry<bool> AutoSave = null!;

        internal static void Bind(ConfigFile config)
        {
            file = config;
            config.SaveOnConfigSet = false;

            ActiveCameraMode = config.Bind("Camera", "Mode", CameraMode.Spectator, "Default shows your own first person view, Spectator follows the selected player, Freecam flies the camera on its own.");
            FieldOfView = Range(config, "Camera", "Field Of View", 100f, 10f, 130f);
            NearClipPlane = Range(config, "Camera", "Near Clip", 0.01f, 0.01f, 0.99f);
            AudioListener = config.Bind("Camera", "Audio Listener", true, "Hear the game from the spectator camera.");
            HeadDistance = Range(config, "Camera", "Head Distance", 0f, 0f, 0.99f, "0 is first person, anything higher orbits the player.");
            PositionSmoothing = Range(config, "Camera", "Position Smoothing", 0.05f, 0f, 0.95f, "How much the orbit camera lags behind the player. 0 follows instantly.");
            RotationSmoothing = Range(config, "Camera", "Rotation Smoothing", 0f, 0f, 0.75f, "How much the orbit camera's rotation lags behind. 0 is the lightest smoothing.");
            OrbitSpeed = Range(config, "Camera", "Orbit Speed", 150f, 10f, 600f, "Arrow key orbit speed in degrees per second.");
            OrbitAutoRotate = config.Bind("Camera", "Auto Orbit", false, "Slowly rotate the orbit camera around the spectated player with no input. Arrow keys still steer on top.");
            BodyLerping = Range(config, "Camera", "Body Lerping", 1f, 0.1f, 2.5f, "Multiplier on how quickly other players' rigs move to their networked position.");
            RegisterNewRigs = config.Bind("Camera", "Register New Rigs", true, "Keep the player list updated as players join and leave.");
            Microphone = config.Bind("Camera", "Microphone", false, "On is all chat, off is push to talk.");
            PushToTalkKey = config.Bind("Camera", "Hold To Talk Key", Key.V, "Hold to open the mic to all chat; release returns to the Microphone setting (push to talk by default).");
            NextPlayerKey = config.Bind("Camera", "Next Player Key", Key.RightBracket, "Spectate the next player in the list.");
            PreviousPlayerKey = config.Bind("Camera", "Previous Player Key", Key.LeftBracket, "Spectate the previous player in the list.");

            Wasd = config.Bind("Freecam", "Enabled", false, "Hold right mouse to fly your own player with WASD. Turned off while the camera is in Freecam mode.");
            MouseClick = config.Bind("Freecam", "Mouse Click", false, "Left click presses whatever is under the cursor with the right hand.");
            MovementSpeed = Range(config, "Freecam", "Movement Speed", 10f, 5f, 35f);
            MouseSpeed = Range(config, "Freecam", "Mouse Sensitivity", 0.3f, 0.05f, 2f);

            NameTags = config.Bind("Nametags", "Enabled", false);
            NameTagPlayerColor = config.Bind("Nametags", "Player Color", false);
            NameTagFps = config.Bind("Nametags", "Player FPS", false);
            NameTagPlatform = config.Bind("Nametags", "Platform Indicator", false);
            NameTagFont = config.Bind("Nametags", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game.", new AcceptableValueRange<int>(0, 255)));

            MiniMapEnabled = config.Bind("MiniMap", "Enabled", false);
            MiniMapDraggable = config.Bind("MiniMap", "Draggable", false);
            MiniMapSize = Range(config, "MiniMap", "Size", 520f, 100f, 520f, "Width and height at 1080p, scaled with the interface.");
            MiniMapHeight = Range(config, "MiniMap", "Camera Height", 8f, 1f, 50f);
            MiniMapOffsetX = Range(config, "MiniMap", "Offset X", 10f, 0f, 8000f, "Distance from the right edge of the screen at 1080p.");
            MiniMapOffsetY = Range(config, "MiniMap", "Offset Y", 10f, 0f, 8000f, "Distance from the top edge of the screen at 1080p.");

            DiscordPresence = config.Bind("Discord", "Rich Presence", true, "Show what you are doing on your Discord profile.");

            HudFont = config.Bind("HUD", "Font Index", 0, new ConfigDescription("Index into the fonts found in the game, shared by the leaderboard and closest-lava overlays.", new AcceptableValueRange<int>(0, 255)));

            LeaderboardEnabled = config.Bind("Leaderboard", "Enabled", false, "Show the bottom-left player leaderboard.");
            LeaderboardScale = Range(config, "Leaderboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            LeaderboardPlatformIcons = config.Bind("Leaderboard", "Platform Icons", true, "Show each player's Steam or Meta icon.");
            LeaderboardFps = config.Bind("Leaderboard", "Show FPS", false, "Show each player's frame rate next to their name.");
            LeaderboardOffsetX = Range(config, "Leaderboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            LeaderboardOffsetY = Range(config, "Leaderboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");

            ClosestLavaEnabled = config.Bind("ClosestLava", "Enabled", false, "Show the bottom-center distance to the nearest tagged (lava) player from the spectated player.");
            ClosestLavaScale = Range(config, "ClosestLava", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ClosestLavaOffsetX = Range(config, "ClosestLava", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ClosestLavaOffsetY = Range(config, "ClosestLava", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");

            ScoreboardEnabled = config.Bind("Scoreboard", "Enabled", false, "Show the top-center two-team scoreboard with a timer.");
            ScoreboardScale = Range(config, "Scoreboard", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            ScoreboardOffsetX = Range(config, "Scoreboard", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            ScoreboardOffsetY = Range(config, "Scoreboard", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");
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

            KillFeedEnabled = config.Bind("KillFeed", "Enabled", false, "Show the top-right feed of players as they get tagged.");
            KillFeedScale = Range(config, "KillFeed", "Scale", 1f, 0.5f, 2f, "Size multiplier on top of automatic resolution scaling.");
            KillFeedOffsetX = Range(config, "KillFeed", "Offset X", 0f, -800f, 800f, "Shift left (negative) or right (positive), in 1080p pixels.");
            KillFeedOffsetY = Range(config, "KillFeed", "Offset Y", 0f, -500f, 500f, "Move down (negative) or up (positive), in 1080p pixels.");

            PlayerName = config.Bind("Player", "Name", "GENNECASTER", "Up to 12 letters or digits.");
            PlayerRed = Range(config, "Player", "Red", 0f, 0f, 1f);
            PlayerGreen = Range(config, "Player", "Green", 0f, 0f, 1f);
            PlayerBlue = Range(config, "Player", "Blue", 0f, 0f, 1f);

            DisableVSync = config.Bind("Game", "Disable VSync", true, "Turn off VSync so the spectator view runs at the highest frame rate.");
            DisableAfkKick = config.Bind("Game", "Disable AFK Kick", true, "Stop the game from kicking you for being idle while spectating.");

            Theme = config.Bind("Interface", "Theme Name", "Red", "Name of a theme from the Theme Loader list.");
            UiScale = Range(config, "Interface", "UI Scale", 0.75f, 0.5f, 2f, "Multiplier on top of automatic resolution scaling.");
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
            if (!dirty || file == null) return;
            dirty = false;
            try { Write(file); }
            catch (Exception e) { Log.Exception(e, "Saving the config failed"); }
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
            if (key == Key.None || keyboard == null) return false;
            try { return keyboard[key].wasPressedThisFrame; }
            catch (ArgumentOutOfRangeException) { return false; }
        }

        internal static bool KeyHeld(ConfigEntry<Key> entry) => KeyHeld(entry.Value);

        internal static bool KeyHeld(Key key)
        {
            Keyboard? keyboard = Keyboard.current;
            if (key == Key.None || keyboard == null) return false;
            try { return keyboard[key].isPressed; }
            catch (ArgumentOutOfRangeException) { return false; }
        }
    }

    internal sealed class CasterConfigSaver : ObjectComponent
    {
        internal void Update() => CasterConfig.Tick();

        protected override void OnApplicationQuit()
        {
            CasterConfig.SaveOnExit();
            base.OnApplicationQuit();
        }
    }
}
