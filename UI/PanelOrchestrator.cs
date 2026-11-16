// Copyright (c) 2026 Zerori contributors
// SPDX-License-Identifier: LicenseRef-Zerori-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

using BepInEx.Configuration;
using Zerori.Base;
using Zerori.Replay;
using Zerori.UI.Panels;
using UnityEngine;

namespace Zerori.UI
{
    internal sealed class PanelOrchestrator : IDisposable
    {
        private const int PanelCount = (int)PanelId.Count;
        private const float PanelWidth = 160f;
        private const float Margin = 8f;
        private const float Gap = 6f;
        private const float ColumnPitch = PanelWidth + Gap;
        private const float RowPitch = Panel.HeaderHeight + Gap;
        private const float PositionDamping = 16f;

        private readonly Panel[] panels = new Panel[PanelCount];
        private readonly ConfigEntry<string>[] positions = new ConfigEntry<string>[PanelCount];
        private readonly Action<Panel>[] drawContent = new Action<Panel>[PanelCount];
        private readonly PanelLayout layout = new PanelLayout(PanelCount);
        private readonly int[] columnOf = new int[PanelCount];
        private readonly float[] rowOf = new float[PanelCount];
        private readonly float[] heights = new float[PanelCount];
        private PanelId? floating;
        private int originColumn;
        private int originIndex;
        private int slotColumn;
        private int slotIndex;
        private bool placed;
        private bool replayLayout;

        internal PanelOrchestrator()
        {
            var content = new PanelContent(this);
            Color32 accent = ColorSpace.Current.Color;
            for (int i = 0; i < PanelCount; i++)
            {
                var id = (PanelId)i;
                panels[i] = new Panel(PanelWidth, PanelCatalog.Title(id), accent);
                drawContent[i] = content.For(id);
                positions[i] = CasterConfig.BindPanelPosition(PanelCatalog.PositionKey(id));
            }
            ArrangeFromConfig();
            panels[(int)PanelId.Main].Expanded = true;
        }

        private void ArrangeFromConfig()
        {
            PanelId[][] defaults = PanelCatalog.Columns;
            for (int column = 0; column < defaults.Length; column++)
            {
                for (int row = 0; row < defaults[column].Length; row++)
                {
                    int i = (int)defaults[column][row];
                    columnOf[i] = column;
                    rowOf[i] = Margin + row * RowPitch;
                }
            }
            for (int i = 0; i < PanelCount; i++)
            {
                if (!PositionFormat.TryParse(positions[i].Value, out Vector2 saved)) continue;
                columnOf[i] = Mathf.RoundToInt((saved.x - Margin) / ColumnPitch);
                rowOf[i] = saved.y;
            }
            layout.Arrange(columnOf, rowOf);
            floating = null;
        }

        internal void Tick(float dt, float timelinePresence)
        {
            float blend = placed ? 1f - Mathf.Exp(-PositionDamping * dt) : 1f;
            placed = true;
            for (int i = 0; i < PanelCount; i++)
            {
                panels[i].Animate(dt);
                heights[i] = panels[i].Rect.height;
            }
            PanelId? leader = SyncReplayLayout() ? PanelId.Replay : (PanelId?)null;
            float top = leader != null ? Mathf.Lerp(Margin, ReplayTimeline.Bottom + Gap, timelinePresence) : Margin;
            layout.Fit(Mathf.FloorToInt((UiScale.Width - Margin + Gap) / ColumnPitch));
            TrackFloating(leader, top);
            for (int column = 0; column < layout.ColumnCount; column++) PlaceColumn(column, leader, top, blend);
        }

        private bool SyncReplayLayout()
        {
            bool replaying = ReplaySession.IsReplaying;
            if (replaying && !replayLayout) panels[(int)PanelId.Replay].Expanded = true;
            replayLayout = replaying;
            return replaying;
        }

        private void TrackFloating(PanelId? leader, float top)
        {
            PanelId? dragged = DraggedPanel();
            if (floating is PanelId released && dragged != floating) Dock(released, leader);
            if (dragged is PanelId lifted && floating == null) Lift(lifted);
            if (floating is not PanelId id) return;
            Rect rect = panels[(int)id].Rect;
            slotColumn = Mathf.Clamp(Mathf.FloorToInt((rect.center.x - Margin + Gap * 0.5f) / ColumnPitch), 0, layout.ColumnCount - 1);
            slotIndex = layout.SlotIndex(slotColumn, leader, heights, Gap, top, rect.y - Gap * 0.5f);
        }

        private PanelId? DraggedPanel()
        {
            for (int i = 0; i < PanelCount; i++)
                if (panels[i].IsDragging) return (PanelId)i;
            return null;
        }

        private void Lift(PanelId id)
        {
            layout.Find(id, out originColumn, out originIndex);
            layout.Lift(id);
            floating = id;
        }

        private void Dock(PanelId id, PanelId? leader)
        {
            floating = null;
            layout.Dock(id, Mathf.Min(slotColumn, layout.ColumnCount - 1), slotIndex, leader);
            layout.Find(id, out int column, out int index);
            if (column != originColumn || index != originIndex) SaveLayout();
        }

        private void PlaceColumn(int column, PanelId? leader, float top, float blend)
        {
            List<PanelId> order = layout.Order(column, leader);
            bool opensSlot = floating != null && column == slotColumn;
            float x = Margin + column * ColumnPitch;
            float y = top;
            for (int i = 0; i < order.Count; i++)
            {
                if (opensSlot && i == slotIndex) y += heights[(int)floating!.Value] + Gap;
                int index = (int)order[i];
                Panel panel = panels[index];
                panel.SetPosition(Vector2.Lerp(panel.Rect.position, new Vector2(x, y), blend));
                y += heights[index] + Gap;
            }
        }

        internal void Draw()
        {
            for (int i = 0; i < PanelCount; i++)
            {
                panels[i].Draw(drawContent[i]);
                if (panels[i].IsDragging) panels[i].ClampToScreen();
            }
        }

        private void SaveLayout()
        {
            for (int column = 0; column < layout.ColumnCapacity; column++)
            {
                List<PanelId> order = layout.Order(column, null);
                for (int row = 0; row < order.Count; row++)
                    positions[(int)order[row]].Value = PositionFormat.Format(new Vector2(Margin + column * ColumnPitch, Margin + row * RowPitch));
            }
        }

        internal void ResetLayout()
        {
            for (int i = 0; i < PanelCount; i++) positions[i].Value = string.Empty;
            ArrangeFromConfig();
        }

        internal void SetTheme(int index)
        {
            if (index < 0 || index >= ColorSpace.Themes.Length) return;
            CasterConfig.Theme.Value = ColorSpace.Themes[index].Name;
            foreach (Panel panel in panels) panel.SetAccent(ColorSpace.Themes[index].Color);
        }

        internal void ReapplyFromConfig()
        {
            Color32 accent = ColorSpace.Current.Color;
            foreach (Panel panel in panels) panel.SetAccent(accent);
            ArrangeFromConfig();
        }

        public void Dispose()
        {
            foreach (Panel panel in panels) panel?.Dispose();
        }
    }
}
