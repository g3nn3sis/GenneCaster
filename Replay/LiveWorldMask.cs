// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Player;
using UnityEngine;

namespace GenneCaster.Replay
{
    internal sealed class LiveWorldMask : IDisposable
    {
        private const float RescanInterval = 1f;

        private readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
        private readonly HashSet<AudioSource> muted = new HashSet<AudioSource>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<AudioSource> sources = new List<AudioSource>();
        private readonly List<Renderer> rendererScratch = new List<Renderer>();
        private readonly List<AudioSource> sourceScratch = new List<AudioSource>();
        private float nextScan;

        internal void Enforce()
        {
            if (Time.unscaledTime >= nextScan) Rescan();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                renderer.enabled = false;
                hidden.Add(renderer);
            }
            foreach (AudioSource source in sources)
            {
                if (source == null || source.mute) continue;
                source.mute = true;
                muted.Add(source);
            }
        }

        private void Rescan()
        {
            nextScan = Time.unscaledTime + RescanInterval;
            renderers.Clear();
            sources.Clear();
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null) continue;
                rig.GetComponentsInChildren(true, rendererScratch);
                renderers.AddRange(rendererScratch);
                rig.GetComponentsInChildren(true, sourceScratch);
                sources.AddRange(sourceScratch);
            }
        }

        public void Dispose()
        {
            foreach (Renderer renderer in hidden)
                if (renderer != null) renderer.enabled = true;
            foreach (AudioSource source in muted)
                if (source != null) source.mute = false;
            hidden.Clear();
            muted.Clear();
            renderers.Clear();
            sources.Clear();
        }
    }
}
