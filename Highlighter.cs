using MelonLoader;
using UnityEngine;
using System.Collections.Generic;

namespace Colorblind_Holds
{
    public class Highlighter : MelonMod
    {
        private readonly List<string> targetTags = new List<string>
        {
            "Climbable",
            "ClimbableMicroHold",
            "ClimbableRigidbody",
            "Crack",
            "ClimbablePitch",
            "PinchHold",
            "Volume"
        };

        private Dictionary<string, Color> colorblindModes = new Dictionary<string, Color>
        {
            { "Protanopia", new Color(1f, 0.5f, 0.5f) },
            { "Deuteranopia", new Color(0.5f, 1f, 0.5f) },
            { "Tritanopia", new Color(0.5f, 0.5f, 1f) }
        };

        private bool isMenuVisible = false;
        private bool isModActive = true;
        private string selectedMode = null;
        private bool useCustomColor = false;
        private Color customColor = Color.white;

        private Dictionary<GameObject, Renderer> cachedRenderers = new Dictionary<GameObject, Renderer>();

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);
            CacheRenderers();

            if (isModActive)
            {
                HighlightAllObjects();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (Input.GetKeyDown(KeyCode.Insert))
            {
                isMenuVisible = !isMenuVisible;
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (!isMenuVisible)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 325), GUI.skin.box);

            isModActive = GUILayout.Toggle(isModActive, "Mod Active");

            GUILayout.Label("Modes");

            foreach (var mode in colorblindModes.Keys)
            {
                bool isSelected = selectedMode == mode;
                bool toggle = GUILayout.Toggle(isSelected, mode);
                if (toggle && selectedMode != mode)
                {
                    selectedMode = mode;
                    useCustomColor = false;
                }
                else if (!toggle && isSelected)
                {
                    selectedMode = null;
                }
            }

            useCustomColor = GUILayout.Toggle(useCustomColor, "Use Custom Color");
            if (useCustomColor)
            {
                selectedMode = null;

                GUILayout.Label("Custom Color Picker");
                customColor.r = GUILayout.HorizontalSlider(customColor.r, 0f, 1f);
                GUILayout.Label($"Red: {customColor.r:F2}");
                customColor.g = GUILayout.HorizontalSlider(customColor.g, 0f, 1f);
                GUILayout.Label($"Green: {customColor.g:F2}");
                customColor.b = GUILayout.HorizontalSlider(customColor.b, 0f, 1f);
                GUILayout.Label($"Blue: {customColor.b:F2}");
            }

            if (GUILayout.Button("Apply Changes"))
            {
                UpdateObjectColors();
            }

            GUILayout.EndArea();
        }

        private void CacheRenderers()
        {
            cachedRenderers.Clear();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (var obj in allObjects)
            {
                if (targetTags.Contains(obj.tag))
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        cachedRenderers[obj] = renderer;
                    }
                }
            }
        }

        private void HighlightAllObjects()
        {
            foreach (var obj in cachedRenderers.Keys)
            {
                HighlightObject(obj);
            }
        }

        private void HighlightObject(GameObject obj)
        {
            if (!isModActive)
                return;

            if (cachedRenderers.TryGetValue(obj, out Renderer renderer))
            {
                Color color = customColor;

                if (selectedMode != null && colorblindModes.ContainsKey(selectedMode))
                {
                    color = colorblindModes[selectedMode];
                }

                Material highlightMaterial = new Material(Shader.Find("Standard"))
                {
                    color = color
                };

                renderer.material = highlightMaterial;
            }
        }

        private void UpdateObjectColors()
        {
            if (!isModActive)
                return;

            HighlightAllObjects();
        }
    }
}
