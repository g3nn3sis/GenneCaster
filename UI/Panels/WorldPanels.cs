// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using Zerori.Base;
using Zerori.Game;
using UnityEngine;

namespace Zerori.UI.Panels
{
    internal sealed class WorldPanels
    {
        private static readonly HashSet<string> NonZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "none", "count", "all", "max", "unknown" };

        private readonly string[] zoneNames;
        private readonly Action[] zoneButtons;
        private Vector2 mapScroll;

        internal WorldPanels()
        {
            GTZone[] zones = ((GTZone[])Enum.GetValues(typeof(GTZone))).Distinct().Where(z => !NonZones.Contains(z.ToString())).ToArray();
            zoneNames = zones.Select(z => DisplayNames.Humanize(z.ToString())).ToArray();
            zoneButtons = new Action[zones.Length];
            for (int i = 0; i < zones.Length; i++)
            {
                GTZone zone = zones[i];
                zoneButtons[i] = () => LoadZone(zone);
            }
        }

        internal static void DrawTime(Panel panel)
        {
            panel.HorizontalArea(() =>
            {
                panel.Button("Morning", TimeManager.SetMorning);
                panel.Button("Day", TimeManager.SetDay);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Evening", TimeManager.SetEvening);
                panel.Button("Night", TimeManager.SetNight);
            });
            panel.HorizontalArea(() =>
            {
                panel.Button("Start Rain", TimeManager.StartRain);
                panel.Button("Stop Rain", TimeManager.StopRain);
            });
        }

        internal void DrawMapLoader(Panel panel)
        {
            mapScroll = panel.ScrollArea(mapScroll, Panel.ListHeight, zoneButtons.Length, () =>
            {
                for (int i = 0; i < zoneButtons.Length; i++)
                    panel.Button(zoneNames[i], zoneButtons[i]);
            });
        }

        private static void LoadZone(GTZone zone)
        {
            try { ZoneManagement.SetActiveZone(zone); }
            catch (Exception e) { Log.Exception(e, $"Loading {zone} failed"); }
        }
    }
}
