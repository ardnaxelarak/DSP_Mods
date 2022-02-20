using BepInEx;
using HarmonyLib;

namespace PlanetLoader {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class PlanetLoaderPlugin : BaseUnityPlugin {
        private const string PluginGuid = "com.ardnaxelarak.dsp.PlanetLoader";
        private const string PluginName = "PlanetLoader";
        private const string PluginVersion = "1.0.0";
        internal void Awake() {
            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(PlanetLoaderPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameData), "ArrivePlanet")]
        public static void UIGame__OnCreate_Postfix(PlanetData planet) {
            PlanetSimulator simulator = planet.gameObject?.GetComponent<PlanetSimulator>();
            if (simulator != null) {
                simulator.SetLayers();
            }
        }
    }
}