using GenneCaster.Base;
using UnityEngine;

namespace GenneCaster.UI
{
    internal static class UiScale
    {
        private const float ReferenceHeight = 1080f;
        private const float MinimumAutoScale = 0.85f;

        internal static float Factor => Mathf.Clamp(Screen.height / ReferenceHeight, MinimumAutoScale, 4f) * CasterConfig.UiScale.Value;
        internal static float Width => Screen.width / Factor;
        internal static float Height => Screen.height / Factor;

        internal static Matrix4x4 Begin()
        {
            Matrix4x4 previous = GUI.matrix;
            float factor = Factor;
            GUI.matrix = Matrix4x4.Scale(new Vector3(factor, factor, 1f));
            return previous;
        }

        internal static Rect ClampToScreen(Rect rect)
        {
            rect.x = Mathf.Clamp(rect.x, 0f, Mathf.Max(0f, Width - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Height - rect.height));
            return rect;
        }
    }

    internal static class DisplayNames
    {
        internal static string Humanize(string name)
        {
            var builder = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || c == '-')
                {
                    builder.Append(' ');
                    continue;
                }
                if (i > 0 && builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    char previous = name[i - 1];
                    bool wordStart = char.IsUpper(c) && char.IsLower(previous);
                    bool digitStart = char.IsDigit(c) && char.IsLetter(previous);
                    if (wordStart || digitStart) builder.Append(' ');
                }
                builder.Append(i == 0 || builder.Length == 0 || builder[builder.Length - 1] == ' ' ? char.ToUpperInvariant(c) : c);
            }
            return builder.ToString().Trim();
        }
    }
}
