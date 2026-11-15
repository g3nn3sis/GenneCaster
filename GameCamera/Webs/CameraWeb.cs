// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.GameCamera.Webs
{
    internal static class CameraWeb
    {
        internal const int NoNode = -1;

        private static readonly List<Vector3> nodes = new List<Vector3>();
        private static string? loadedName;

        internal static IReadOnlyList<Vector3> Nodes => nodes;
        internal static string Name => CasterConfig.WebName.Value;

        internal static void Sync()
        {
            string name = Name;
            if (name == loadedName) return;
            loadedName = name;
            nodes.Clear();
            if (name.Length > 0) nodes.AddRange(CameraWebFile.Load(name));
        }

        internal static void Load(string name)
        {
            loadedName = null;
            CasterConfig.WebName.Value = name;
            Sync();
        }

        internal static bool Save(string rawName)
        {
            string name = Presets.Sanitize(rawName);
            if (name.Length == 0 || !CameraWebFile.Save(name, nodes)) return false;
            loadedName = name;
            CasterConfig.WebName.Value = name;
            return true;
        }

        internal static void Add(Vector3 position) => nodes.Add(position);

        internal static void RemoveNearest(Vector3 position)
        {
            int nearest = NearestIndex(position);
            if (nearest != NoNode) nodes.RemoveAt(nearest);
        }

        internal static void Clear() => nodes.Clear();

        internal static int NearestIndex(Vector3 position)
        {
            int nearest = NoNode;
            float best = float.PositiveInfinity;
            for (int i = 0; i < nodes.Count; i++)
            {
                float distance = (nodes[i] - position).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                nearest = i;
            }
            return nearest;
        }
    }
}
