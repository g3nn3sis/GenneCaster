using System;
using GenneCaster.Classes.Game;
using GenneCaster.Classes.GameCamera;
using GenneCaster.Classes.GameCamera.Modes;
using GenneCaster.Classes.Player;
using GorillaNetworking;
using Photon.Pun;
using UnityEngine;

namespace GenneCaster.Classes.UI
{
    internal sealed class PanelContent
    {
        private static readonly GUILayoutOption[] CompactLabel = { GUILayout.Height(20f) };
        private static readonly GUILayoutOption[] CompactSlider = { GUILayout.Height(18f) };
        private static readonly GUILayoutOption[] CompactToggle = { GUILayout.Height(22f) };

        private readonly PanelOrchestrator owner;
        private readonly GTZone[] zones = (GTZone[])Enum.GetValues(typeof(GTZone));
        private readonly Action[] mapActions;
        private readonly Action[] themeActions;
        private Vector2 mapScroll;
        private Vector2 themeScroll;
        private string roomToJoin = string.Empty;
        private string nameToSet = "GenneCaster";

        internal readonly Action<Panel>[] DrawContent;

        internal PanelContent(PanelOrchestrator owner)
        {
            this.owner = owner;
            DrawContent = new Action<Panel>[] { DrawMain, DrawUniversal, DrawFreecam, DrawCamera, DrawPlayerSettings, DrawTime, DrawMapLoader, DrawPlayer, DrawNametag, DrawTheme, DrawMiniMap };
            mapActions = new Action[zones.Length];
            for (int i = 0; i < zones.Length; i++)
            {
                GTZone zone = zones[i];
                mapActions[i] = () => ZoneManagement.SetActiveZone(zone);
            }
            themeActions = new Action[ColorSpace.Themes.Length];
            for (int i = 0; i < themeActions.Length; i++)
            {
                int index = i;
                themeActions[i] = () => owner.SetTheme(index);
            }
        }

        private static void Label(string text) => GUILayout.Label(text, CompactLabel);
        private static float Slider(float value, float min, float max) => GUILayout.HorizontalSlider(value, min, max, CompactSlider);
        private static bool Toggle(bool value, string label) => GUILayout.Toggle(value, label, CompactToggle);

        private void DrawMain(Panel panel)
        {
            panel.CreateButton($"Panels | {(owner.IsVisible(PanelId.Universal) ? "On" : "Off")}", () => owner.Toggle(PanelId.Universal));
            panel.CreateButton($"Freecam | {(owner.IsVisible(PanelId.Freecam) ? "On" : "Off")}", () => owner.Toggle(PanelId.Freecam));
            roomToJoin = panel.CreateTextFieldUpper(roomToJoin);
            panel.CreateButton(PhotonNetwork.InRoom ? "Leave" : "Join", () =>
            {
                if (PhotonNetwork.InRoom) PhotonNetwork.Disconnect();
                else LocalPlayerManager.AttemptRoomJoin(roomToJoin, JoinType.Solo, null);
            });
            panel.CreateButton("Join Public", () => LocalPlayerManager.AttemptPublicRoomJoin(JoinType.Solo, true));
        }

        private void DrawUniversal(Panel panel)
        {
            panel.CreateButton("Camera Manager", () => owner.Toggle(PanelId.Camera));
            panel.CreateButton("Player Settings", () => owner.Toggle(PanelId.PlayerSettings));
            panel.CreateButton("Time Manager", () => owner.Toggle(PanelId.Time));
            panel.CreateButton("Map Loader", () => owner.Toggle(PanelId.MapLoader));
            panel.CreateButton("Player Manager", () => owner.Toggle(PanelId.Player));
            panel.CreateButton("Nametag", () => owner.Toggle(PanelId.Nametag));
            panel.CreateButton("Theme Manager", () => owner.Toggle(PanelId.Theme));
            panel.CreateButton("MiniMap", () => owner.Toggle(PanelId.MiniMap));
        }

        private void DrawFreecam(Panel panel)
        {
            panel.CreateButton($"FreeCam: {(GorillaController.freeCam ? "ON" : "OFF")}", () => GorillaController.freeCam = !GorillaController.freeCam);
            panel.CreateButton($"Mouse Click: {(GorillaController.mouseClick ? "ON" : "OFF")}", () => GorillaController.mouseClick = !GorillaController.mouseClick);
            Label($"Movement Speed: {GorillaController._movementSpeed:F1}");
            GorillaController._movementSpeed = Slider(GorillaController._movementSpeed, 5f, 35f);
        }

        private void DrawCamera(Panel panel)
        {
            Label("Field Of View");
            CameraManager.fieldOfView = Slider(CameraManager.fieldOfView, 10f, 130f);
            Label("Near Clip");
            CameraManager.nearClipPlane = Slider(CameraManager.nearClipPlane, 0.01f, 0.99f);
        }

        private void DrawPlayerSettings(Panel panel)
        {
            LocalPlayerManager.microphoneStatus = Toggle(LocalPlayerManager.microphoneStatus, $"Microphone : {(LocalPlayerManager.microphoneStatus ? "On" : "Off")}");
            CameraManager.audioListener = Toggle(CameraManager.audioListener, "AudioListener");
            NetworkedPlayerManager.registerNewRigs = Toggle(NetworkedPlayerManager.registerNewRigs, "Register New Rigs");
            Label("Body Smoothing");
            _DirectPlayerCasting.bodySmoothing = Slider(_DirectPlayerCasting.bodySmoothing, 0.95f, 0f);
            Label("Rotation Smoothing");
            _DirectPlayerCasting.rotationSmoothing = Slider(_DirectPlayerCasting.rotationSmoothing, 0f, 0.75f);
            Label("Body Lerping");
            NetworkedPlayerManager.bodyLerping = Slider(NetworkedPlayerManager.bodyLerping, 2.5f, 0.1f);
            Label("Head Distance");
            _DirectPlayerCasting.headDistance = Slider(_DirectPlayerCasting.headDistance, 0f, 0.99f);
            panel.CreateButton("Reset Offset", () => { _DirectPlayerCasting.cameraXRotation = 0f; _DirectPlayerCasting.cameraYRotation = 0f; });
        }

        private void DrawTime(Panel panel)
        {
            panel.CreateButton("Set Day", TimeManager.SetDay);
            panel.CreateButton("Set Night", TimeManager.SetNight);
            panel.CreateButton("Set Morning", TimeManager.SetMorning);
            panel.CreateButton("Set Evening", TimeManager.SetEvening);
            panel.CreateButton("Start Rain", TimeManager.StartRain);
            panel.CreateButton("Stop Rain", TimeManager.StopRain);
        }

        private void DrawMapLoader(Panel panel)
        {
            mapScroll = GUILayout.BeginScrollView(mapScroll, false, false, GUIStyle.none, GUIStyle.none);
            try
            {
                for (int i = 0; i < zones.Length; i++)
                    panel.CreateButton(zones[i].ToString(), mapActions[i]);
            }
            finally { GUILayout.EndScrollView(); }
        }

        private void DrawPlayer(Panel panel)
        {
            nameToSet = panel.CreateTextFieldLower(nameToSet);
            panel.CreateButton("Set Name", () =>
            {
                try { LocalPlayerManager.ChangeName(nameToSet); }
                catch (Exception ex) { Debug.LogException(ex); }
            });
            float red = LocalPlayerManager._localPlayerRed;
            float green = LocalPlayerManager._localPlayerGreen;
            float blue = LocalPlayerManager._localPlayerBlue;
            Label("Red");
            LocalPlayerManager._localPlayerRed = Slider(red, 0f, 1f);
            Label("Green");
            LocalPlayerManager._localPlayerGreen = Slider(green, 0f, 1f);
            Label("Blue");
            LocalPlayerManager._localPlayerBlue = Slider(blue, 0f, 1f);
            if (Event.current.type == EventType.Repaint ||
                !Mathf.Approximately(red, LocalPlayerManager._localPlayerRed) ||
                !Mathf.Approximately(green, LocalPlayerManager._localPlayerGreen) ||
                !Mathf.Approximately(blue, LocalPlayerManager._localPlayerBlue))
                LocalPlayerManager.ChangeColor();
        }

        private void DrawNametag(Panel panel)
        {
            panel.HorizontalArea(() =>
            {
                panel.CreateButton("<", () => ShiftFont(-1));
                panel.CreateButton(">", () => ShiftFont(1));
            });
            NameTag.enabled = Toggle(NameTag.enabled, "NameTags");
            if (!NameTag.enabled) return;
            NameTag.usePlayerColor = Toggle(NameTag.usePlayerColor, "Player Color");
            NameTag.hzDisplay = Toggle(NameTag.hzDisplay, "Player FPS");
            NameTag.platformIndicator = Toggle(NameTag.platformIndicator, "Platform Indicator");
        }

        private static void ShiftFont(int amount)
        {
            int count = AssetManager.discoveredFonts?.Length ?? 0;
            if (count == 0) return;
            NameTag.currentFontIndex = (NameTag.currentFontIndex + count + amount) % count;
        }

        private void DrawTheme(Panel panel)
        {
            themeScroll = GUILayout.BeginScrollView(themeScroll, false, false, GUIStyle.none, GUIStyle.none);
            try
            {
                for (int i = 0; i < themeActions.Length; i++)
                    panel.CreateButton(ColorSpace.Themes[i].Name, themeActions[i]);
            }
            finally { GUILayout.EndScrollView(); }
        }

        private void DrawMiniMap(Panel panel)
        {
            panel.CreateButton($"Enabled: {MiniMap.enabled}", () => MiniMap.enabled = !MiniMap.enabled);
            panel.CreateButton($"Draggable: {MiniMap.draggable}", () => MiniMap.draggable = !MiniMap.draggable);
            Label($"MiniMap Size: {MiniMap.miniMapSize:F0}");
            MiniMap.miniMapSize = Slider(MiniMap.miniMapSize, 100f, 520f);
            Label($"Camera Height: {MiniMap.cameraHeight:F0}");
            MiniMap.cameraHeight = Slider(MiniMap.cameraHeight, 1f, 50f);
        }
    }
}
