// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.Game;
using Zerori.GameCamera;
using Zerori.GameCamera.Modes;
using Zerori.Player;
using UnityEngine;

namespace Zerori.UI.Panels
{
    internal static class ViewPanels
    {
        internal static void DrawCamera(Panel panel)
        {
            panel.Label($"Mode: {CameraManager.mode}");
            panel.HorizontalArea(() =>
            {
                panel.Button("Default", () => CameraManager.mode = CameraMode.Default, CameraManager.mode != CameraMode.Default);
                panel.Button("Spectator", () => CameraManager.mode = CameraMode.Spectator, CameraManager.mode != CameraMode.Spectator);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Freecam", () => CameraManager.mode = CameraMode.Freecam, CameraManager.mode != CameraMode.Freecam);
                panel.Button("Web", () => CameraManager.mode = CameraMode.Web, CameraManager.mode != CameraMode.Web);
            });
            if (CameraManager.mode == CameraMode.Freecam || WebCamera.Editing) DrawFlightSpeeds(panel);
            DrawLens(panel);
            DrawFollow(panel);
            DrawOrbit(panel);
            DrawSmoothing(panel);
        }

        private static void DrawLens(Panel panel)
        {
            CameraManager.fieldOfView = panel.Slider("Field Of View", CameraManager.fieldOfView, 10f, 130f, "F0", "°");
            panel.HorizontalArea(() =>
            {
                panel.Button("FOV 60", () => CameraManager.fieldOfView = 60f);
                panel.Button("FOV 90", () => CameraManager.fieldOfView = 90f);
                panel.Button("FOV 110", () => CameraManager.fieldOfView = 110f);
            });
            CameraManager.nearClipPlane = panel.Slider("Near Clip", CameraManager.nearClipPlane, 0.01f, 0.99f);
        }

        private static void DrawFollow(Panel panel)
        {
            CasterConfig.HeadDistance.Value = panel.Slider("Head Distance", CasterConfig.HeadDistance.Value, 0f, 0.99f);
            panel.HorizontalArea(() =>
            {
                panel.Button("First Person", () => CasterConfig.HeadDistance.Value = 0f);
                panel.Button("Orbit", () => CasterConfig.HeadDistance.Value = Mathf.Max(0.5f, CasterConfig.HeadDistance.Value));
            });
        }

        private static void DrawOrbit(Panel panel)
        {
            CasterConfig.OrbitSpeed.Value = panel.Slider("Orbit Speed", CasterConfig.OrbitSpeed.Value, 10f, 600f, "F0", "°/s");
            CasterConfig.OrbitAutoRotate.Value = panel.Toggle("Auto Orbit", CasterConfig.OrbitAutoRotate.Value);
            panel.Button("Reset Orbit", SpectatorCamera.ResetOrbit);
        }

        private static void DrawSmoothing(Panel panel)
        {
            CasterConfig.PositionSmoothing.Value = panel.Slider("Position Smoothing", CasterConfig.PositionSmoothing.Value, 0f, 0.95f);
            CasterConfig.RotationSmoothing.Value = panel.Slider("Rotation Smoothing", CasterConfig.RotationSmoothing.Value, 0f, 0.75f);
        }

        internal static void DrawWasd(Panel panel)
        {
            if (CameraManager.mode == CameraMode.Freecam) panel.Label("WASD is off in Freecam mode");
            else GorillaController.wasd = panel.Toggle("WASD", GorillaController.wasd);
            GorillaController.mouseClick = panel.Toggle("Mouse Click", GorillaController.mouseClick);
            DrawFlightSpeeds(panel);
        }

        internal static void DrawMiniMap(Panel panel)
        {
            MiniMap.enabled = panel.Toggle("MiniMap", MiniMap.enabled);
            MiniMap.draggable = panel.Toggle("Draggable", MiniMap.draggable);
            PositionPicker.Draw(panel, HudSlot.MiniMap);
            MiniMap.miniMapSize = Mathf.Round(panel.Slider("Size", MiniMap.miniMapSize, 100f, 520f, "F0", " px"));
            MiniMap.cameraHeight = panel.Slider("Camera Height", MiniMap.cameraHeight, 1f, 50f, "F0", " m");
            panel.Button("Reset Position", MiniMap.ResetPosition);
        }

        private static void DrawFlightSpeeds(Panel panel)
        {
            GorillaController.MovementSpeed = panel.Slider("Movement Speed", GorillaController.MovementSpeed, 5f, 35f, "F1", " m/s");
            GorillaController.MouseSensitivity = panel.Slider("Mouse Sensitivity", GorillaController.MouseSensitivity, 0.05f, 2f);
        }
    }
}
