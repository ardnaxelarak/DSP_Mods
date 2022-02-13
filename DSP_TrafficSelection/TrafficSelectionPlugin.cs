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

        internal void Awake() {
            var harmony = new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(TrafficSelectionPlugin));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "AddRemotePair")]
        public static bool AddRemotePair_Prefix(StationComponent __instance, int sId, int sIdx, int dId, int dIdx) {
            Debug.Log("AddRemotePair: " + sId + " " + sIdx + " " + dId + " " + dIdx);
            return false;
        }
    }
}
