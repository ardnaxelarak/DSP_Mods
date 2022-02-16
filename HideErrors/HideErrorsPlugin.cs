using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace DSP_NoErrors {
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public class HideErrorsPlugin : BaseUnityPlugin {
        private const string PluginGuid = "com.ardnaxelarak.dsp.HideErrors";
        private const string PluginName = "HideErrors";
        private const string PluginVersion = "1.0.0";

        internal static bool _initialized = false;
        internal static UIFatalErrorTip _uiError;
        internal static UIGame _uiGame;

        public static ConfigEntry<string> allowList;

        internal void Awake() {
            allowList = Config.Bind("Settings", "allowList", "", "Space-separated list of terms to cause errors to be allowed.");

            new Harmony(PluginGuid);
            Harmony.CreateAndPatchAll(typeof(HideErrorsPlugin));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIFatalErrorTip), "_OnCreate")]
        public static void UIFatalErrorTip__OnCreate_Postfix(UIFatalErrorTip __instance) {
            if (_uiGame == null) {
                _uiError = __instance;
            } else if (!_initialized) {
                UIErrorClose.MakeUIErrorClose(_uiGame, __instance);
                _initialized = true;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix(UIGame __instance) {
            if (_uiError == null) {
                _uiGame = __instance;
            } else if (!_initialized) {
                UIErrorClose.MakeUIErrorClose(__instance, _uiError);
                _initialized = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnOpen")]
        public static void UIFatalErrorTip__OnOpen_Postfix(UIFatalErrorTip __instance) {
            string[] allowedTerms = allowList.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string term in allowedTerms) {
                if (__instance.errorLogText.text.Contains(term)) {
                    return;
                }
            }
            __instance._Close();
        }
    }

    public class UIErrorClose : MonoBehaviour {
        public UIFatalErrorTip uiError;

        [SerializeField] public UIButton closeBtn;

        public void CloseError(int _) {
            uiError._Close();
        }

        public static UIErrorClose MakeUIErrorClose(UIGame uiGame, UIFatalErrorTip uiError) {
            GameObject parent = uiError.gameObject;

            UIErrorClose uiClose = parent.AddComponent<UIErrorClose>();
            uiClose.uiError = uiError;

            UIStorageGrid inventoryWindow = uiGame.inventory;

            GameObject template = inventoryWindow.transform.Find("panel-bg/btn-box/close-btn")?.gameObject;
            if (template != null) {
                GameObject go = GameObject.Instantiate(template, parent.transform);
                GameObject.Destroy(go.GetComponent<Image>());

                uiClose.closeBtn = go.GetComponent<UIButton>();
                uiClose.closeBtn.onClick += uiClose.CloseError;
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.pivot = Vector2.one;
                rect.anchorMin = Vector2.one;
                rect.anchorMax = Vector2.one;
                rect.anchoredPosition = new Vector2(0f, -5f);
                go.SetActive(true);
            }

            return uiClose;
        }
    }
}