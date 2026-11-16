// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.GameCamera;
using Zetori.GameCamera.Modes;
using Zetori.GameCamera.Webs;
using UnityEngine;

namespace Zetori.UI
{
    internal sealed class WebMarkers : ObjectComponent
    {
        private const float MarkerSize = 10f;
        private const float LabelGap = 3f;
        private const float LabelWidth = 40f;
        private const float LabelHeight = 16f;
        private const int LabelFontSize = 12;

        private GUIStyle? labelStyle;

        internal void OnGUI()
        {
            if (!WebCamera.Editing || Event.current.type != EventType.Repaint) return;
            Camera? view = CameraManager.camera?.Camera;
            if (view == null) return;
            try { Draw(view); }
            catch (Exception e) { Log.ExceptionOnce("web-markers", e, "Drawing the web markers failed"); }
        }

        private void Draw(Camera view)
        {
            IReadOnlyList<Vector3> nodes = CameraWeb.Nodes;
            int nearest = CameraWeb.NearestIndex(view.transform.position);
            Color accent = ColorSpace.Current.Color;
            GUIStyle style = labelStyle ??= CreateLabelStyle();
            float factor = UiScale.Factor;
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = UiScale.Begin();
            try
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    Vector3 screen = view.WorldToScreenPoint(nodes[i]);
                    if (screen.z <= 0f) continue;
                    var center = new Vector2(screen.x / factor, (Screen.height - screen.y) / factor);
                    GUI.color = i == nearest ? Color.white : accent;
                    GUI.DrawTexture(new Rect(center.x - MarkerSize * 0.5f, center.y - MarkerSize * 0.5f, MarkerSize, MarkerSize), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(center.x + MarkerSize * 0.5f + LabelGap, center.y - LabelHeight * 0.5f, LabelWidth, LabelHeight), (i + 1).ToString(), style);
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }

        private static GUIStyle CreateLabelStyle()
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = LabelFontSize, fontStyle = FontStyle.Bold, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(0, 0, 0, 0) };
            style.normal.textColor = Color.white;
            return style;
        }
    }
}
