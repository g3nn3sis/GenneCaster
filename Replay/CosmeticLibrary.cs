// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using System.Reflection;
using GorillaTag.CosmeticSystem;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zetori.Base;

namespace Zetori.Replay
{
    internal sealed class CosmeticLibrary : IDisposable
    {
        private sealed class Entry
        {
            internal AsyncOperationHandle<GameObject> Load;
            internal GameObject? Template;
            internal GorillaSkin? Skin;
            internal bool Finished;
        }

        private static readonly FieldInfo? SkinField = AccessTools.Field(typeof(GorillaSkinToggle), "_skin");

        private static GameObject? shelf;

        private readonly Dictionary<CosmeticKey, Entry> entries = new Dictionary<CosmeticKey, Entry>();
        private Dictionary<string, GorillaSkin>? namedSkins;
        private Dictionary<string, AudioMixerGroup>? mixers;

        private static Transform Shelf
        {
            get
            {
                if (shelf != null) return shelf.transform;
                shelf = new GameObject("_Zetori Replay Cosmetics");
                shelf.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(shelf);
                return shelf.transform;
            }
        }

        internal GameObject? Template(CosmeticKey key) => Ready(key)?.Template;

        internal bool TryCosmeticSkin(ReplayActor actor, string playFabId, out GorillaSkin? skin)
        {
            skin = null;
            bool pending = false;
            foreach (RecordedCosmetic cosmetic in actor.Cosmetics)
            {
                if (cosmetic.Key.PlayFabId != playFabId || cosmetic.Key.Kind == CosmeticKind.Holdable) continue;
                Entry? entry = Ready(cosmetic.Key);
                if (entry == null)
                {
                    pending = true;
                    continue;
                }
                if (entry.Skin == null) continue;
                skin = entry.Skin;
                return true;
            }
            return !pending;
        }

        internal GorillaSkin? NamedSkin(string name)
        {
            namedSkins ??= Resources.FindObjectsOfTypeAll<GorillaSkin>()
                .Where(skin => skin != null && !string.IsNullOrEmpty(skin.name))
                .GroupBy(skin => skin.name)
                .ToDictionary(group => group.Key, group => group.First());
            return namedSkins.TryGetValue(name, out GorillaSkin skin) ? skin : null;
        }

        internal AudioMixerGroup? Mixer(string name)
        {
            if (name.Length == 0) return null;
            mixers ??= Resources.FindObjectsOfTypeAll<AudioMixerGroup>()
                .Where(group => group != null && !string.IsNullOrEmpty(group.name))
                .GroupBy(group => group.name)
                .ToDictionary(group => group.Key, group => group.First());
            return mixers.TryGetValue(name, out AudioMixerGroup group) ? group : null;
        }

        private Entry? Ready(CosmeticKey key)
        {
            CosmeticKey prefab = key.Prefab;
            if (!entries.TryGetValue(prefab, out Entry entry))
            {
                entry = Request(prefab);
                entries.Add(prefab, entry);
            }
            if (!entry.Finished && entry.Load.IsValid() && entry.Load.IsDone) Finish(prefab, entry);
            return entry.Finished ? entry : null;
        }

        private static Entry Request(CosmeticKey key)
        {
            var entry = new Entry();
            CosmeticSO? cosmetic = CosmeticCatalog.Find(key.PlayFabId);
            CosmeticPart[] parts = cosmetic != null ? CosmeticCatalog.PartsOf(cosmetic, key.Kind) : Array.Empty<CosmeticPart>();
            if (key.Part < 0 || key.Part >= parts.Length || !CosmeticCatalog.Loadable(parts[key.Part]))
            {
                entry.Finished = true;
                return entry;
            }
            try { entry.Load = parts[key.Part].prefabAssetRef.InstantiateAsync(Shelf); }
            catch (Exception e)
            {
                Log.ExceptionOnce($"replay-cosmetic-{key.PlayFabId}", e, $"Loading the cosmetic {key.PlayFabId} for the replay failed");
                entry.Finished = true;
            }
            return entry;
        }

        private static void Finish(CosmeticKey key, Entry entry)
        {
            entry.Finished = true;
            if (entry.Load.Status != AsyncOperationStatus.Succeeded || entry.Load.Result == null)
            {
                Log.WarningOnce($"replay-cosmetic-{key.PlayFabId}", $"The cosmetic {key.PlayFabId} could not be loaded for the replay.");
                return;
            }
            GameObject loaded = entry.Load.Result;
            GorillaSkinToggle? toggle = loaded.GetComponentInChildren<GorillaSkinToggle>(true);
            entry.Skin = toggle != null ? SkinField?.GetValue(toggle) as GorillaSkin : null;
            entry.Template = BuildTemplate(key, VisualRoot(loaded, key.Kind));
        }

        private static GameObject? BuildTemplate(CosmeticKey key, Transform source)
        {
            GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, Shelf, false);
            try
            {
                CosmeticStripper.Strip(copy);
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;
                copy.SetActive(true);
                return copy;
            }
            catch (Exception e)
            {
                UnityEngine.Object.Destroy(copy);
                Log.ExceptionOnce($"replay-cosmetic-copy-{key.PlayFabId}", e, $"Preparing the cosmetic {key.PlayFabId} for the replay failed");
                return null;
            }
        }

        private static Transform VisualRoot(GameObject loaded, CosmeticKind kind)
        {
            if (kind != CosmeticKind.Holdable) return loaded.transform;
            HoldableObject? holdable = loaded.GetComponentInChildren<HoldableObject>(true);
            return holdable != null ? holdable.transform : loaded.transform;
        }

        private static void Release(AsyncOperationHandle<GameObject> load)
        {
            if (!load.IsValid()) return;
            if (load.Status == AsyncOperationStatus.Succeeded) Addressables.ReleaseInstance(load);
            else Addressables.Release(load);
        }

        public void Dispose()
        {
            foreach (Entry entry in entries.Values)
            {
                if (entry.Template != null) UnityEngine.Object.Destroy(entry.Template);
                if (!entry.Load.IsValid()) continue;
                if (entry.Load.IsDone) Release(entry.Load);
                else entry.Load.Completed += Release;
            }
            entries.Clear();
        }
    }
}
