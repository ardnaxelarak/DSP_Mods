using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;

namespace TrafficSelection {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    public class TrafficSelectionPlugin : BaseUnityPlugin, IModCanSave {
        private const string PluginGuid = "com.ardnaxelarak.dsp.TrafficSelection";
        private const string PluginName = "TrafficSelection";
        private const string PluginVersion = "0.0.1";

        internal static bool _initialized = false;
        public static UIFilterWindow _win;


        internal void Awake() {
            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(TrafficSelectionPlugin));
            Harmony.CreateAndPatchAll(typeof(StarDistance.Patch));
        }

        internal static void AddButtonToStationWindow() {
            UIStationWindow stationWindow = UIRoot.instance.uiGame.stationWindow;

            UIStationStorageParasite.win = _win;

            UIStationStorage[] storageUIs = AccessTools.FieldRefAccess<UIStationWindow, UIStationStorage[]>(stationWindow, "storageUIs");
            for (int i = 0; i < storageUIs.Length; i++) {
                UIStationStorageParasite.MakeUIStationStorageParasite(storageUIs[i]);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix() {
            if (!_initialized) {
                _initialized = true;
                _win = UIFilterWindow.CreateWindow("TrafficSelector", "STS");

                AddButtonToStationWindow();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnInit")]
        public static void UIGame__OnInit_Postfix() {
            _win._Init(_win.data);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnFree")]
        public static void UIGame__OnFree_Postfix() {
            _win._Free();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnUpdate")]
        public static void UIGame__OnUpdate_Postfix() {
            if (GameMain.isPaused || !GameMain.isRunning) {
                return;
            }
            _win._Update();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStationStorage), "RefreshValues")]
        public static void UIStationStorage_RefreshValues_Postfix(UIStationStorage __instance) {
            __instance.GetComponent<UIStationStorageParasite>()?.RefreshValues();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StationComponent), "AddRemotePair")]
        public static bool StationComponent_AddRemotePair_Prefix(int sId, int sIdx, int dId, int dIdx) {
            Debug.Log("AddRemotePair: " + sId + " " + sIdx + " " + dId + " " + dIdx);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "ShutAllFunctionWindow")]
        public static void UIGame_ShutAllFunctionWindow_Postfix()
        {
            _win.Close();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFInput), "get__cameraZoomIn")]
        public static void VFInput__cameraZoomIn_Postfix(ref float __result) {
            if (_win.isPointEnter) {
                __result = 0f;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(VFInput), "get__cameraZoomOut")]
        public static void VFInput__cameraZoomOut_Postfix(ref float __result) {
            if (_win.isPointEnter) {
                __result = 0f;
            }
        }

        public void Export(BinaryWriter w) {
            FilterProcessor.Instance.WriteSerialization(w);
        }

        public void Import(BinaryReader r) {
            FilterProcessor.Instance.ReadSerialization(r);
        }

        public void IntoOtherSave() {
            FilterProcessor.Instance.Clear();
        }
    }
}
