// Copyright (c) 2026 Zetori contributors
// SPDX-License-Identifier: LicenseRef-Zetori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using UnityEngine;

namespace Zetori.Replay
{
    internal static class CosmeticStripper
    {
        private const string TextNamespace = "TMPro";
        private const string InterfaceNamespace = "UnityEngine.UI";

        private static readonly Dictionary<Type, Type[]> requirements = new Dictionary<Type, Type[]>();

        internal static void Strip(GameObject root)
        {
            var doomed = root.GetComponentsInChildren<Component>(true).Where(component => component != null && !Kept(component)).ToList();
            while (doomed.Count > 0)
            {
                int before = doomed.Count;
                for (int i = doomed.Count - 1; i >= 0; i--)
                {
                    Component component = doomed[i];
                    if (component != null && RequiredByNeighbour(component)) continue;
                    if (component != null) UnityEngine.Object.DestroyImmediate(component);
                    doomed.RemoveAt(i);
                }
                if (doomed.Count == before) DiscardTangled(root, doomed);
            }
        }

        private static bool Kept(Component component) =>
            component is Transform
            || component is MeshFilter
            || component is Renderer
            || component is ParticleSystem
            || component is LODGroup
            || component is Light
            || component is Cloth
            || component is Canvas
            || component is CanvasRenderer
            || IsPresentation(component.GetType().Namespace);

        private static bool IsPresentation(string? space) =>
            space != null && (space == TextNamespace || space.StartsWith(TextNamespace + ".", StringComparison.Ordinal) || space == InterfaceNamespace);

        private static bool RequiredByNeighbour(Component component)
        {
            Type type = component.GetType();
            foreach (Component neighbour in component.GetComponents<Component>())
            {
                if (neighbour == null || neighbour == component) continue;
                foreach (Type required in RequirementsOf(neighbour.GetType()))
                    if (required.IsAssignableFrom(type)) return true;
            }
            return false;
        }

        private static Type[] RequirementsOf(Type type)
        {
            if (requirements.TryGetValue(type, out Type[] known)) return known;
            Type[] found = type.GetCustomAttributes(typeof(RequireComponent), true)
                .OfType<RequireComponent>()
                .SelectMany(attribute => new[] { attribute.m_Type0, attribute.m_Type1, attribute.m_Type2 })
                .Where(required => required != null)
                .ToArray();
            requirements[type] = found;
            return found;
        }

        private static void DiscardTangled(GameObject root, List<Component> doomed)
        {
            foreach (Component component in doomed)
            {
                if (component == null) continue;
                if (component.gameObject == root) throw new InvalidOperationException($"The cosmetic {root.name} has scripts that cannot be removed safely.");
                UnityEngine.Object.DestroyImmediate(component.gameObject);
            }
            doomed.Clear();
        }
    }
}
