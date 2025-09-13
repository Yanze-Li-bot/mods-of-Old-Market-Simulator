using UnityEngine;
using HarmonyLib;
using Unity.Netcode;
using BepInEx;
using System.Linq;
using UnityEngine.InputSystem;

namespace AutoRestock
{
    [BepInPlugin("com.yourname.autorestock", "AutoRestock", "1.0.0")]
    public class AutoRestockPlugin : BaseUnityPlugin
    {
        public static bool IsEnabled = true;
        private static string statusMessage = "";
        private static float messageTimer = 0f;

        private void Awake()
        {
            new Harmony("com.yourname.autorestock").PatchAll();

            GameObject obj = new GameObject("AutoRestockManager");
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.AddComponent<AutoRestockManager>();
            DontDestroyOnLoad(obj);

            GameObject togglerObj = new GameObject("AutoRestockToggler");
            togglerObj.hideFlags = HideFlags.HideAndDontSave;
            togglerObj.AddComponent<AutoRestockToggler>();
            DontDestroyOnLoad(togglerObj);
        }

        public class AutoRestockToggler : MonoBehaviour
        {
            void Update()
            {
                if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame)
                {
                    IsEnabled = !IsEnabled;
                    statusMessage = IsEnabled ? "AutoRestock Open" : "AutoRestock Close";
                    messageTimer = 3f;
                    Debug.Log($"[AutoRestock] {statusMessage}");
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

    public class AutoRestockManager : MonoBehaviour
    {
        private float timer = 0f;
        void Update()
        {
            if (!AutoRestockPlugin.IsEnabled)
                return;

            timer += Time.deltaTime;
            if (timer >= 5f)
            {
                TryAssignRestock();
                timer = 0f;
            }
        }

        void TryAssignRestock()
        {
            if (!AutoRestockPlugin.IsEnabled)
                return;

            var restocker = FindObjectsOfType<RestockerController>()
                .FirstOrDefault(r => r.sourceItem == null && r.targetItem == null);

            if (restocker == null)
            {
                return;
            }

            var all = FindObjectsOfType<Item>().ToList();

            var cellarItems = all
                .Where(i => i.transform.position.y < 1f && i.amount.Value > 0)
                .ToList();

            var shelfItems = all
                .Where(i => i.transform.position.y > 1f && i.amount.Value <= 0)
                .ToList();

            foreach (var shelf in shelfItems)
            {
                if (!AutoRestockPlugin.IsEnabled)
                    break;

                var match = cellarItems.FirstOrDefault(i => i.itemSO.id == shelf.itemSO.id);
                if (match != null)
                {
                    restocker.SetItems(shelf, match);
                    restocker.ChangeState(restocker.stateRestockTarget);
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(RestockerController), nameof(RestockerController.TakeSource))]
    public class Patch_TakeSource
    {
        private static readonly AccessTools.FieldRef<RestockerController, NetworkVariable<InventorySlot>> inventorySlotRef =
            AccessTools.FieldRefAccess<RestockerController, NetworkVariable<InventorySlot>>("inventorySlot");

        static bool Prefix(RestockerController __instance)
        {
            if (!AutoRestockPlugin.IsEnabled)
                return true;

            if (__instance.sourceItem == null || __instance.targetItem == null)
            {
                return false;
            }

            var source = __instance.sourceItem;
            var target = __instance.targetItem;


            var slot = inventorySlotRef(__instance);
            slot.Value = new InventorySlot
            {
                itemId = source.itemSO.id,
                amount = source.amount.Value,
                cost = source.cost,
                dayCounter = source.dayCounter.Value
            };

            if (source.NetworkObject != null)
            {
                source.NetworkObject.Despawn(true);
            }

            __instance.RestockTarget();
            return false;
        }
    }

    [HarmonyPatch(typeof(Item), "OnAmountChanged")]
    public class Patch_FishProtect_Amount
    {
        static bool Prefix(Item __instance, int previous, int current)
        {
            if (!AutoRestockPlugin.IsEnabled)
                return true;

            if (!AutoRestockHelper.IsShelfFish(__instance))
                return true;

            if (previous == 1 && current < 1)
            {

                bool success = AutoRestockHelper.DecreaseMatchingCellarFish(__instance);
                if (!success)
                {
                    return true;
                }

                __instance.amount.Value = previous;

                var crate = __instance.GetComponentInChildren<ItemCrate>();
                if (crate != null)
                    crate.EnableDummyItems(previous);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn))]
    public class Patch_FishProtect_Despawn
    {
        static bool Prefix(NetworkObject __instance, bool destroy)
        {
            if (!AutoRestockPlugin.IsEnabled)
                return true;

            var item = __instance.GetComponent<Item>();
            if (!AutoRestockHelper.IsShelfFish(item))
                return true;

            bool hasCellarFish = AutoRestockHelper.HasMatchingCellarFish(item);
            if (!hasCellarFish)
            {
                return true;
            }

            return false;
        }
    }

    public static class AutoRestockHelper
    {
        public static bool IsShelfFish(Item item)
        {
            return item != null &&
                   item.itemSO != null &&
                   item.transform.position.y > 1f &&
                   item.itemSO.name.ToLower().Contains("fish");
        }

        public static bool DecreaseMatchingCellarFish(Item shelfFish)
        {
            var cellarItems = GameObject.FindObjectsOfType<Item>()
                .Where(i =>
                    i != shelfFish &&
                    i.transform.position.y < 1f &&
                    i.itemSO.id == shelfFish.itemSO.id &&
                    i.amount.Value > 0)
                .ToList();

            if (cellarItems.Count == 0)
            {
                return false;
            }

            var source = cellarItems[0];
            source.amount.Value -= 1;
            return true;
        }

        public static bool HasMatchingCellarFish(Item shelfFish)
        {
            return GameObject.FindObjectsOfType<Item>()
                .Any(i =>
                    i != shelfFish &&
                    i.transform.position.y < 1f &&
                    i.itemSO != null &&
                    i.itemSO.id == shelfFish.itemSO.id &&
                    i.amount.Value > 0);
        }
    }
}
