// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using GenneCaster.Player;
using GenneCaster.Replay;
using Photon.Pun;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class MiniMap : ObjectComponent
    {
        internal static new bool enabled { get => CasterConfig.MiniMapEnabled.Value; set => CasterConfig.MiniMapEnabled.Value = value; }
        internal static bool draggable { get => CasterConfig.MiniMapDraggable.Value; set => CasterConfig.MiniMapDraggable.Value = value; }
        internal static float miniMapSize { get => CasterConfig.MiniMapSize.Value; set => CasterConfig.MiniMapSize.Value = value; }
        internal static float cameraHeight { get => CasterConfig.MiniMapHeight.Value; set => CasterConfig.MiniMapHeight.Value = value; }

        private const float BorderWidth = 2f;
        private const int MinimumTextureSize = 64;
        private const int MaximumTextureSize = 2048;

        private readonly int windowId = UI.Panel.NextWindowId();
        private Camera? topViewCamera;
        private RenderTexture? renderTexture;

        internal static void ResetPosition()
        {
            CasterConfig.MiniMapOffsetX.Value = (float)CasterConfig.MiniMapOffsetX.DefaultValue;
            CasterConfig.MiniMapOffsetY.Value = (float)CasterConfig.MiniMapOffsetY.DefaultValue;
        }

        private static bool Active => enabled && PhotonNetwork.InRoom && NetworkedPlayerManager.selectedPlayer != null && !ReplaySession.IsReplaying;

        internal void Start()
        {
            try
            {
                var cameraObject = new GameObject("_GenneCaster MiniMap Camera");
                DontDestroyOnLoad(cameraObject);
                topViewCamera = cameraObject.AddComponent<Camera>();
                topViewCamera.clearFlags = CameraClearFlags.Color;
                topViewCamera.backgroundColor = Color.black;
                topViewCamera.cameraType = CameraType.Reflection;
                topViewCamera.enabled = false;
            }
            catch (Exception e) { Log.Exception(e, "Creating the minimap camera failed"); }
        }

        internal void LateUpdate()
        {
            if (topViewCamera == null) return;
            bool active = Active;
            if (topViewCamera.enabled != active) topViewCamera.enabled = active;
            if (!active) return;
            EnsureTexture(Mathf.RoundToInt(miniMapSize * UI.UiScale.Factor));
            Transform target = NetworkedPlayerManager.selectedPlayer!.transform;
            topViewCamera.transform.SetPositionAndRotation(target.position + Vector3.up * cameraHeight, Quaternion.Euler(90f, target.eulerAngles.y, 0f));
        }

        private void EnsureTexture(int size)
        {
            size = Mathf.Clamp(size, MinimumTextureSize, MaximumTextureSize);
            if (renderTexture != null && renderTexture.width == size) return;
            ReleaseTexture();
            renderTexture = new RenderTexture(size, size, 16) { name = "_GenneCaster MiniMap" };
            topViewCamera!.targetTexture = renderTexture;
        }

        private void ReleaseTexture()
        {
            if (renderTexture == null) return;
            if (topViewCamera != null) topViewCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        internal void OnGUI()
        {
            if (!Active || renderTexture == null) return;
            Matrix4x4 previous = UI.UiScale.Begin();
            try
            {
                float size = miniMapSize;
                var rect = new Rect(UI.UiScale.Width - size - CasterConfig.MiniMapOffsetX.Value, CasterConfig.MiniMapOffsetY.Value, size, size);
                rect = UI.UiScale.ClampToScreen(rect);
                Rect moved = UI.UiScale.ClampToScreen(GUI.Window(windowId, rect, DrawWindow, GUIContent.none, GUIStyle.none));
                if (moved.position != rect.position)
                {
                    CasterConfig.MiniMapOffsetX.Value = Mathf.Round(UI.UiScale.Width - moved.xMax);
                    CasterConfig.MiniMapOffsetY.Value = Mathf.Round(moved.y);
                }
            }
            finally { GUI.matrix = previous; }
        }

        private void DrawWindow(int id)
        {
            float size = miniMapSize;
            Color previous = GUI.color;
            GUI.color = UI.ColorSpace.Current.Color;
            GUI.DrawTexture(new Rect(0f, 0f, size, size), Texture2D.whiteTexture);
            GUI.color = previous;
            if (renderTexture != null)
                GUI.DrawTexture(new Rect(BorderWidth, BorderWidth, size - BorderWidth * 2f, size - BorderWidth * 2f), renderTexture, ScaleMode.ScaleAndCrop, false);
            if (draggable) GUI.DragWindow();
        }

        protected override void OnDestroy()
        {
            ReleaseTexture();
            if (topViewCamera != null) Destroy(topViewCamera.gameObject);
            base.OnDestroy();
        }
    }
}
