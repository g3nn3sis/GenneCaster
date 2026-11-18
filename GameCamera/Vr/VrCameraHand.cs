// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.GameCamera.Vr
{
    internal sealed class VrCameraHand
    {
        internal bool IsLeft { get; }
        internal Transform Palm { get; private set; } = null!;
        internal Transform Finger { get; private set; } = null!;
        internal bool Gripping { get; private set; }
        internal bool GripStarted { get; private set; }

        internal VrCameraHand(bool isLeft) => IsLeft = isLeft;

        internal void Sync(Transform palm, Transform finger, bool gripping)
        {
            Palm = palm;
            Finger = finger;
            GripStarted = gripping && !Gripping;
            Gripping = gripping;
        }
    }
}
