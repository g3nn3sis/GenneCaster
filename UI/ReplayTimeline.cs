// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Replay;
using UnityEngine;

namespace GenneCaster.UI
{
    internal sealed class ReplayTimeline : IDisposable
    {
        internal const float Margin = 8f;
        internal const float Height = 48f;
        internal static float Bottom => Margin + Height;

        private const float Inset = 10f;
        private const float Spacing = 8f;
        private const float ButtonWidth = 56f;
        private const float ButtonHeight = 24f;
        private const float ClockWidth = 96f;
        private const float RailHeight = 6f;
        private const float KnobWidth = 8f;
        private const float KnobHeight = 20f;
        private const float MarkerWidth = 2f;
        private const float MarkerHeight = 8f;
        private const float VoiceHeight = 3f;
        private const float HoverRadius = 4f;
        private const float TooltipWidth = 220f;
        private const float TooltipHeight = 20f;
        private const int FontSize = 12;
        private const int SliceBorder = 10;

        private static readonly Color32 BackgroundColor = new Color32(10, 10, 10, 190);
        private static readonly Color32 ButtonColor = new Color32(20, 5, 8, 210);
        private static readonly Color RailColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color VoiceColor = new Color(0.45f, 0.85f, 1f, 0.9f);
        private static readonly Color TagColor = new Color(1f, 0.33f, 0.05f, 1f);
        private static readonly Color JoinColor = new Color(0.35f, 0.9f, 0.45f, 1f);
        private static readonly Color LeaveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly Color EnvironmentColor = new Color(0.55f, 0.7f, 1f, 1f);

        private readonly int scrubHint = "GenneCasterReplayScrub".GetHashCode();
        private Texture2D? background, button;
        private GUIStyle? backgroundStyle, buttonStyle, clockStyle, tooltipStyle;
        private Color32 builtAccent;
        private bool resumeAfterScrub;

        internal static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        internal void Draw(ReplayPlayer replay)
        {
            EnsureStyles(ColorSpace.Current.Color);
            var bar = new Rect(Margin, Margin, UiScale.Width - Margin * 2f, Height);
            GUI.Box(bar, GUIContent.none, backgroundStyle);

            float buttonY = bar.y + (Height - ButtonHeight) * 0.5f;
            float x = bar.x + Inset;
            if (GUI.Button(new Rect(x, buttonY, ButtonWidth, ButtonHeight), replay.Playing ? "Pause" : "Play", buttonStyle)) replay.TogglePlaying();
            x += ButtonWidth + Spacing;
            GUI.Label(new Rect(x, bar.y, ClockWidth, Height), $"{Format(replay.Cursor)} / {Format(replay.Recording.Duration)}", clockStyle);
            x += ClockWidth + Spacing;

            float stopX = bar.xMax - Inset - ButtonWidth;
            if (GUI.Button(new Rect(stopX, buttonY, ButtonWidth, ButtonHeight), "Stop", buttonStyle))
            {
                ReplaySession.ExitReplay();
                return;
            }

            Rect track = Rect.MinMaxRect(x + KnobWidth * 0.5f, bar.y, stopX - Spacing - KnobWidth * 0.5f, bar.yMax);
            if (track.width <= 0f) return;
            Scrub(track, replay);
            DrawTrack(track, replay);
            DrawHover(track, bar, replay);
        }

        private void Scrub(Rect track, ReplayPlayer replay)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(scrubHint, FocusType.Passive, track);
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && track.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    resumeAfterScrub = replay.Playing;
                    replay.Playing = false;
                    replay.Seek(TimeAt(track, e.mousePosition.x, replay));
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    replay.Seek(TimeAt(track, e.mousePosition.x, replay));
                    e.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    replay.Playing = resumeAfterScrub;
                    e.Use();
                    break;
            }
        }

        private static void DrawTrack(Rect track, ReplayPlayer replay)
        {
            if (Event.current.type != EventType.Repaint) return;
            ReplayRecording recording = replay.Recording;
            var rail = new Rect(track.x, track.center.y - RailHeight * 0.5f, track.width, RailHeight);
            Fill(rail, RailColor);
            float progress = Progress(replay.Cursor, recording);
            Fill(new Rect(rail.x, rail.y, rail.width * progress, rail.height), ColorSpace.Current.Color);

            ReplayPuppet? spectated = replay.Freecam ? null : replay.Spectated;
            if (spectated != null)
            {
                foreach (VoiceSegment segment in spectated.Actor.Voice.Segments)
                {
                    float start = rail.x + rail.width * Progress(segment.Start, recording);
                    float end = rail.x + rail.width * Progress(segment.End, recording);
                    Fill(new Rect(start, rail.yMax + 3f, Mathf.Max(1f, end - start), VoiceHeight), VoiceColor);
                }
            }

            foreach (ReplayEvent replayEvent in recording.Events)
            {
                float markerX = rail.x + rail.width * Progress(replayEvent.Time, recording);
                Fill(new Rect(markerX - MarkerWidth * 0.5f, rail.y - MarkerHeight - 2f, MarkerWidth, MarkerHeight), MarkerColor(replayEvent.Kind));
            }

            float knobX = rail.x + rail.width * progress;
            Fill(new Rect(knobX - KnobWidth * 0.5f, track.center.y - KnobHeight * 0.5f, KnobWidth, KnobHeight), Color.white);
        }

        private void DrawHover(Rect track, Rect bar, ReplayPlayer replay)
        {
            Vector2 mouse = Event.current.mousePosition;
            if (!track.Contains(mouse)) return;
            ReplayRecording recording = replay.Recording;
            float time = TimeAt(track, mouse.x, replay);
            string text = Format(time);
            foreach (ReplayEvent replayEvent in recording.Events)
            {
                float markerX = track.x + track.width * Progress(replayEvent.Time, recording);
                if (Mathf.Abs(markerX - mouse.x) > HoverRadius) continue;
                text = $"{Format(replayEvent.Time)}  {recording.Describe(replayEvent)}";
                break;
            }
            float x = Mathf.Clamp(mouse.x - TooltipWidth * 0.5f, bar.x, bar.xMax - TooltipWidth);
            GUI.Label(new Rect(x, bar.yMax + 2f, TooltipWidth, TooltipHeight), text, tooltipStyle);
        }

        private static float TimeAt(Rect track, float x, ReplayPlayer replay) =>
            Mathf.Clamp01((x - track.x) / track.width) * replay.Recording.Duration;

        private static float Progress(float time, ReplayRecording recording) =>
            recording.Duration > 0f ? Mathf.Clamp01(time / recording.Duration) : 0f;

        private static Color MarkerColor(ReplayEventKind kind) => kind switch
        {
            ReplayEventKind.Tagged => TagColor,
            ReplayEventKind.Joined => JoinColor,
            ReplayEventKind.Left => LeaveColor,
            _ => EnvironmentColor
        };

        private static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previous.a);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles(Color32 accent)
        {
            if (background != null && builtAccent.Equals(accent) && backgroundStyle != null) return;
            DestroyTextures();
            builtAccent = accent;
            background = Textures.CreateTexture(64, 32, BackgroundColor, accent, 2, SliceBorder);
            button = Textures.CreateTexture(64, (int)ButtonHeight, ButtonColor, accent, 1, 4);
            var slice = new RectOffset(SliceBorder, SliceBorder, SliceBorder, SliceBorder);
            backgroundStyle = new GUIStyle { border = slice };
            backgroundStyle.normal.background = background;
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize, border = new RectOffset(5, 5, 5, 5) };
            buttonStyle.normal.background = buttonStyle.hover.background = buttonStyle.active.background = button;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = Color.white;
            clockStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize, fontStyle = FontStyle.Bold };
            clockStyle.normal.textColor = Color.white;
            tooltipStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = FontSize - 1, clipping = TextClipping.Clip };
            tooltipStyle.normal.textColor = Color.white;
        }

        private void DestroyTextures()
        {
            if (background != null) UnityEngine.Object.Destroy(background);
            if (button != null) UnityEngine.Object.Destroy(button);
            background = button = null;
        }

        public void Dispose() => DestroyTextures();
    }
}
