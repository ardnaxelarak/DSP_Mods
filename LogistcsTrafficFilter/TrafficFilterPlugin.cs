using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LogisticsTrafficFilter {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [BepInDependency(NebulaModAPI.API_GUID)]
    public class TrafficFilterPlugin : BaseUnityPlugin, IModCanSave, IMultiplayerModWithSettings {
        private const string PluginGuid = "com.ardnaxelarak.dsp.LogisticsTrafficFilter";
        private const string PluginName = "LogisticsTrafficFilter";
        private const string PluginVersion = "1.0.0";

        internal static bool _initialized = false;
        public static UIFilterWindow _win;

        public string Version => PluginVersion;

        internal void Awake() {
            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(TrafficFilterPlugin));
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
                _win = UIFilterWindow.CreateWindow("LogisticsTrafficFilter", "Traffic Filtering");

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

            StationIdentifier supplyIdent = FilterProcessor.GetIdentifier(supply, supply.storage[sIdx].itemId);
            StationIdentifier demandIdent = FilterProcessor.GetIdentifier(demand, demand.storage[dIdx].itemId);

            FilterValue value = FilterProcessor.Instance.GetValue(supplyIdent, demandIdent);
            return value.allowed;
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
            w.Write('L');
            w.Write('T');
            w.Write('F');
            w.Write(FilterProcessor.saveVersion);
            FilterProcessor.Instance.WriteSerialization(w);
        }

        public void Import(BinaryReader r) {
            try {
                bool flag = true;
                flag &= r.ReadChar() == 'L';
                flag &= r.ReadChar() == 'T';
                flag &= r.ReadChar() == 'F';
                if (flag) {
                    int saveVersion = r.ReadInt32();
                    FilterProcessor.Instance.ReadSerialization(r, saveVersion);
                } else {
                    FilterProcessor.Instance.Clear();
                }
            } catch (IOException) {
                Debug.Log("Error reading LogisticsTrafficFilter save file");
                FilterProcessor.Instance.Clear();
            }
        }

        public void IntoOtherSave() {
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost) {
                FilterProcessor.Instance.Clear();
            }
        }

        public bool CheckVersion(string hostVersion, string clientVersion) {
            return hostVersion.Equals(clientVersion);
        }
    }
}
