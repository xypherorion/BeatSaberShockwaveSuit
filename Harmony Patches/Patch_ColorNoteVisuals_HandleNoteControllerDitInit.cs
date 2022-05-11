using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using ShockwaveSuit;


namespace ShockwaveSuit.HarmonyPatches {
	//[HarmonyPatch("HandleNoteControllerDidInit")]
	[HarmonyPatch(typeof(ColorNoteVisuals), "HandleNoteControllerDidInit")]
	public class Patch_ColorNoteVisuals_HandleNoteControllerDidInit {
		[HarmonyPostfix]
		static void Postfix(ColorNoteVisuals __instance, NoteControllerBase noteController, ref ColorManager ____colorManager) {
			ModPlugin.Instance.colorMan = ____colorManager;
		}
	}
}

/*
namespace ShockwaveSuit.HarmonyPatches {
	[HarmonyPatch("SendNoteWasCutEvent")]
	[HarmonyPatch(typeof(NoteController), "SendNoteWasCutEvent")]
	public class HookNoteController : MonoBehaviour {
		[HarmonyPostfix]
		static void Postfix(NoteController __instance, in NoteCutInfo ___noteCutInfo, INoteControllerNoteWasCutEvent ___noteControllerNoteWasCutEvent, LazyCopyHashSet<INoteControllerNoteWasCutEvent> ___noteWasCutEvent) {
			ModPlugin.Log("TEST");
			//foreach(INoteControllerNoteWasCutEvent evt in ___noteWasCutEvent.items) {
				//if(___noteCutInfo.cutAngle < 7.5 && ___noteCutInfo.cutAngle > -7.5) {
					ModPlugin.Log($"~~~SHOCKWAVE DEBUG~~~ NOTE CUT | A:{___noteCutInfo.cutAngle} Spd:{___noteCutInfo.saberSpeed}");
					//ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.SHOULDERS, ___noteCutInfo.saberSpeed, Mathf.RoundToInt(___noteCutInfo.swingRatingCounter.afterCutRating * 200));
					ShockwaveManager.Instance?.SendHapticGroup(ShockwaveManager.HapticGroup.FRONT, 1.0f, 100);
				//}
			}
        }
    }
*/
	/*
	[HarmonyPatch("Awake")]
	[HarmonyPatch(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.StartSong))]
	public class HookAudioTimeSyncController : MonoBehaviour {
		
//		static void Postfix(AudioTimeSyncController __instance) {
			//if(ModPlugin.Instance != null) {
//				SharedCoroutineStarter.instance.StartCoroutine(ModPlugin.Instance.SetupControllersForGameScene());
            //}
			/*
#if DEBUG
			ModPlugin.Log("AudioTimeSyncController.StartSong()");
#endif
			ObjectContainer.obj = GameObject.Find("LocalPlayerGameCore");
			ModPlugin.Log($"Found {ObjectContainer.obj.ToString()}");

			ObjectContainer.obj.transform.localEulerAngles = new Vector3(0, -50.0f, 0);

			SharedCoroutineStarter.instance.StartCoroutine(MoveTransform());
			*/
//		}

		/*
		static IEnumerator MoveTransform() {
			float waitSeconds = 0.01f;
			float i = 0;
			while (true) {
				for (i = 0.0f; i <= 10.0f;) {
					ObjectContainer.obj.transform.localEulerAngles = Vector3.Lerp(new Vector3(0.0f, -50.0f, 0.0f), new Vector3(0, 10.0f, 0), i);
					i = i + 0.001f;

					yield return new WaitForSecondsRealtime(waitSeconds);
				}
				//yield return new WaitForSeconds(15);
				i = 0;
				for (i = 0.0f; i <= 10.0f;i += 0.001f) {
					ObjectContainer.obj.transform.localEulerAngles = Vector3.Lerp(new Vector3(0.0f, 10.0f, 0.0f), new Vector3(0, -50.0f, 0), i);
					yield return new WaitForSeconds(waitSeconds);
				}
				yield return new WaitForSeconds(15);
			}
		}
		*/
	//}

	/*
	[HarmonyPatch]
	class HookAudioTimeSyncControllerPause {
		static void Postfix() {
#if DEBUG
			ModPlugin.Log("AudioTimeSyncController.Pause()");
#endif
			SharedCoroutineStarter.instance.StopAllCoroutines();
			ObjectContainer.obj.transform.eulerAngles = new Vector3(0, 0, 0);
		}

		[HarmonyTargetMethods]
		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.Method(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.Pause));
		}
	}

	public static class ObjectContainer {
		public static GameObject obj;
    }

	[HarmonyPatch]
	class HookAudioTimeSyncControllerUnpause {
		static void Postfix() {
#if DEBUG
			ModPlugin.Log("AudioTimeSyncController.Resume()");
#endif
			SharedCoroutineStarter.instance.StartCoroutine(MoveTransform());
		}

		static IEnumerator MoveTransform() {
			float waitSeconds = 0.01f;
			float i = 0;
			while (true) {
				for (i = 0.0f; i <= 10.0f; i += 0.001f) {
					ObjectContainer.obj.transform.localEulerAngles = Vector3.Lerp(new Vector3(0.0f, -50.0f, 0.0f), new Vector3(0, 10.0f, 0), i);
					yield return new WaitForSeconds(waitSeconds);
				}
				//yield return new WaitForSeconds(15);
				i = 0;
				for (i = 0.0f; i <= 10.0f; i += 0.001f) {
					ObjectContainer.obj.transform.localEulerAngles = Vector3.Lerp(new Vector3(0.0f, 10.0f, 0.0f), new Vector3(0, -50.0f, 0), i);
					yield return new WaitForSeconds(waitSeconds);
				}
				//yield return new WaitForSeconds(15);
			}
		}

		[HarmonyTargetMethods]
		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.Method(typeof(AudioTimeSyncController), nameof(AudioTimeSyncController.Resume));
		}
	}
	*/
//}
