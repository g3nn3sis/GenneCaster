// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zetori.Base;
using Zetori.Player;
using Zetori.Replay;
using Photon.Pun;
using TMPro;
using UnityEngine;

namespace Zetori.Game
{
    internal sealed class ClosestLava : HudOverlay
    {
        internal static new bool enabled { get => CasterConfig.ClosestLavaEnabled.Value; set => CasterConfig.ClosestLavaEnabled.Value = value; }

        private RectTransform? root;
        private TMP_Text? value;
        private Vector2 basePos;
        private string lastText = string.Empty;

        protected override bool Active => enabled && (ReplaySession.Player is ReplayPlayer replay
            ? replay.Spectated != null
            : PhotonNetwork.InRoom && NetworkedPlayerManager.selectedPlayer != null);

        protected override GameObject? Prefab => HudAssets.ClosestLavaPrefab;

        protected override bool Bind(GameObject canvas)
        {
            root = canvas.transform.Find("ClosestLava") as RectTransform;
            value = root != null ? root.Find("Value")?.GetComponent<TMP_Text>() : null;
            if (root == null || value == null) return false;
            basePos = root.anchoredPosition;
            return true;
        }

        protected override void Refresh()
        {
            Place(root!, basePos, CasterConfig.ClosestLavaScale, CasterConfig.ClosestLavaOffsetX, CasterConfig.ClosestLavaOffsetY);
            ApplyFontIfChanged();

            float? closest = ReplaySession.Player is ReplayPlayer replay ? ReplayClosest(replay) : LiveClosest();
            if (closest == null) return;
            float best = closest.Value;

            string text = float.IsPositiveInfinity(best) ? "--" : $"{best:0.0}m";
            if (text != lastText)
            {
                lastText = text;
                value!.text = $"CLOSEST LAVA  <color=#FFFFFF>{text}</color>";
            }
        }

        private static float? LiveClosest()
        {
            VRRig? target = NetworkedPlayerManager.selectedPlayer;
            if (target == null) return null;
            float best = float.PositiveInfinity;
            Vector3 from = target.transform.position;
            foreach (VRRig rig in NetworkedPlayerManager.activeRigs ?? Array.Empty<VRRig>())
            {
                if (rig == null || !RigTagState.IsLava(rig)) continue;
                float distance = Vector3.Distance(from, rig.transform.position);
                if (distance < best) best = distance;
            }
            return best;
        }

        private static float? ReplayClosest(ReplayPlayer replay)
        {
            ReplayPuppet? target = replay.Spectated;
            if (target == null) return null;
            float best = float.PositiveInfinity;
            Vector3 from = target.Body.position;
            foreach (ReplayPuppet puppet in replay.Puppets)
            {
                if (!puppet.Visible || puppet.Current.Tag != ReplayTagState.Lava) continue;
                float distance = Vector3.Distance(from, puppet.Body.position);
                if (distance < best) best = distance;
            }
            return best;
        }
    }
}
