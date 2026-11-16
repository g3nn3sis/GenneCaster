// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using UnityEngine;

namespace Zerori.UI.Panels
{
    internal sealed class KeybindPanel
    {
        private const float ListHeight = 264f;

        private readonly Action[] rebind;
        private Vector2 scroll;

        internal KeybindPanel()
        {
            rebind = new Action[KeyBinds.All.Length];
            for (int i = 0; i < rebind.Length; i++)
            {
                int index = i;
                rebind[i] = () => KeyBinds.ToggleListening(index);
            }
        }

        internal void Draw(Panel panel)
        {
            KeyBind[] binds = KeyBinds.All;
            scroll = panel.ScrollArea(scroll, ListHeight, binds.Length, () =>
            {
                for (int i = 0; i < binds.Length; i++)
                    panel.Button(KeyBinds.IsListening(i) ? $"{binds[i].Label}: press a key" : $"{binds[i].Label}: {binds[i].Entry.Value}", rebind[i]);
            });
            panel.Button("Reset Keys", KeyBinds.ResetAll);
        }
    }
}
