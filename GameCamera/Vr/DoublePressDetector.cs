// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.GameCamera.Vr
{
    internal sealed class DoublePressDetector
    {
        private readonly float window;
        private bool wasPressed;
        private float lastPress = float.NegativeInfinity;

        internal DoublePressDetector(float window) => this.window = window;

        internal bool Update(bool pressed, float now)
        {
            bool started = pressed && !wasPressed;
            wasPressed = pressed;
            if (!started) return false;
            if (now - lastPress <= window)
            {
                lastPress = float.NegativeInfinity;
                return true;
            }
            lastPress = now;
            return false;
        }
    }
}
