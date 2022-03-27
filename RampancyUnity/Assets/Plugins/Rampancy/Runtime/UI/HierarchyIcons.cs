using Rampancy.Halo3;
using UnityEditor;
using UnityEngine;

namespace Rampancy.UI
{
    [InitializeOnLoad]
    public class HierarchyIcons
    {
        private static Texture2D InstanceIcon;

        static HierarchyIcons()
        {
            LoadIcons();
            EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
        }

        private static void LoadIcons()
        {
            InstanceIcon = InstanceIcon ? InstanceIcon : AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BaseData/Textures/Icons/InstanceIcon.png");
        }

        private static void DrawIcon(int instanceid, Rect selectionrect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceid) as GameObject;

            if (go?.GetComponent<Instance>()) {
                DrawIcon(InstanceIcon,  selectionrect);
            }
        }

        private static void DrawIcon(Texture tex, Rect selectionrect)
        {
            float iconWidth = 15;
            EditorGUIUtility.SetIconSize(new Vector2(iconWidth, iconWidth));
            var padding        = new Vector2(5, 0);
            var iconDrawRect   = new Rect(selectionrect.xMax - (iconWidth + padding.x), selectionrect.yMin, selectionrect.width, selectionrect.height);
            var iconGUIContent = new GUIContent(tex);
            EditorGUI.LabelField(iconDrawRect, iconGUIContent);
            EditorGUIUtility.SetIconSize(Vector2.zero);
        }
    }
}