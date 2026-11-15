// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GenneCaster.UI
{
    internal sealed class Panel : IDisposable
    {
        private const float Inset = 8f;
        internal const float HeaderHeight = 24f;
        private const float ArrowAreaWidth = 26f;
        private const float ArrowWidth = 8f;
        private const float ArrowHeight = 5f;
        private const float RowGap = 2f;
        internal const float ButtonHeight = 20f;
        internal const float LabelHeight = 16f;
        internal const float SliderHeight = 14f;
        internal const float ListHeight = 160f;
        private const int TitleFontSize = 12;
        private const int ControlFontSize = 11;
        private const int MinimumFontSize = 8;
        private const int BorderThickness = 2;
        private const int CornerRadius = 12;
        private const int ArrowTextureWidth = 32;
        private const int ArrowTextureHeight = 20;
        private const float HeightDamping = 18f;
        private const float HeightSnap = 0.5f;
        private const float DragThreshold = 4f;
        private const float StalePressTimeout = 0.5f;

        private static int nextWindowId = 0x47430000;

        private readonly int windowId = NextWindowId();
        private readonly GUIContent measured = new GUIContent();
        private readonly float[] sliceX = new float[4];
        private readonly float[] sliceY = new float[4];
        private readonly float[] sliceU = new float[4];
        private readonly float[] sliceV = new float[4];
        private Rect rect;
        private float contentHeight = HeaderHeight;
        private float shownHeight = HeaderHeight;
        private bool pressed;
        private bool dragging;
        private Vector2 dragged;
        private float releasedFor;
        private float builtScale;
        private int cornerPixels;
        private Action<Panel>? content;
        private bool disposed;

        private Color32 accent;
        private static readonly Color32 BackgroundColor = new Color32(10, 10, 10, 160);
        private static readonly Color32 ButtonColor = new Color32(20, 5, 8, 190);
        private static readonly Color32 FieldColor = new Color32(20, 20, 20, 230);
        private Texture2D? background, button, field, arrowDown, arrowUp;
        private GUIStyle? buttonStyle, fieldStyle, titleStyle, labelStyle;

        internal string Title { get; }
        internal bool Expanded { get; set; }
        internal bool IsDragging => dragging;
        internal Rect Rect => rect;
        private float ContentWidth => rect.width - Inset * 2f;
        private bool ShowsContent => Expanded || shownHeight > HeaderHeight;
        private float Openness => contentHeight > HeaderHeight ? Mathf.Clamp01((shownHeight - HeaderHeight) / (contentHeight - HeaderHeight)) : 0f;

        internal Panel(float width, string title, Color32 accentColor)
        {
            Title = title;
            accent = accentColor;
            rect = new Rect(0f, 0f, width, shownHeight);
        }

        internal static int NextWindowId() => System.Threading.Interlocked.Increment(ref nextWindowId);

        internal void SetPosition(Vector2 position) => rect.position = position;

        internal void ClampToScreen() => rect = UiScale.ClampToScreen(rect);

        internal void SetAccent(Color32 color)
        {
            if (accent.Equals(color)) return;
            accent = color;
            DestroyTextures();
        }

        internal void Animate(float dt)
        {
            DropStalePress(dt);
            shownHeight = EaseHeight(shownHeight, Expanded ? contentHeight : HeaderHeight, dt);
            rect.height = shownHeight;
        }

        private static float EaseHeight(float current, float target, float dt)
        {
            float next = Mathf.Lerp(current, target, 1f - Mathf.Exp(-HeightDamping * dt));
            return Mathf.Abs(next - target) < HeightSnap ? target : next;
        }

        internal void Draw(Action<Panel> drawContent)
        {
            if (disposed) return;
            content = drawContent;
            EnsureTextures();
            Vector2 before = rect.position;
            rect.position = GUI.Window(windowId, rect, DrawWindow, GUIContent.none, GUIStyle.none).position;
            TrackPress(rect.position - before);
        }

        private void DrawWindow(int id)
        {
            EnsureStyles();
            bool showsContent = ShowsContent;
            DrawNineSlice(background!, new Rect(0f, 0f, rect.width, rect.height));
            float arrowX = rect.width - ArrowAreaWidth;
            GUI.Label(new Rect(Inset + 3f, 0f, arrowX - Inset - 3f, HeaderHeight), Title, titleStyle);
            var arrowArea = new Rect(arrowX, 0f, ArrowAreaWidth, HeaderHeight);
            if (GUI.Button(arrowArea, GUIContent.none, GUIStyle.none)) Expanded = !Expanded;
            DrawArrow(arrowArea);
            if (showsContent) DrawContent();

            var grip = new Rect(0f, 0f, arrowX, HeaderHeight);
            Event current = Event.current;
            if (GUI.enabled && current.type == EventType.MouseDown && current.button == 0 && grip.Contains(current.mousePosition)) BeginPress();
            GUI.DragWindow(grip);
        }

        private void DrawArrow(Rect area)
        {
            if (Event.current.type != EventType.Repaint) return;
            float flip = 1f - 2f * Openness;
            float height = ArrowHeight * Mathf.Abs(flip);
            if (height < 0.5f) return;
            Texture2D arrow = flip > 0f ? arrowDown! : arrowUp!;
            GUI.DrawTexture(new Rect(area.center.x - ArrowWidth * 0.5f, area.center.y - height * 0.5f, ArrowWidth, height), arrow);
        }

        private void DrawContent()
        {
            float top = HeaderHeight + RowGap;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * Openness);
            GUI.enabled = previousEnabled && Expanded;
            GUILayout.BeginArea(new Rect(Inset, top, ContentWidth, Mathf.Max(1f, rect.height - top - Inset)));
            GUILayout.BeginVertical();
            try { content?.Invoke(this); }
            catch (Exception e) when (!(e is ExitGUIException)) { Log.ExceptionOnce($"panel-{Title}", e, $"Drawing the {Title} panel failed"); }
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint) contentHeight = Mathf.Ceil(top + GUILayoutUtility.GetLastRect().height + Inset);
            GUILayout.EndArea();
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
        }

        private void BeginPress()
        {
            pressed = true;
            dragged = Vector2.zero;
            releasedFor = 0f;
        }

        private void TrackPress(Vector2 moved)
        {
            if (!pressed) return;
            if (moved != Vector2.zero)
            {
                dragging = true;
                dragged += moved;
            }
            Event current = Event.current;
            if (current.rawType == EventType.MouseUp && current.button == 0) EndPress(true);
        }

        private void EndPress(bool togglesOnClick)
        {
            pressed = false;
            dragging = false;
            if (dragged.magnitude >= DragThreshold) return;
            rect.position -= dragged;
            if (togglesOnClick) Expanded = !Expanded;
        }

        private void DropStalePress(float dt)
        {
            if (!pressed) return;
            Mouse? mouse = Mouse.current;
            if (mouse == null || mouse.leftButton.isPressed)
            {
                releasedFor = 0f;
                return;
            }
            releasedFor += dt;
            if (releasedFor >= StalePressTimeout) EndPress(false);
        }

        private void DrawNineSlice(Texture2D texture, Rect area)
        {
            float corner = Mathf.Min(cornerPixels / builtScale, area.width * 0.5f, area.height * 0.5f);
            float uv = corner * builtScale / texture.width;
            SetSlices(sliceX, area.x, area.x + corner, area.xMax - corner, area.xMax);
            SetSlices(sliceY, area.y, area.y + corner, area.yMax - corner, area.yMax);
            SetSlices(sliceU, 0f, uv, 1f - uv, 1f);
            SetSlices(sliceV, 1f, 1f - uv, uv, 0f);
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    var target = new Rect(sliceX[column], sliceY[row], sliceX[column + 1] - sliceX[column], sliceY[row + 1] - sliceY[row]);
                    var coordinates = new Rect(sliceU[column], sliceV[row + 1], sliceU[column + 1] - sliceU[column], sliceV[row] - sliceV[row + 1]);
                    GUI.DrawTextureWithTexCoords(target, texture, coordinates);
                }
            }
        }

        private static void SetSlices(float[] slices, float a, float b, float c, float d)
        {
            slices[0] = a;
            slices[1] = b;
            slices[2] = c;
            slices[3] = d;
        }

        private Rect NextRect(float height)
        {
            Rect row = GUILayoutUtility.GetRect(0f, 10000f, height + RowGap, height + RowGap, GUILayout.ExpandWidth(true));
            return new Rect(row.x + 1f, row.y + RowGap * 0.5f, Mathf.Max(1f, row.width - 2f), height);
        }

        internal void Button(string text, Action? onClick, bool interactable = true) => DrawButton(text, onClick, interactable, buttonStyle!);

        private void DrawButton(string text, Action? onClick, bool interactable, GUIStyle style)
        {
            Rect area = NextRect(ButtonHeight);
            style.fontSize = FittingFontSize(text, area.width - 6f, style);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && interactable;
            if (GUI.Button(area, text, style)) onClick?.Invoke();
            GUI.enabled = wasEnabled;
        }

        internal bool Toggle(string label, bool value, string on = "On", string off = "Off")
        {
            bool result = value;
            Button($"{label}: {(value ? on : off)}", () => result = !value);
            return result;
        }

        internal void Label(string text)
        {
            measured.text = text;
            float wrappedHeight = Mathf.Ceil(labelStyle!.CalcHeight(measured, ContentWidth - 2f));
            GUI.Label(NextRect(Mathf.Max(LabelHeight, wrappedHeight)), text, labelStyle);
        }

        internal float Slider(string label, float value, float min, float max, string format = "F2", string unit = "")
        {
            Label($"{label}: {value.ToString(format)}{unit}");
            Rect area = NextRect(SliderHeight);
            return GUI.HorizontalSlider(new Rect(area.x + 2f, area.y + 2f, area.width - 4f, area.height - 4f), value, min, max);
        }

        internal string TextField(string text, int maxLength, Func<string, string>? normalize = null)
        {
            fieldStyle!.fontSize = ControlFontSize;
            string value = GUI.TextField(NextRect(ButtonHeight), text ?? string.Empty, maxLength, fieldStyle);
            return normalize != null ? normalize(value) : value;
        }

        internal void ColorSwatch(Color color)
        {
            Rect area = NextRect(ButtonHeight);
            Color previous = GUI.color;
            GUI.color = accent;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(color.r, color.g, color.b, previous.a);
            GUI.DrawTexture(new Rect(area.x + 1f, area.y + 1f, area.width - 2f, area.height - 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        internal void HorizontalArea(Action draw)
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            try { draw(); }
            finally { GUILayout.EndHorizontal(); }
        }

        internal Vector2 ScrollArea(Vector2 scroll, float maximumHeight, int buttonRows, Action draw)
        {
            float height = Mathf.Min(maximumHeight, buttonRows * (ButtonHeight + RowGap));
            scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUIStyle.none, GUILayout.Height(height));
            try { draw(); }
            finally { GUILayout.EndScrollView(); }
            return scroll;
        }

        private int FittingFontSize(string text, float availableWidth, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return ControlFontSize;
            measured.text = text;
            for (int size = ControlFontSize; size > MinimumFontSize; size--)
            {
                style.fontSize = size;
                if (style.CalcSize(measured).x <= availableWidth) return size;
            }
            return MinimumFontSize;
        }

        private void EnsureTextures()
        {
            float scale = UiScale.Factor;
            if (!Mathf.Approximately(scale, builtScale)) DestroyBackground();
            builtScale = scale;
            int Px(float units) => Mathf.Max(1, Mathf.RoundToInt(units * scale));
            if (background == null)
            {
                cornerPixels = Px(CornerRadius);
                int size = cornerPixels * 2 + 2;
                background = Textures.CreateTexture(size, size, BackgroundColor, accent, Px(BorderThickness), cornerPixels);
            }
            if (button != null) return;
            button = Textures.CreateTexture(64, (int)ButtonHeight, ButtonColor, accent, 1, 4);
            field = Textures.CreateTexture(64, (int)ButtonHeight, FieldColor, accent, 1, 4);
            arrowDown = Textures.CreateTriangle(ArrowTextureWidth, ArrowTextureHeight, Color.white, true);
            arrowUp = Textures.CreateTriangle(ArrowTextureWidth, ArrowTextureHeight, Color.white, false);
            if (buttonStyle != null) ApplyTextures();
        }

        private void EnsureStyles()
        {
            if (buttonStyle != null) return;
            var none = new RectOffset(0, 0, 0, 0);
            const int border = 5;
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(3, 3, 1, 1), margin = none, border = new RectOffset(border, border, border, border) };
            fieldStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = new RectOffset(4, 4, 1, 1), margin = none, border = new RectOffset(border, border, border, border) };
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = TitleFontSize, fontStyle = FontStyle.Bold, padding = none, margin = none, clipping = TextClipping.Clip };
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = ControlFontSize, padding = new RectOffset(2, 2, 0, 0), margin = none, wordWrap = true, clipping = TextClipping.Clip };
            foreach (GUIStyle style in new[] { buttonStyle, fieldStyle, titleStyle, labelStyle }) SetTextColor(style, Color.white);
            ApplyTextures();
        }

        private void ApplyTextures()
        {
            SetBackground(buttonStyle!, button);
            SetBackground(fieldStyle!, field);
        }

        private static void SetBackground(GUIStyle style, Texture2D? texture)
        {
            style.normal.background = style.focused.background = style.onNormal.background = style.onFocused.background = texture;
            style.hover.background = style.active.background = style.onHover.background = style.onActive.background = texture;
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = color;
        }

        private void DestroyBackground()
        {
            if (background != null) UnityEngine.Object.Destroy(background);
            background = null;
        }

        private void DestroyTextures()
        {
            DestroyBackground();
            foreach (Texture2D? texture in new[] { button, field, arrowDown, arrowUp })
                if (texture != null) UnityEngine.Object.Destroy(texture);
            button = field = arrowDown = arrowUp = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Expanded = false;
            DestroyTextures();
            content = null;
        }
    }
}
