using System.Globalization;
using UnityEngine;

namespace GenneCaster.UI
{
    internal static class PositionFormat
    {
        internal static bool TryParse(string value, out Vector2 position)
        {
            position = Vector2.zero;
            if (string.IsNullOrEmpty(value)) return false;
            string[] parts = value.Split(',');
            if (parts.Length != 2) return false;
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return false;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return false;
            position = new Vector2(x, y);
            return true;
        }

        internal static string Format(Vector2 position) =>
            string.Format(CultureInfo.InvariantCulture, "{0:0},{1:0}", position.x, position.y);
    }
}
