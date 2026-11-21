// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace Zetori.Replay
{
    [Flags]
    internal enum CosmeticNodeFlags : byte
    {
        None = 0,
        Active = 1,
        Visible = 2,
        Emitting = 4,
        Lit = 8,
        Trailing = 16,
    }
}
