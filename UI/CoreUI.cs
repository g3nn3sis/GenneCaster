// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.UI
{
    internal sealed class CoreUI : ObjectComponent
    {
        private const float OpenDuration = 0.18f;

        private PanelOrchestrator? panels;
        private float openProgress;
        internal static bool Visible = true;

        internal void Update()
        {
            panels ??= new PanelOrchestrator();
            if (CasterConfig.KeyPressed(CasterConfig.MenuKey))
            {
                Visible = !Visible;
                if (Visible) panels.ShowMain();
            }
            openProgress = Mathf.MoveTowards(openProgress, Visible ? 1f : 0f, Time.unscaledDeltaTime / OpenDuration);
            if (openProgress > 0f) panels.Tick(Time.unscaledDeltaTime);
        }

        internal void OnGUI()
        {
            if (panels == null || openProgress <= 0f) return;
            float eased = Mathf.SmoothStep(0f, 1f, openProgress);
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            UiScale.Begin();
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * eased);
            GUI.enabled = previousEnabled && Visible;
            try { panels.Draw(); }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        protected override void OnDestroy()
        {
            panels?.Dispose();
            panels = null;
            base.OnDestroy();
        }
    }
}
