using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.InputSystem;

namespace FishMore
{
    [BepInPlugin("com.yourname.fishmore", "Fish More", "1.0.0")]
    public class FishMore : BaseUnityPlugin
    {
        public static bool Enabled = false;
        public static string Message = "";
        public static float MessageTimer = 0f;

        private void Awake()
        {
            Logger.LogInfo("FishMore Mod Loading");
            new Harmony("com.yourname.fishmore").PatchAll();

            GameObject FishMoreObj = new GameObject("FishMoreTogger");
            FishMoreObj.hideFlags = HideFlags.HideAndDontSave;
            FishMoreObj.AddComponent<FishMoreTogger>();
            DontDestroyOnLoad(FishMoreObj);
        }
    }

    public class FishMoreTogger : MonoBehaviour
    {
        public static int FishNumber = 1;
        public static bool awaitingInput = false;
        private string inputBuffer = "";

        void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f6Key.wasPressedThisFrame)
                {
                    FishMore.Enabled = !FishMore.Enabled;
                    if (FishMore.Enabled)
                    {
                        awaitingInput = true;
                        inputBuffer = "";
                        FishMore.Message = "Enter FishNumber:";
                    }
                    else
                    {
                        FishMore.Message = "FishMore Disabled";
                    }
                    FishMore.MessageTimer = 10f;
                }

                if (FishMore.Enabled && awaitingInput)
                {
                    string digitPressed = null;
                    if (Keyboard.current.digit0Key.wasPressedThisFrame) digitPressed = "0";
                    if (Keyboard.current.digit1Key.wasPressedThisFrame) digitPressed = "1";
                    if (Keyboard.current.digit2Key.wasPressedThisFrame) digitPressed = "2";
                    if (Keyboard.current.digit3Key.wasPressedThisFrame) digitPressed = "3";
                    if (Keyboard.current.digit4Key.wasPressedThisFrame) digitPressed = "4";
                    if (Keyboard.current.digit5Key.wasPressedThisFrame) digitPressed = "5";
                    if (Keyboard.current.digit6Key.wasPressedThisFrame) digitPressed = "6";
                    if (Keyboard.current.digit7Key.wasPressedThisFrame) digitPressed = "7";
                    if (Keyboard.current.digit8Key.wasPressedThisFrame) digitPressed = "8";
                    if (Keyboard.current.digit9Key.wasPressedThisFrame) digitPressed = "9";

                    if (digitPressed != null)
                    {
                        inputBuffer += digitPressed;
                        FishMore.Message = $"Enter FishNumber: {inputBuffer}";
                        FishMore.MessageTimer = 10f;
                    }

                    if (Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        if (int.TryParse(inputBuffer, out int result) && result > 0)
                        {
                            FishNumber = result;
                            FishMore.Message = $"FishMore Enabled (FishNumber: {FishNumber})";
                        }
                        else
                        {
                            FishMore.Message = "Invalid input. Using default.";
                        }

                        awaitingInput = false;
                        FishMore.MessageTimer = 3f;
                    }

                    if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        awaitingInput = false;
                        FishMore.Message = $"FishMore Enabled (Number: {FishNumber})";
                        FishMore.MessageTimer = 3f;
                    }
                }
            }

            if (FishMore.MessageTimer > 0f)
                FishMore.MessageTimer -= Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            if (FishMore.MessageTimer > 0f)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 22;
                style.normal.textColor = FishMore.Enabled ? Color.green : Color.red;
                style.alignment = TextAnchor.UpperCenter;

                GUI.Label(new Rect(UnityEngine.Screen.width / 2 - 200, 120, 400, 60), FishMore.Message, style);
            }
        }
    }

    [HarmonyPatch(typeof(FishingRod), "PullFish")]
    public static class Patch_PullFish
    {
        static bool Prefix(FishingRod __instance)
        {
            if (!FishMore.Enabled)
            {
                Debug.Log("[FishMore] Mod disabled, skipping PullFish.");
                return true;
            }

            var checkBaitMethod = AccessTools.Method(typeof(FishingRod), "CheckBait");
            var fieldInfo = AccessTools.Field(typeof(FishingRod), "playerInventory");
            var playerInventory = fieldInfo.GetValue(__instance);
            PlayerInventory inv = (PlayerInventory)playerInventory;

            for (int i = 0; i < FishMoreTogger.FishNumber; i++)
            {
                ItemSO itemSO = (ItemSO)checkBaitMethod.Invoke(__instance, null);
                if (itemSO != null)
                {
                    inv.GiveItemServerRpc(itemSO.id, itemSO.amount, 1, 0);
                }
            }


            return false;
        }
    }
}
