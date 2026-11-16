// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zerori.GameCamera.Webs
{
    internal readonly struct WebSubject
    {
        internal readonly Transform Body;
        internal readonly Vector3 Position;
        internal readonly bool Tagged;
        internal readonly float Interest;

        internal WebSubject(Transform body, Vector3 position, bool tagged, float interest)
        {
            Body = body;
            Position = position;
            Tagged = tagged;
            Interest = interest;
        }
    }
}
