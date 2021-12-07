using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;
using System;

namespace Neos_OpenSeeFace_Integration
{
	public class Neos_OpenSeeFace_Integration : NeosMod
	{
		public override string Name => "Neos-WCFace-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/https://github.com/dfgHiatus/Neos-OpenSeeFace-Integration/";
		public override void OnEngineInit()
		{
			// Harmony.DEBUG = true;
			Harmony harmony = new Harmony("net.dfgHiatus.Neos-WCFace-Integration");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(InputInterface), MethodType.Constructor)]
		[HarmonyPatch(new[] { typeof(Engine)})]
		public class InputInterfaceCtorPatch
		{
			public static void Postfix(InputInterface __instance)
			{
				try
				{
					WCFaceInputDevice wsf = new WCFaceInputDevice();
					Debug("WCFace Module: " + wsf.ToString());
					__instance.RegisterInputDriver(wsf);
				}
				catch
				{
					Warn("WCFace failed to initiallize.");
					throw;
				}
			}
		}
	}

	class WCFaceInputDevice : IInputDriver
	{
		public Eyes eyes;
		//public Mouth mouth;
		public WCFace.MainWCFT wcfTracker = new WCFace.MainWCFT();
		public int UpdateOrder => 100;

		public void CollectDeviceInfos(BaseX.DataTreeList list) // (dmx) do this later ... this should be fine
        {
			DataTreeDictionary EyeDataTreeDictionary = new DataTreeDictionary();
			EyeDataTreeDictionary.Add("Name", "WCFace Eye Tracking");
			EyeDataTreeDictionary.Add("Type", "Eye Tracking");
			EyeDataTreeDictionary.Add("Model", "Webcamera");
			list.Add(EyeDataTreeDictionary);

			//DataTreeDictionary MouthDataTreeDictionary = new DataTreeDictionary();
			//MouthDataTreeDictionary.Add("Name", "WCFace Face Tracking");
			//MouthDataTreeDictionary.Add("Type", "Face Tracking");
			//MouthDataTreeDictionary.Add("Model", "Webcamera");
			//list.Add(MouthDataTreeDictionary);
		}

		public void RegisterInputs(InputInterface inputInterface)
		{
			wcfTracker.Initialize();
			wcfTracker.StartThread();
			eyes = new Eyes(inputInterface, "OpenSeeFace Eye Tracking");
			// mouth = new Mouth(inputInterface, "OpenSeeFace Mouth Tracking");
		}

		public void UpdateInputs(float deltaTime)
        {
			/* TODO: Eye tracking "freezes" currently. This means the engine respects an eye tracker,
			 * But I'm guessing it hangs as things like Pupil Dilation are not accounted for.
			 * Stream Dummy Information!?
			 */

			// This should be active at all times
			// eyes.IsEyeTrackingActive = wcfTracker.lastWCFTData.IsFaceTracking;

			// This is gonna break things...
			wcfTracker.Update();

			eyes.LeftEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			eyes.RightEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			eyes.CombinedEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;

			eyes.LeftEye.Squeeze = wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.LeftEyebrowUpDown);
			eyes.RightEye.Squeeze = wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.RightEyebrowUpDown);
			eyes.RightEye.Squeeze = MathX.Average(wcfTracker.lastWCFTData.LeftEyebrowUpDown,
												  wcfTracker.lastWCFTData.RightEyebrowUpDown);

			eyes.LeftEye.Widen = wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.LeftEyebrowUpDown);
			eyes.RightEye.Widen = wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.RightEyebrowUpDown);
			eyes.CombinedEye.Widen = MathX.Average(wcfTracker.lastWCFTData.LeftEyebrowUpDown,
												   wcfTracker.lastWCFTData.RightEyebrowUpDown);

			eyes.LeftEye.Openness = wcfTracker.lastWCFTData.LeftEyeBlink;
			eyes.RightEye.Openness = wcfTracker.lastWCFTData.RightEyeBlink;
			eyes.CombinedEye.Openness = MathX.Average(wcfTracker.lastWCFTData.LeftEyeBlink, 
													  wcfTracker.lastWCFTData.RightEyeBlink);

			// For now while we dummy test eyes

			// This should be active at all times
			// mouth.IsDeviceActive = wcfTracker.lastWCFTData.IsFaceTracking;
			// mouth.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			// mouth.JawOpen = wcfTracker.lastWCFTData.MouthOpen;
			// mouth.MouthLeftSmileFrown = MathX.Clamp01(wcfTracker.lastWCFTData.MouthWide);
			// mouth.MouthRightSmileFrown = MathX.Clamp01(wcfTracker.lastWCFTData.MouthWide);

		}
	}
}