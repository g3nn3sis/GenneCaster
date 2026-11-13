// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GenneCaster.Base;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace GenneCaster.Game
{
    internal sealed class AssetManager : ObjectComponent
    {
        internal static TMP_FontAsset[] discoveredFonts = Array.Empty<TMP_FontAsset>();
        internal static Sprite? smallMetaSprite;
        internal static Sprite? smallSteamSprite;

        private static readonly MethodInfo? LoadImageBytes = typeof(ImageConversion).GetMethod(
            "LoadImage", new[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });

        protected override void Awake()
        {
            base.Awake();
            try { discoveredFonts = DiscoverFonts(); }
            catch (Exception e) { Log.Exception(e, "Font discovery failed"); }
            smallSteamSprite = LoadIconSprite("GenneCaster.Icons.steam.png");
            smallMetaSprite = LoadIconSprite("GenneCaster.Icons.meta.png");
        }

        internal static string FontName(int index)
        {
            if (discoveredFonts.Length == 0) return "Default";
            string name = discoveredFonts[Mathf.Clamp(index, 0, discoveredFonts.Length - 1)].name.TrimStart('_');
            return name.EndsWith(" SDF", StringComparison.Ordinal) ? name.Substring(0, name.Length - 4) : name;
        }

        private static TMP_FontAsset[] DiscoverFonts()
        {
            TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            var clones = new List<TMP_FontAsset>(loaded.Length);
            foreach (TMP_FontAsset font in loaded)
            {
                TMP_FontAsset clone = Instantiate(font);
                DontDestroyOnLoad(clone);
                clone.name = $"_{font.name}";
                clones.Add(clone);
            }
            Log.Debug($"Discovered {clones.Count} fonts.");
            return clones.ToArray();
        }

        private static Sprite? LoadIconSprite(string resourceName)
        {
            using Stream? stream = typeof(AssetManager).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Log.Error($"Missing embedded icon {resourceName}.");
                return null;
            }
            if (LoadImageBytes == null)
            {
                Log.Error("ImageConversion.LoadImage(Texture2D, byte[], bool) was not found; platform icons cannot load.");
                return null;
            }
            byte[] bytes = new byte[stream.Length];
            int read = 0;
            while (read < bytes.Length)
            {
                int n = stream.Read(bytes, read, bytes.Length - read);
                if (n <= 0) break;
                read += n;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!(LoadImageBytes.Invoke(null, new object[] { texture, bytes, false }) is bool loaded && loaded))
            {
                Log.Error($"Failed to decode embedded icon {resourceName} ({read} of {bytes.Length} bytes read).");
                Destroy(texture);
                return null;
            }
            texture.name = resourceName;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height, 0, SpriteMeshType.FullRect);
            sprite.name = resourceName;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
    }
}
