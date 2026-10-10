using UnityEngine;

namespace GenneCaster.Classes.UI
{
    public class ColorSpace
    {
        public struct Theme
        {
            public string Name;
            public Color32 Color;
            public Theme(string name, Color32 color)
            {
                Name = name;
                Color = color;
            }
        }
        public static Theme[] Themes = new Theme[]
        {
            new Theme("Red", new Color32(255, 0, 0, 255)),
            new Theme("Dark Red", new Color32(180, 0, 0, 255)),
            new Theme("Wine Red", new Color32(123, 0, 0, 255)),
            new Theme("Maroon", new Color32(128, 0, 0, 255)),
            new Theme("Indian Red", new Color32(205, 92, 92, 255)),
            new Theme("Crimson", new Color32(220, 20, 60, 255)),
            new Theme("Fire Brick", new Color32(178, 34, 34, 255)),
            new Theme("Salmon", new Color32(250, 128, 114, 255)),
            new Theme("Light Salmon", new Color32(255, 160, 122, 255)),
            new Theme("Dark Salmon", new Color32(233, 150, 122, 255)),
            new Theme("Tomato", new Color32(255, 99, 71, 255)),
            new Theme("Light Coral", new Color32(240, 128, 128, 255)),
            new Theme("Misty Rose", new Color32(255, 228, 225, 255)),

            new Theme("Coral", new Color32(255, 127, 80, 255)),
            new Theme("Orange", new Color32(255, 165, 0, 255)),
            new Theme("Dark Orange", new Color32(255, 140, 0, 255)),
            new Theme("Red Orange", new Color32(255, 69, 0, 255)),
            new Theme("Orange Red", new Color32(255, 69, 0, 255)),
            new Theme("Peach Puff", new Color32(255, 218, 185, 255)),
            new Theme("Peru", new Color32(205, 133, 63, 255)),
            new Theme("Dark Goldenrod", new Color32(184, 134, 11, 255)),
            new Theme("Sandy Brown", new Color32(244, 164, 96, 255)),
            new Theme("Chocolate", new Color32(210, 105, 30, 255)),

            new Theme("Yellow", new Color32(255, 255, 0, 255)),
            new Theme("Gold", new Color32(255, 215, 0, 255)),
            new Theme("Light Yellow", new Color32(255, 255, 224, 255)),
            new Theme("Lemon Chiffon", new Color32(255, 250, 205, 255)),
            new Theme("Khaki", new Color32(240, 230, 140, 255)),
            new Theme("Pale Goldenrod", new Color32(238, 232, 170, 255)),
            new Theme("Light Goldenrod Yellow", new Color32(250, 250, 210, 255)),

            new Theme("Green", new Color32(0, 255, 0, 255)),
            new Theme("Lime", new Color32(0, 128, 0, 255)),
            new Theme("Dark Green", new Color32(0, 100, 0, 255)),
            new Theme("Olive", new Color32(128, 128, 0, 255)),
            new Theme("Forest Green", new Color32(34, 139, 34, 255)),
            new Theme("Sea Green", new Color32(46, 139, 87, 255)),
            new Theme("Medium Sea Green", new Color32(60, 179, 113, 255)),
            new Theme("Dark Sea Green", new Color32(143, 188, 143, 255)),
            new Theme("Aquamarine", new Color32(127, 255, 212, 255)),
            new Theme("Medium Aquamarine", new Color32(102, 205, 170, 255)),
            new Theme("Mint Cream", new Color32(245, 255, 250, 255)),

            new Theme("Blue", new Color32(0, 0, 255, 255)),
            new Theme("Dark Blue", new Color32(0, 0, 160, 255)),
            new Theme("Navy", new Color32(0, 0, 128, 255)),
            new Theme("Royal Blue", new Color32(65, 105, 225, 255)),
            new Theme("Dodger Blue", new Color32(30, 144, 255, 255)),
            new Theme("Dark Dodger Blue", new Color32(8, 90, 177, 255)),
            new Theme("Deep Sky Blue", new Color32(0, 191, 255, 255)),
            new Theme("Sky Blue", new Color32(135, 206, 235, 255)),
            new Theme("Steel Blue", new Color32(70, 130, 180, 255)),
            new Theme("Cyan", new Color32(0, 255, 255, 255)),

            new Theme("Purple", new Color32(123, 3, 252, 255)),
            new Theme("Indigo", new Color32(75, 0, 130, 255)),
            new Theme("Medium Orchid", new Color32(186, 85, 211, 255)),
            new Theme("Slate Blue", new Color32(106, 90, 205, 255)),
            new Theme("Dark Slate Blue", new Color32(72, 61, 139, 255)),
            new Theme("Lavender", new Color32(230, 230, 250, 255)),
            new Theme("Plum", new Color32(221, 160, 221, 255)),
            new Theme("Magenta", new Color32(255, 0, 255, 255)),

            new Theme("Pink", new Color32(255, 192, 203, 255)),
            new Theme("Hot Pink", new Color32(255, 105, 180, 255)),
            new Theme("Deep Pink", new Color32(255, 20, 147, 255)),

            new Theme("Brown", new Color32(139, 69, 19, 255)),
            new Theme("Saddle Brown", new Color32(139, 69, 19, 255)),
            new Theme("Sienna", new Color32(160, 82, 45, 255)),
            new Theme("Burly Wood", new Color32(222, 184, 135, 255)),
            new Theme("Tan", new Color32(210, 180, 140, 255)),
            new Theme("Rosy Brown", new Color32(188, 143, 143, 255)),

            new Theme("Black", new Color32(0, 0, 0, 255)),
            new Theme("Grey", new Color32(128, 128, 128, 255)),
            new Theme("Light Grey", new Color32(192, 192, 192, 255)),
            new Theme("Dark Grey", new Color32(80, 80, 80, 255)),
            new Theme("Darker Grey", new Color32(40, 40, 40, 255)),
            new Theme("White", new Color32(255, 255, 255, 255)),
            new Theme("Linen", new Color32(250, 240, 230, 255)),
            new Theme("Old Lace", new Color32(253, 245, 230, 255)),
            new Theme("Sea Shell", new Color32(255, 245, 238, 255))
        };
    }
}
