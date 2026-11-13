// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.IO;

namespace GenneCaster.Base
{
    internal static class Presets
    {
        private const string FolderName = "GenneCaster-presets";
        private const string Extension = ".cfg";

        internal static string? Directory
        {
            get
            {
                string? config = CasterConfig.ConfigFilePath;
                string? dir = config != null ? Path.GetDirectoryName(config) : null;
                return dir != null ? Path.Combine(dir, FolderName) : null;
            }
        }

        internal static string[] Names()
        {
            string? dir = Directory;
            if (dir == null || !System.IO.Directory.Exists(dir)) return Array.Empty<string>();
            try
            {
                string[] files = System.IO.Directory.GetFiles(dir, "*" + Extension);
                var names = new string[files.Length];
                for (int i = 0; i < files.Length; i++) names[i] = Path.GetFileNameWithoutExtension(files[i]);
                Array.Sort(names, StringComparer.OrdinalIgnoreCase);
                return names;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Listing presets failed");
                return Array.Empty<string>();
            }
        }

        internal static bool Save(string rawName)
        {
            string name = Sanitize(rawName);
            string? dir = Directory;
            string? live = CasterConfig.ConfigFilePath;
            if (name.Length == 0 || dir == null || live == null) return false;
            try
            {
                CasterConfig.ForceSave();
                System.IO.Directory.CreateDirectory(dir);
                File.Copy(live, Path.Combine(dir, name + Extension), true);
                Log.Info($"Saved preset '{name}'.");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Saving preset '{name}' failed");
                return false;
            }
        }

        internal static bool Load(string name)
        {
            string? dir = Directory;
            string? live = CasterConfig.ConfigFilePath;
            if (dir == null || live == null) return false;
            string path = Path.Combine(dir, name + Extension);
            if (!File.Exists(path)) return false;
            try
            {
                File.WriteAllText(live, ConfigFormat.StripComments(File.ReadAllText(path)));
                CasterConfig.Reload();
                Log.Info($"Loaded preset '{name}'.");
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Loading preset '{name}' failed");
                return false;
            }
        }

        internal static void Delete(string name)
        {
            string? dir = Directory;
            if (dir == null) return;
            string path = Path.Combine(dir, name + Extension);
            try
            {
                if (File.Exists(path)) File.Delete(path);
                Log.Info($"Deleted preset '{name}'.");
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Deleting preset '{name}' failed");
            }
        }

        internal static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var builder = new System.Text.StringBuilder(name.Length);
            foreach (char c in name.Trim())
                if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_') builder.Append(c);
            return builder.ToString().Trim();
        }
    }
}
