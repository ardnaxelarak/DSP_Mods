using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using UnityEngine;

namespace DSP_TrafficSelection {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    public class TrafficSelectionPlugin : BaseUnityPlugin {
        private const string PluginGuid = "com.ardnaxelarak.dsp.TrafficSelection";
        private const string PluginName = "TrafficSelection";
        private const string PluginVersion = "0.0.1";

        internal static bool _initialized = false;

        internal void Awake() {
            var harmony = new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(TrafficSelectionPlugin));
        }

        internal static void AddButtonToStationWindow() {
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;

            UIStationStorage[] storageUIs = AccessTools.FieldRefAccess<UIStationWindow, UIStationStorage[]>(stationWindow, "storageUIs");
            for (int i = 0; i < storageUIs.Length; i++) {
                UIStationStorageParasite.MakeUIStationStorageParasite(storageUIs[i]);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix() {
            if (!_initialized) {
                _initialized = true;

                AddButtonToStationWindow();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        public static void UIStationStorage_RefreshValues_Postfix(UIStationStorage __instance) {
            __instance.GetComponent<UIStationStorageParasite>()?.RefreshValues();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "AddRemotePair")]
        public static bool StationComponent_AddRemotePair_Prefix(StationComponent __instance, int sId, int sIdx, int dId, int dIdx) {
            Debug.Log("AddRemotePair: " + sId + " " + sIdx + " " + dId + " " + dIdx);
            return false;
        }
    }
}