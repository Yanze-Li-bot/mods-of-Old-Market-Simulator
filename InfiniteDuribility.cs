using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfiniteDurability
{
    [BepInPlugin("com.yourname.infinitedurability", "Infinite Durability", "1.0.0")]
    public class InfiniteDurabilityPlugin : BaseUnityPlugin
    {
        public static bool IsEnabled = true;
        private static string statusMessage = "";
        private static float messageTimer = 0f;

        private void Awake()
        {
            Logger.LogInfo("✅ Infinite Durability plugin loaded.");

            new Harmony("com.yourname.infinitedurability").PatchAll();

            GameObject togglerObj = new GameObject("DurabilityToggler");
            togglerObj.hideFlags = HideFlags.HideAndDontSave;
            togglerObj.AddComponent<DurabilityToggler>();
            DontDestroyOnLoad(togglerObj);
        }



        public class DurabilityToggler : MonoBehaviour
        {
            void Update()
            {
                if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame)
                {
                    IsEnabled = !IsEnabled;
                    statusMessage = IsEnabled ? "✅ 无限耐久已启用" : "❌ 无限耐久已关闭";
                    messageTimer = 3f;
                    Debug.Log($"[InfiniteDurability] {statusMessage}");
                }

                if (messageTimer > 0f)
                    messageTimer -= Time.unscaledDeltaTime;
            }

            void OnGUI()
            {
                if (messageTimer > 0f)
                {
                    GUIStyle style = new GUIStyle(GUI.skin.label);
                    style.fontSize = 22;
                    style.normal.textColor = IsEnabled ? Color.green : Color.red;
                    style.alignment = TextAnchor.UpperCenter;

                    GUI.Label(new Rect(Screen.width / 2 - 150, 40, 300, 40), statusMessage, style);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "ReduceCurrentItemAmount")]
    public static class Patch_ReduceCurrentItemAmount
    {
        static bool Prefix()
        {
            return !InfiniteDurabilityPlugin.IsEnabled;
        }
    }
}
