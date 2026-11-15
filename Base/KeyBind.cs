// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using UnityEngine.InputSystem;

namespace GenneCaster.Base
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
