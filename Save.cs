using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Save
{
    [BepInPlugin("com.yourname.save", "Save", "1.0.0")]
    public class Save : BaseUnityPlugin
    {
        private static string message = "";
        private static float messageTimer = 0f;

        private void Awake()
        {
            Logger.LogInfo("Save mod loaded");

            GameObject obj = new GameObject("SaveTogger");
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<SaveTogger>();
            DontDestroyOnLoad(obj);
        }

        public class SaveTogger : MonoBehaviour
        {
            void Update()
            {
                if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    SaveManager.Instance.SaveGame();
                    message = "Game saved!";
                    messageTimer = 3f;
                    Debug.Log("[Save] saved ");
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
                    style.normal.textColor = Color.green;
                    style.alignment = TextAnchor.UpperCenter;

                    GUI.Label(new Rect(Screen.width / 2 - 150, 40, 300, 40), message, style);
                }
            }
        }
    }
}
