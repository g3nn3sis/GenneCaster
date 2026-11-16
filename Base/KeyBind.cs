// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace Zetori.Base
{
    internal sealed class KeyBind
    {
        internal string Label { get; }
        internal ConfigEntry<Key> Entry { get; }

        internal KeyBind(string label, ConfigEntry<Key> entry)
        {
            Label = label;
            Entry = entry;
        }
    }
}
