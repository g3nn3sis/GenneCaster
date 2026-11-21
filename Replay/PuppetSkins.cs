// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal sealed class PuppetSkins : IDisposable
    {
        internal sealed class Look
        {
            internal readonly Material? Body;
            internal readonly Material? Chest;
            internal readonly Mesh? Mesh;

            internal Look(Material? body, Material? chest, Mesh? mesh)
            {
                Body = body;
                Chest = chest;
                Mesh = mesh;
            }
        }

        private readonly ReplayActor actor;
        private readonly CosmeticLibrary library;
        private readonly Dictionary<int, Look> resolved = new Dictionary<int, Look>();
        private readonly List<Material> owned = new List<Material>();

        internal PuppetSkins(ReplayActor actor, CosmeticLibrary library)
        {
            this.actor = actor;
            this.library = library;
        }

        internal bool TryResolve(int index, out Look? look)
        {
            look = null;
            if (index == RecordedSkin.None) return true;
            if (resolved.TryGetValue(index, out Look known))
            {
                look = known;
                return true;
            }
            RecordedSkin recorded = actor.Skins[index];
            GorillaSkin? skin;
            if (recorded.Source.Length == 0) skin = library.NamedSkin(recorded.AssetName);
            else if (!library.TryCosmeticSkin(actor, recorded.Source, out skin)) return false;
            look = skin != null ? Build(skin, recorded) : new Look(null, null, null);
            resolved.Add(index, look);
            return true;
        }

        private Look Build(GorillaSkin skin, RecordedSkin recorded)
        {
            Mesh? mesh = skin.bodyMesh;
            bool fits = mesh != null && mesh.bindposes.Length == actor.Skeleton.BoneCount;
            return new Look(Copy(skin.bodyMaterial, recorded.BodyColor), Copy(skin.chestMaterial, recorded.ChestColor), fits ? mesh : null);
        }

        private Material? Copy(Material? source, Color color)
        {
            if (source == null) return null;
            var copy = new Material(source);
            RendererColors.Tint(copy, color);
            owned.Add(copy);
            return copy;
        }

        public void Dispose()
        {
            foreach (Material material in owned)
                if (material != null) UnityEngine.Object.Destroy(material);
            owned.Clear();
            resolved.Clear();
        }
    }
}
