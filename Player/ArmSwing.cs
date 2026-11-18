// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Player
{
    internal sealed class ArmSwing
    {
        private const float NeutralFollowRate = 1.5f;

        private bool started;
        private Vector3 neutral;

        internal void Reset() => started = false;

        internal Vector3 Exaggerate(Transform body, Vector3 hand, float amount, float deltaTime)
        {
            Vector3 local = body.InverseTransformPoint(hand);
            neutral = started ? Vector3.Lerp(neutral, local, 1f - Mathf.Exp(-NeutralFollowRate * deltaTime)) : local;
            started = true;
            return body.TransformPoint(neutral + (local - neutral) * amount);
        }
    }
}
