// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GenneCaster.GameCamera.Webs
{
    internal static class CameraWebFile
    {
        private const string Extension = ".gcweb";
        private const string Header = "GenneCaster Web 1";
        private const char Separator = ' ';
        private const int Axes = 3;

        internal static string[] Names()
        {
            string folder = DataFolders.Webs;
            if (!Directory.Exists(folder)) return Array.Empty<string>();
            try
            {
                string[] names = Directory.GetFiles(folder, "*" + Extension)
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToArray();
                Array.Sort(names, StringComparer.OrdinalIgnoreCase);
                return names;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Listing webs failed");
                return Array.Empty<string>();
            }
        }

        internal static bool Save(string name, IReadOnlyList<Vector3> nodes)
        {
            try
            {
                Directory.CreateDirectory(DataFolders.Webs);
                var lines = new List<string>(nodes.Count + 1) { Header };
                foreach (Vector3 node in nodes) lines.Add(Format(node));
                File.WriteAllLines(PathOf(name), lines);
                Log.Info($"Saved web '{name}' with {nodes.Count} nodes.");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Saving web '{name}' failed");
                return false;
            }
        }

        internal static List<Vector3> Load(string name)
        {
            var nodes = new List<Vector3>();
            string path = PathOf(name);
            if (!File.Exists(path)) return nodes;
            try
            {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length == 0 || lines[0].Trim() != Header)
                {
                    Log.Warning($"Web '{name}' is not a GenneCaster web file.");
                    return nodes;
                }
                for (int i = 1; i < lines.Length; i++)
                    if (TryParse(lines[i], out Vector3 node)) nodes.Add(node);
                Log.Info($"Loaded web '{name}' with {nodes.Count} nodes.");
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Loading web '{name}' failed");
            }
            return nodes;
        }

        internal static void Delete(string name)
        {
            try
            {
                string path = PathOf(name);
                if (File.Exists(path)) File.Delete(path);
                Log.Info($"Deleted web '{name}'.");
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Deleting web '{name}' failed");
            }
        }

        private static string PathOf(string name) => Path.Combine(DataFolders.Webs, name + Extension);

        private static string Format(Vector3 node) =>
            string.Join(Separator.ToString(), Format(node.x), Format(node.y), Format(node.z));

        private static string Format(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static bool TryParse(string line, out Vector3 node)
        {
            node = default;
            string[] parts = line.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != Axes) return false;
            if (!TryParse(parts[0], out float x) || !TryParse(parts[1], out float y) || !TryParse(parts[2], out float z)) return false;
            node = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParse(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
