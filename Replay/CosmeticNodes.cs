// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal static class CosmeticNodes
    {
        internal const int Unmatched = -1;

        internal static Transform[] Of(Transform root) => root.GetComponentsInChildren<Transform>(true);

        internal static int[] Align(string[] names, Transform[] nodes)
        {
            var matches = new int[names.Length];
            int next = 1;
            for (int i = 0; i < names.Length; i++)
            {
                matches[i] = i == 0 && nodes.Length > 0 ? 0 : Unmatched;
                if (i == 0) continue;
                for (int candidate = next; candidate < nodes.Length; candidate++)
                {
                    if (nodes[candidate].name != names[i]) continue;
                    matches[i] = candidate;
                    next = candidate + 1;
                    break;
                }
            }
            return matches;
        }
    }
}
