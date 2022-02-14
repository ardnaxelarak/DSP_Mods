using HarmonyLib;
using BepInEx;
using System.Reflection;

namespace DequeCraft {
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInProcess("DSPGAME.exe")]
	public class DequeCraft : BaseUnityPlugin {
		private const string PluginGuid = "com.ardnaxelarak.dsp.DequeCraft";
		private const string PluginName = "DequeCraft";
		private const string PluginVersion = "1.0.2";

		internal void Awake() {
			new Harmony(PluginGuid);
			Harmony.CreateAndPatchAll(typeof(DequeCraft));
		}

		private static bool tempflag;
		private static int beforeLen;
		private static bool rightClick;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIReplicatorWindow), "_OnCreate")]
		public static void UIReplicatorWindow__OnCreate_Postfix(UIReplicatorWindow __instance, UIButton ___okButton) {
			___okButton.onRightClick += (whatever) => RightClick(__instance);
		}

		internal static void RightClick(UIReplicatorWindow replicator) {
			rightClick = true;
			MethodInfo buttonClick = replicator.GetType().GetMethod("OnOkButtonClick", BindingFlags.NonPublic | BindingFlags.Instance);
			buttonClick.Invoke(replicator, new object[] { 0, true });
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MechaForge), "AddTask")]
		public static bool MechaForge_AddTask_Prefix(MechaForge __instance, int recipeId, int count) {
			if (!__instance.gameHistory.RecipeUnlocked(recipeId)) {
				rightClick = false;
				return false;
			}
			tempflag = __instance.TryAddTask(recipeId, count);
			if (tempflag) {
				beforeLen = __instance.tasks.Count;
				return true;
			}
			rightClick = false;
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MechaForge), "AddTask")]
		public static void MechaForge_AddTask_Postfix(MechaForge __instance) {
			if (tempflag) {
				if (rightClick) {
					rightClick = false;
					return;
				}

				if (beforeLen == 0)
					return;

				int curLen = __instance.tasks.Count;
				int gap = curLen - beforeLen;

				if (gap == 0)
					return;

				for (int i = 0; i < beforeLen; i++) {
					if (__instance.tasks[i].parentTaskIndex != -1) {
						__instance.tasks[i].parentTaskIndex += gap;
					}
				}

				for (int i = beforeLen; i < curLen; i++) {
					ForgeTask t = __instance.tasks[curLen - 1];
					if (t.parentTaskIndex != -1) {
						t.parentTaskIndex -= beforeLen;
					}
					__instance.tasks.RemoveAt(curLen - 1);
					__instance.tasks.Insert(0, t);
				}
			}
		}
	}
}