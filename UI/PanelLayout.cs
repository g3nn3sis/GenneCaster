// Copyright (c) 2026 GenneCaster contributors
// SPDX-License-Identifier: LicenseRef-GenneCaster-Source-Available
// Unauthorized use, modification, or distribution is prohibited. See LICENSE.

namespace GenneCaster.UI
{
    internal sealed class PanelLayout
    {
        private readonly List<PanelId>[] columns;
        private readonly List<PanelId> order = new List<PanelId>((int)PanelId.Count);

        internal int ColumnCapacity => columns.Length;
        internal int ColumnCount { get; private set; }

        internal PanelLayout(int columnCapacity)
        {
            columns = new List<PanelId>[columnCapacity];
            for (int i = 0; i < columnCapacity; i++) columns[i] = new List<PanelId>((int)PanelId.Count);
            ColumnCount = columnCapacity;
        }

        internal void Arrange(int[] columnOf, float[] rowOf)
        {
            foreach (List<PanelId> column in columns) column.Clear();
            for (int i = 0; i < (int)PanelId.Count; i++)
                columns[Math.Max(0, Math.Min(columnOf[i], columns.Length - 1))].Add((PanelId)i);
            foreach (List<PanelId> column in columns)
                column.Sort((a, b) => rowOf[(int)a] != rowOf[(int)b] ? rowOf[(int)a].CompareTo(rowOf[(int)b]) : ((int)a).CompareTo((int)b));
        }

        internal void Fit(int usableColumns)
        {
            ColumnCount = Math.Max(1, Math.Min(usableColumns, columns.Length));
            List<PanelId> last = columns[ColumnCount - 1];
            for (int i = ColumnCount; i < columns.Length; i++)
            {
                if (columns[i].Count == 0) continue;
                last.AddRange(columns[i]);
                columns[i].Clear();
            }
        }

        internal bool Find(PanelId id, out int column, out int index)
        {
            for (column = 0; column < columns.Length; column++)
            {
                index = IndexOf(columns[column], id);
                if (index >= 0) return true;
            }
            column = -1;
            index = -1;
            return false;
        }

        internal void Lift(PanelId id)
        {
            if (Find(id, out int column, out int index)) columns[column].RemoveAt(index);
        }

        internal void Dock(PanelId id, int column, int slot, PanelId? leader)
        {
            List<PanelId> shown = Order(column, leader);
            List<PanelId> list = columns[column];
            int index = slot >= shown.Count ? list.Count : slot <= 0 ? 0 : IndexOf(list, shown[slot]);
            list.Insert(index, id);
        }

        internal List<PanelId> Order(int column, PanelId? leader)
        {
            order.Clear();
            List<PanelId> list = columns[column];
            bool leads = leader is PanelId first && IndexOf(list, first) >= 0;
            if (leads) order.Add(leader!.Value);
            for (int i = 0; i < list.Count; i++)
                if (!leads || list[i] != leader!.Value) order.Add(list[i]);
            return order;
        }

        internal int SlotIndex(int column, PanelId? leader, float[] heights, float gap, float top, float pointerY)
        {
            List<PanelId> shown = Order(column, leader);
            float y = top;
            for (int i = 0; i < shown.Count; i++)
            {
                float height = heights[(int)shown[i]];
                if (pointerY < y + height * 0.5f) return i;
                y += height + gap;
            }
            return shown.Count;
        }

        private static int IndexOf(List<PanelId> list, PanelId id)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == id) return i;
            return -1;
        }
    }
}
