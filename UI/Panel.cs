// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.UI
{
    internal sealed class Panel : IDisposable
    {
        private const float Inset = 8f;
        private const float HeaderY = 4f;
        private const float HeaderHeight = 24f;
        private const float CloseSize = 15f;
        private const float RowGap = 2f;
        internal const float ButtonHeight = 20f;
        internal const float LabelHeight = 16f;
        internal const float SliderHeight = 14f;
        private const int TitleFontSize = 12;
        private const int ControlFontSize = 11;
        private const int MinimumFontSize = 8;
        private const int BorderThickness = 2;
        private const int CornerRadius = 12;

        private static int nextWindowId = 0x47430000;

        private readonly int windowId = NextWindowId();
        private readonly GUIContent measured = new GUIContent();
        private Rect rect;
        private float fittedHeight;
        private float builtScale;
        private Action<Panel>? content;
        private bool disposed;

        private Color32 accent;
        private static readonly Color32 BackgroundColor = new Color32(10, 10, 10, 160);
        private static readonly Color32 ButtonColor = new Color32(20, 5, 8, 190);
        private static readonly Color32 FieldColor = new Color32(20, 20, 20, 230);
        private Texture2D? background, button, field, close, closeHover;
        private GUIStyle? buttonStyle, fieldStyle, closeStyle, titleStyle, labelStyle;

        internal string Title { get; }
        internal bool Enabled { get; set; }
        internal Rect Rect => rect;
        private float ContentWidth => rect.width - Inset * 2f;

        internal event Action? Closed;
        internal event Action? Moved;

        internal Panel(float width, string title, Color32 accentColor)
        {
            Title = title;
            accent = accentColor;
            rect = new Rect(0f, 0f, width, HeaderY + HeaderHeight + Inset + ButtonHeight);
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

        internal void Draw(Action<Panel> drawContent)
        {
            if (disposed || !Enabled) return;
            content = drawContent;
            if (fittedHeight > 0f && !Mathf.Approximately(fittedHeight, rect.height))
            {
                rect.height = fittedHeight;
                DestroyBackground();
            }
            EnsureTextures();
            Vector2 before = rect.position;
            rect = GUI.Window(windowId, rect, DrawWindow, GUIContent.none, GUIStyle.none);
            if (rect.position != before) Moved?.Invoke();
        }

        private void DrawWindow(int id)
        {
            EnsureStyles();
            GUI.DrawTexture(new Rect(0f, 0f, rect.width, rect.height), background);
            float closeX = rect.width - Inset - CloseSize;
            GUI.Label(new Rect(Inset + 3f, HeaderY, closeX - Inset - 6f, HeaderHeight), Title, titleStyle);
            if (GUI.Button(new Rect(closeX, HeaderY + (HeaderHeight - CloseSize) * 0.5f, CloseSize, CloseSize), "×", closeStyle))
            {
                Enabled = false;
                Closed?.Invoke();
            }

            float top = HeaderY + HeaderHeight + RowGap;
            GUILayout.BeginArea(new Rect(Inset, top, ContentWidth, Mathf.Max(1f, rect.height - top - Inset)));
            GUILayout.BeginVertical();
            try { content?.Invoke(this); }
            catch (Exception e) when (!(e is ExitGUIException)) { Log.ExceptionOnce($"panel-{Title}", e, $"Drawing the {Title} panel failed"); }
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
                fittedHeight = Mathf.Ceil(top + GUILayoutUtility.GetLastRect().height + Inset);
            GUILayout.EndArea();

            GUI.DragWindow(new Rect(0f, 0f, closeX - 3f, HeaderY + HeaderHeight));
        }

        private Rect NextRect(float height)
        {
            Rect row = GUILayoutUtility.GetRect(0f, 10000f, height + RowGap, height + RowGap, GUILayout.ExpandWidth(true));
            return new Rect(row.x + 1f, row.y + RowGap * 0.5f, Mathf.Max(1f, row.width - 2f), height);
        }

        internal void Button(string text, Action? onClick, bool interactable = true)
        {
            Rect area = NextRect(ButtonHeight);
            buttonStyle!.fontSize = FittingFontSize(text, area.width - 6f);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && interactable;
            if (GUI.Button(area, text, buttonStyle)) onClick?.Invoke();
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
            GUI.color = color;
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

        private int FittingFontSize(string text, float availableWidth)
        {
            if (string.IsNullOrEmpty(text)) return ControlFontSize;
            measured.text = text;
            for (int size = ControlFontSize; size > MinimumFontSize; size--)
            {
                buttonStyle!.fontSize = size;
                if (buttonStyle.CalcSize(measured).x <= availableWidth) return size;
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
                background = Textures.CreateTexture(Px(rect.width), Px(rect.height), BackgroundColor, accent, Px(BorderThickness), Px(CornerRadius));
            if (button != null) return;
            button = Textures.CreateTexture(64, (int)ButtonHeight, ButtonColor, accent, 1, 4);
            field = Textures.CreateTexture(64, (int)ButtonHeight, FieldColor, accent, 1, 4);
            close = Textures.CreateTexture((int)CloseSize, (int)CloseSize, new Color32(115, 25, 25, 235), new Color32(175, 50, 50, 255), 1, 4);
            closeHover = Textures.CreateTexture((int)CloseSize, (int)CloseSize, new Color32(190, 35, 35, 255), new Color32(235, 75, 75, 255), 1, 4);
            if (buttonStyle != null) ApplyTextures();
        }

        private void EnsureStyles()
        {
            if (buttonStyle != null) return;
            var none = new RectOffset(0, 0, 0, 0);
            const int border = 5;
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(3, 3, 1, 1), margin = none, border = new RectOffset(border, border, border, border) };
            fieldStyle = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip, padding = new RectOffset(4, 4, 1, 1), margin = none, border = new RectOffset(border, border, border, border) };
            closeStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 11, padding = new RectOffset(0, 0, 0, 1), margin = none, border = new RectOffset(border, border, border, border) };
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = TitleFontSize, fontStyle = FontStyle.Bold, padding = none, margin = none, clipping = TextClipping.Clip };
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = ControlFontSize, padding = new RectOffset(2, 2, 0, 0), margin = none, wordWrap = true, clipping = TextClipping.Clip };
            foreach (GUIStyle style in new[] { buttonStyle, fieldStyle, closeStyle, titleStyle, labelStyle }) SetTextColor(style, Color.white);
            ApplyTextures();
        }

        private void ApplyTextures()
        {
            SetBackground(buttonStyle!, button, button);
            SetBackground(fieldStyle!, field, field);
            SetBackground(closeStyle!, close, closeHover);
        }

        private static void SetBackground(GUIStyle style, Texture2D? normal, Texture2D? hover)
        {
            style.normal.background = style.focused.background = style.onNormal.background = style.onFocused.background = normal;
            style.hover.background = style.active.background = style.onHover.background = style.onActive.background = hover;
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
            foreach (Texture2D? texture in new[] { button, field, close, closeHover })
                if (texture != null) UnityEngine.Object.Destroy(texture);
            button = field = close = closeHover = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Enabled = false;
            DestroyTextures();
            content = null;
            Closed = null;
            Moved = null;
        }
    }
}
