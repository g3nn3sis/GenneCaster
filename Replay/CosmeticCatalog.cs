// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using GorillaNetworking;
using GorillaTag;
using GorillaTag.CosmeticSystem;

namespace Zetori.Replay
{
    internal static class CosmeticCatalog
    {
        private const string LeftSuffix = " LEFT.";
        private const string RightSuffix = " RIGHT.";

        private static Dictionary<string, CosmeticSO>? byPlayFabId;

        internal static CosmeticSO? Find(string playFabId)
        {
            Dictionary<string, CosmeticSO>? lookup = Lookup();
            return lookup != null && lookup.TryGetValue(playFabId, out CosmeticSO cosmetic) ? cosmetic : null;
        }

        internal static bool TryParse(string objectName, out string playFabId, out ECosmeticSelectSide side)
        {
            side = objectName.EndsWith(LeftSuffix, StringComparison.Ordinal) ? ECosmeticSelectSide.Left
                : objectName.EndsWith(RightSuffix, StringComparison.Ordinal) ? ECosmeticSelectSide.Right
                : ECosmeticSelectSide.Both;
            int suffixLength = side switch
            {
                ECosmeticSelectSide.Left => LeftSuffix.Length,
                ECosmeticSelectSide.Right => RightSuffix.Length,
                _ => 0
            };
            playFabId = objectName.Substring(0, objectName.Length - suffixLength);
            return playFabId.Length > 0;
        }

        internal static bool IsAttached(CosmeticSO cosmetic) =>
            cosmetic.info.category.Value != CosmeticsController.CosmeticCategory.Collectable;

        internal static CosmeticPart[] PartsOf(CosmeticSO cosmetic, CosmeticKind kind) =>
            kind switch
            {
                CosmeticKind.Holdable => cosmetic.info.holdableParts,
                CosmeticKind.LocalRig => cosmetic.info.localRigParts,
                _ => cosmetic.info.functionalParts,
            } ?? Array.Empty<CosmeticPart>();

        internal static bool Loadable(CosmeticPart part) => part.prefabAssetRef != null && part.prefabAssetRef.RuntimeKeyIsValid();

        internal static int HoldablePart(CosmeticSO cosmetic, ECosmeticSelectSide side)
        {
            CosmeticPart[] parts = PartsOf(cosmetic, CosmeticKind.Holdable);
            for (int i = 0; i < parts.Length; i++)
            {
                CosmeticAttachInfo[]? anchors = parts[i].attachAnchors;
                if (Loadable(parts[i]) && anchors != null && anchors.Length > 0 && anchors[0].selectSide.Value == side) return i;
            }
            for (int i = 0; i < parts.Length; i++)
                if (Loadable(parts[i])) return i;
            return -1;
        }

        private static Dictionary<string, CosmeticSO>? Lookup()
        {
            if (byPlayFabId != null) return byPlayFabId;
            AllCosmeticsArraySO? all = CosmeticsController.LoadedAllCosmeticsSO;
            if (all == null || all.sturdyAssetRefs == null) return null;
            var built = new Dictionary<string, CosmeticSO>(all.sturdyAssetRefs.Length);
            foreach (GTDirectAssetRef<CosmeticSO> reference in all.sturdyAssetRefs)
            {
                CosmeticSO? cosmetic = reference.obj;
                if (cosmetic == null || string.IsNullOrEmpty(cosmetic.info.playFabID) || built.ContainsKey(cosmetic.info.playFabID)) continue;
                built.Add(cosmetic.info.playFabID, cosmetic);
            }
            byPlayFabId = built;
            return built;
        }
    }
}
