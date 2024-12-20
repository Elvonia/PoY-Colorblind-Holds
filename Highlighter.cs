using UnityEngine;
using System.Collections.Generic;


#if BEPINEX
using BepInEx;
using BepInEx.Configuration;
using UnityEngine.SceneManagement;

#elif MELONLOADER
using MelonLoader;

[assembly: MelonInfo(typeof(Colorblind_Holds.Highlighter), "Colorblind Holds", PluginInfo.PLUGIN_VERSION, "Kalico")]
[assembly: MelonGame("TraipseWare", "Peaks of Yore")]

#endif

namespace Colorblind_Holds
{

#if BEPINEX
    [BepInPlugin("com.github.Elvonia.PoY-Colorblind-Holds", "Colorblind Holds", PluginInfo.PLUGIN_VERSION)]
    public class Highlighter : BaseUnityPlugin

#elif MELONLOADER
    public class Highlighter : MelonMod

#endif
    {
        private readonly Dictionary<string, Color> colorblindModes = new Dictionary<string, Color>
        {
            { "Protanopia", new Color(1f, 0.5f, 0.5f) },
            { "Deuteranopia", new Color(0.5f, 1f, 0.5f) },
            { "Tritanopia", new Color(0.5f, 0.5f, 1f) }
        };

        private Dictionary<GameObject, Renderer> cachedRenderers = new Dictionary<GameObject, Renderer>();

        private Dictionary<string, bool> holdTypeToggles = new Dictionary<string, bool>
        {
            { "Climbable", true },
            { "ClimbableMicroHold", true },
            { "ClimbableRigidbody", true },
            { "Crack", true },
            { "ClimbablePitch", true },
            { "PinchHold", true },
            { "Volume", true }
        };

        private bool isMenuVisible = false;
        private bool isModActive = true;
        private string selectedMode = null;
        private bool useCustomColor = true;
        private Color customColor = Color.white;

        private KeyCode toggleKey = KeyCode.Insert;
        private bool awaitingKeybind = false;
        private bool showHoldSubmenu = true;

#if BEPINEX
        private ConfigEntry<string> savedToggleKey;
        private ConfigEntry<bool> savedModActive;
        private ConfigEntry<string> savedSelectedMode;
        private ConfigEntry<bool> savedUseCustomColor;
        private ConfigEntry<string> savedCustomColor;
        private ConfigEntry<string> savedHoldTypes;

        public void Awake()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;

            savedToggleKey = Config.Bind("General", "ToggleKey", KeyCode.Insert.ToString());
            savedModActive = Config.Bind("General", "ModActive", true);
            savedSelectedMode = Config.Bind("General", "SelectedMode", string.Empty);
            savedUseCustomColor = Config.Bind("General", "UseCustomColor", true);
            savedCustomColor = Config.Bind("General", "CustomColor", ColorUtility.ToHtmlStringRGBA(Color.white));
            savedHoldTypes = Config.Bind("General", "HoldTypes", SerializeHoldTypes());
            LoadPreferences();
        }

        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CommonSceneLoad();
        }

        public void Update()
        {
            CommonUpdate();
        }

        public void OnGUI()
        {
            CommonGUI();
        }

#elif MELONLOADER
        private MelonPreferences_Category preferencesCategory;
        private MelonPreferences_Entry<string> savedToggleKey;
        private MelonPreferences_Entry<bool> savedModActive;
        private MelonPreferences_Entry<string> savedSelectedMode;
        private MelonPreferences_Entry<bool> savedUseCustomColor;
        private MelonPreferences_Entry<string> savedCustomColor;
        private MelonPreferences_Entry<string> savedHoldTypes;

        public override void OnInitializeMelon()
        {
            preferencesCategory = MelonPreferences.CreateCategory("Colorblind_Holds", "Colorblind Holds");
            savedToggleKey = preferencesCategory.CreateEntry("ToggleKey", KeyCode.Insert.ToString());
            savedModActive = preferencesCategory.CreateEntry("ModActive", true);
            savedSelectedMode = preferencesCategory.CreateEntry("SelectedMode", string.Empty);
            savedUseCustomColor = preferencesCategory.CreateEntry("UseCustomColor", true);
            savedCustomColor = preferencesCategory.CreateEntry("CustomColor", ColorUtility.ToHtmlStringRGBA(Color.white));
            savedHoldTypes = preferencesCategory.CreateEntry("HoldTypes", SerializeHoldTypes());
            LoadPreferences();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            base.OnSceneWasInitialized(buildIndex, sceneName);
            CommonSceneLoad();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            CommonUpdate();
        }

        public override void OnGUI()
        {
            base.OnGUI();
            CommonGUI();
        }

#endif
        public void CommonSceneLoad()
        {
            CacheRenderers();
            if (isModActive)
            {
                HighlightAllObjects();
            }
        }

        private void CommonUpdate()
        {
            if (!awaitingKeybind && Input.GetKeyDown(toggleKey))
            {
                isMenuVisible = !isMenuVisible;
            }
            if (awaitingKeybind && Event.current != null && Event.current.isKey)
            {
                toggleKey = Event.current.keyCode;
                awaitingKeybind = false;
                SavePreferences();
            }
        }

        public void CommonGUI()
        {
            if (!isMenuVisible)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 525), GUI.skin.box);

            isModActive = GUILayout.Toggle(isModActive, "Mod Active");

            if (GUILayout.Button("Save Changes"))
            {
                SavePreferences();
                UpdateObjectColors();
            }

            if (awaitingKeybind)
            {
                GUILayout.Label("Press any key...");
            }
            else if (GUILayout.Button($"Menu Toggle Key: {toggleKey}"))
            {
                awaitingKeybind = true;
            }

            GUILayout.Label("Colorblind Modes");
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

            showHoldSubmenu = GUILayout.Toggle(showHoldSubmenu, "Toggle Hold Types");
            if (showHoldSubmenu)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                List<string> holdTypes = new List<string>(holdTypeToggles.Keys);
                foreach (var holdType in holdTypes)
                {
                    holdTypeToggles[holdType] = GUILayout.Toggle(holdTypeToggles[holdType], holdType);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
        }

        private void CacheRenderers()
        {
            cachedRenderers.Clear();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (var obj in allObjects)
            {
                if (holdTypeToggles.ContainsKey(obj.tag) && holdTypeToggles[obj.tag])
                {
                    Renderer renderer = obj.GetComponentInChildren<Renderer>();

                    if (renderer == null)
                    {
                        renderer = obj.GetComponentInParent<Renderer>();
                    }

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

            CacheRenderers();
            HighlightAllObjects();
        }

        private void SavePreferences()
        {
            savedToggleKey.Value = toggleKey.ToString();
            savedModActive.Value = isModActive;
            savedSelectedMode.Value = selectedMode ?? string.Empty;
            savedUseCustomColor.Value = useCustomColor;
            savedCustomColor.Value = ColorUtility.ToHtmlStringRGBA(customColor);
            savedHoldTypes.Value = SerializeHoldTypes();

#if BEPINEX
            Config.Save();
#elif MELONLOADER
            MelonPreferences.Save();
#endif
        }

        private void LoadPreferences()
        {
            toggleKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), savedToggleKey.Value);
            isModActive = savedModActive.Value;
            selectedMode = string.IsNullOrEmpty(savedSelectedMode.Value) ? null : savedSelectedMode.Value;
            useCustomColor = savedUseCustomColor.Value;
            if (ColorUtility.TryParseHtmlString($"#{savedCustomColor.Value}", out Color color))
            {
                customColor = color;
            }
            DeserializeHoldTypes(savedHoldTypes.Value);
        }

        private string SerializeHoldTypes()
        {
            List<string> entries = new List<string>();
            foreach (var pair in holdTypeToggles)
            {
                entries.Add($"{pair.Key}:{pair.Value}");
            }
            return string.Join(",", entries);
        }

        private void DeserializeHoldTypes(string data)
        {
            foreach (var entry in data.Split(','))
            {
                var kv = entry.Split(':');
                if (kv.Length == 2 && holdTypeToggles.ContainsKey(kv[0]))
                {
                    holdTypeToggles[kv[0]] = bool.Parse(kv[1]);
                }
            }
        }
    }
}
