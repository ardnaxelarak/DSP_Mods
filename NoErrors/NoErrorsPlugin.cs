using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace DSP_NoErrors {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class NoErrorsPlugin : BaseUnityPlugin {
        private const string PluginGuid = "com.ardnaxelarak.dsp.NoErrors";
        private const string PluginName = "NoErrors";
        private const string PluginVersion = "0.0.1";

        internal static bool _initialized = false;

        internal void Awake() {
            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(NoErrorsPlugin));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnOpen")]
        public static void UIFatalErrorTip__OnOpen_Postfix(UIFatalErrorTip __instance) {
            __instance._Close();
        }
    }
}