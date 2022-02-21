using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

namespace NoZoom {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class NoZoomPlugin : BaseUnityPlugin {
        private const string PluginGuid = "com.ardnaxelarak.dsp.NoZoom";
        private const string PluginName = "NoZoom";
        private const string PluginVersion = "0.0.1";

        private static readonly List<WindowZoomBlocker> windows = new List<WindowZoomBlocker>();
        private static WindowZoomBlocker stationWindow;
        internal static bool _initialized = false;
 
        internal void Awake() {
            Harmony.CreateAndPatchAll(typeof(NoZoomPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix() {
            if (!_initialized) {
                _initialized = true;

                stationWindow = WindowZoomBlocker.MakeWindowZoomBlocker(UIRoot.instance.uiGame.stationWindow.gameObject);
                windows.Add(stationWindow);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStationWindow), "_OnClose")]
        public static void UIStationWindow__OnClose_Postfix() {
            stationWindow.hasPointer = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFInput), "get__cameraZoomIn")]
        public static void VFInput__cameraZoomIn_Postfix(ref float __result) {
            foreach (WindowZoomBlocker window in windows) {
                if (window.hasPointer) {
                    __result = 0f;
                    return;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFInput), "get__cameraZoomOut")]
        public static void VFInput__cameraZoomOut_Postfix(ref float __result) {
            foreach (WindowZoomBlocker window in windows) {
                if (window.hasPointer) {
                    __result = 0f;
                    return;
                }
            }
        }
    }
}
