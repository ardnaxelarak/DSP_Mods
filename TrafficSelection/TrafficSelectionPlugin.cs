using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TrafficSelection {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [BepInDependency(NebulaModAPI.API_GUID)]
    public class TrafficSelectionPlugin : BaseUnityPlugin, IModCanSave, IMultiplayerModWithSettings {
        private const string PluginGuid = "com.ardnaxelarak.dsp.TrafficSelection";
        private const string PluginName = "TrafficSelection";
        private const string PluginVersion = "0.0.1";

        internal static bool _initialized = false;
        public static UIFilterWindow _win;

        public string Version => PluginVersion;

        internal void Awake() {
            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(TrafficSelectionPlugin));
            Harmony.CreateAndPatchAll(typeof(StarDistance.Patch));
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
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
                _win = UIFilterWindow.CreateWindow("TrafficSelector", "Remote Filtering");

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
            GalacticTransport galacticTransport = GameMain.data.galacticTransport;

            StationComponent supply = galacticTransport.stationPool[sId];
            StationComponent demand = galacticTransport.stationPool[dId];

            RemoteIdentifier supplyIdent = FilterProcessor.GetIdentifier(supply, supply.storage[sIdx].itemId);
            RemoteIdentifier demandIdent = FilterProcessor.GetIdentifier(demand, demand.storage[dIdx].itemId);

            return FilterProcessor.Instance.GetValue(supplyIdent, demandIdent).allowed;
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
            Debug.Log("IntoOtherSave called");
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost) {
                FilterProcessor.Instance.Clear();
            }
        }

        public bool CheckVersion(string hostVersion, string clientVersion) {
            return hostVersion.Equals(clientVersion);
        }
    }
}
