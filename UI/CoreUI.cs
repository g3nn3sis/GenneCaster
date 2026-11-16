// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.Replay;
using UnityEngine;

namespace Zerori.UI
{
    internal sealed class CoreUI : ObjectComponent
    {
        internal const float FadeDuration = 0.18f;

        private PanelOrchestrator? panels;
        private ReplayTimeline? timeline;
        private float panelProgress;
        private float timelineProgress;
        private bool wasReplaying;
        internal static bool Visible = true;
        internal static bool TimelineVisible = true;

        internal void Update()
        {
            panels ??= new PanelOrchestrator();
            KeyBinds.Tick();
            if (CasterConfig.KeyPressed(CasterConfig.MenuKey)) Visible = !Visible;
            SyncTimelineToggle();
            float step = Time.unscaledDeltaTime / FadeDuration;
            panelProgress = Mathf.MoveTowards(panelProgress, Visible ? 1f : 0f, step);
            timelineProgress = Mathf.MoveTowards(timelineProgress, TimelineVisible ? 1f : 0f, step);
            if (panelProgress > 0f) panels.Tick(Time.unscaledDeltaTime, Fade(timelineProgress));
        }

        private void SyncTimelineToggle()
        {
            bool replaying = ReplaySession.IsReplaying;
            if (replaying && !wasReplaying) TimelineVisible = true;
            wasReplaying = replaying;
            if (replaying && CasterConfig.KeyPressed(CasterConfig.ReplayTimelineKey)) TimelineVisible = !TimelineVisible;
        }

        private static float Fade(float progress) => Mathf.SmoothStep(0f, 1f, progress);

        internal void OnGUI()
        {
            if (panels == null) return;
            ReplayPlayer? replay = ReplaySession.Player;
            bool showTimeline = replay != null && timelineProgress > 0f;
            if (!showTimeline && panelProgress <= 0f) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            UiScale.Begin();
            try
            {
                if (showTimeline)
                {
                    BeginLayer(previousColor, previousEnabled, timelineProgress, TimelineVisible);
                    (timeline ??= new ReplayTimeline()).Draw(replay!);
                }
                if (panelProgress > 0f)
                {
                    BeginLayer(previousColor, previousEnabled, panelProgress, Visible);
                    panels.Draw();
                }
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        private static void BeginLayer(Color baseColor, bool baseEnabled, float progress, bool interactive)
        {
            GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Fade(progress));
            GUI.enabled = baseEnabled && interactive;
        }

        protected override void OnDestroy()
        {
            panels?.Dispose();
            panels = null;
            timeline?.Dispose();
            timeline = null;
            base.OnDestroy();
        }
    }
}
