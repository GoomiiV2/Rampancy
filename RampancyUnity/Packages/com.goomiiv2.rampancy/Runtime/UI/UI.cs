using JetBrains.Annotations;
using UnityEngine;

namespace Rampancy.UI
{
    public static class UI
    {
        public static Color ColorPrimary   = RGBIntToColor(0, 123, 255);
        public static Color ColorSecondary = Color.gray;
        public static Color ColorSuccess   = RGBIntToColor(40, 167, 69);
        public static Color ColorDanger    = Color.red;

        public static bool ButtonSuccess(string   text) => ButtonColor(text, ColorSuccess);
        public static bool ButtonPrimary(string   text) => ButtonColor(text, ColorPrimary);
        public static bool ButtonSecondary(string text) => ButtonColor(text, ColorSecondary);
        public static bool ButtonDanger(string    text) => ButtonColor(text, ColorDanger);
        public static bool ButtonDefault(string    text) => ButtonColor(text, GUI.backgroundColor);

        public static bool ButtonColor(string text, Color color, string tooltip = null)
        {
            var ogColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            var result = GUILayout.Button(text);
            GUI.backgroundColor = ogColor;

            return result;
        }

        public static Color RGBIntToColor(byte r, byte g, byte b)
        {
            var color = new Color(r / 255f, g / 255f, b / 255f);
            return color;
        }
    }
}