using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;

namespace NeosPimaxIntegration
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
		public Mouth mouth;
		public WCFace.MainWCFT wcfTracker = new WCFace.MainWCFT();
		public int UpdateOrder => 100;

		public void CollectDeviceInfos(BaseX.DataTreeList list) // (dmx) do this later ... this should be fine
        {
			DataTreeDictionary dataTreeDictionary = new DataTreeDictionary();
			dataTreeDictionary.Add("Name", "WCFace Eye and Face Tracking");
			dataTreeDictionary.Add("Type", "Eye and Face Tracking");
			dataTreeDictionary.Add("Model", "Webcamera");
			list.Add(dataTreeDictionary);
		}

		public void RegisterInputs(InputInterface inputInterface)
		{
			if (!wcfTracker.didLoad)
			{
				wcfTracker.Initialize();
			}

			eyes = new Eyes(inputInterface, "OpenSeeFace Eye Tracking");
		}

		public void UpdateInputs(float deltaTime)
        {
			// eyes.IsEyeTrackingActive = wcfTracker.lastWCFTData.IsFaceTracking;
			eyes.LeftEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			eyes.RightEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			eyes.CombinedEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;

			// TODO Remap
			eyes.LeftEye.Squeeze = wcfTracker.lastWCFTData.LeftEyebrowUpDown;
			eyes.RightEye.Squeeze = wcfTracker.lastWCFTData.RightEyebrowUpDown;
			eyes.RightEye.Squeeze = MathX.Average(wcfTracker.lastWCFTData.LeftEyebrowUpDown,
												  wcfTracker.lastWCFTData.RightEyebrowUpDown);

			eyes.LeftEye.Widen = wcfTracker.lastWCFTData.LeftEyebrowUpDown;
			eyes.RightEye.Widen = wcfTracker.lastWCFTData.RightEyebrowUpDown;
			eyes.CombinedEye.Widen = MathX.Average(wcfTracker.lastWCFTData.LeftEyebrowUpDown,
												   wcfTracker.lastWCFTData.RightEyebrowUpDown);

			eyes.LeftEye.Openness = wcfTracker.lastWCFTData.LeftEyeBlink;
			eyes.RightEye.Openness = wcfTracker.lastWCFTData.RightEyeBlink;
			eyes.CombinedEye.Openness = MathX.Average(wcfTracker.lastWCFTData.LeftEyeBlink, 
													  wcfTracker.lastWCFTData.RightEyeBlink);

			// TODO Remap
			// mouth.IsDeviceActive = wcfTracker.lastWCFTData.IsFaceTracking;
			mouth.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
			mouth.JawOpen = wcfTracker.lastWCFTData.MouthOpen;
			mouth.MouthLeftSmileFrown = wcfTracker.lastWCFTData.MouthWide;
			mouth.MouthRightSmileFrown = wcfTracker.lastWCFTData.MouthWide;

		}

	}
}