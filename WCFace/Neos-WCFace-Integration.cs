using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;

namespace Neos_WCFace_Integration
{
	public class Neos_WCFace_Integration : NeosMod
	{
		public static WCFace.MainWCFT wcfTracker;

		public override string Name => "Neos-WCFace-Integration";
		public override string Author => "dfgHiatus";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/dfgHiatus/Neos-WCFace-Integration/";
		public override void OnEngineInit()
		{
			// Harmony.DEBUG = true;
			wcfTracker = new WCFace.MainWCFT();
			wcfTracker.Initialize();
			wcfTracker.StartThread();
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
					WCFaceInputDevice wsfFace = new WCFaceInputDevice();
					Debug("WCFace Module: " + wsfFace.ToString());
					__instance.RegisterInputDriver(wsfFace);
				}
				catch
				{
					Warn("WCFace failed to initiallize.");
				}
			}
		}

		[HarmonyPatch(typeof(Engine), "Shutdown")]
		public class ShutdownPatch
		{
			public static bool Prefix()
			{
				wcfTracker.Teardown();
				return true;
			}
		}

		public class WCFaceInputDevice : IInputDriver
		{
			public Eyes eyes;
			public Mouth mouth;
			public int UpdateOrder => 100;
			public float timeStamp = 0f;

			// Both of these will need tweaking depending on user eye swing
			public float Alpha = 2f;
			public float Beta = 2f;

			public void CollectDeviceInfos(BaseX.DataTreeList list)
			{
				DataTreeDictionary EyeDataTreeDictionary = new DataTreeDictionary();
				EyeDataTreeDictionary.Add("Name", "WCFace Eye Tracking");
				EyeDataTreeDictionary.Add("Type", "Eye Tracking");
				EyeDataTreeDictionary.Add("Model", "Webcamera-1");
				list.Add(EyeDataTreeDictionary);

				DataTreeDictionary MouthDataTreeDictionary = new DataTreeDictionary();
				MouthDataTreeDictionary.Add("Name", "WCFace Face Tracking");
				MouthDataTreeDictionary.Add("Type", "Face Tracking");
				MouthDataTreeDictionary.Add("Model", "Webcamera-2");
				list.Add(MouthDataTreeDictionary);
			}

			public void RegisterInputs(InputInterface inputInterface)
			{
				eyes = new Eyes(inputInterface, "WCFace Eye Tracking");
				mouth = new Mouth(inputInterface, "WCFace Mouth Tracking");
			}

			public void UpdateInputs(float deltaTime)
			{
				eyes.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				eyes.IsEyeTrackingActive = !Engine.Current.InputInterface.VR_Active;

				eyes.LeftEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
				eyes.RightEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
				eyes.CombinedEye.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;

				eyes.LeftEye.Squeeze = wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.LeftEyebrowUpDown);
				eyes.RightEye.Squeeze = wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.RightEyebrowUpDown);
				eyes.RightEye.Squeeze = MathX.Average(wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.LeftEyebrowUpDown),
													  wcfTracker.NegativeToPositive(wcfTracker.lastWCFTData.RightEyebrowUpDown));

				eyes.LeftEye.Widen = wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.LeftEyebrowUpDown);
				eyes.RightEye.Widen = wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.RightEyebrowUpDown);
				eyes.CombinedEye.Widen = MathX.Average(wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.LeftEyebrowUpDown),
													   wcfTracker.ForceAboveNegativeBelowOne(wcfTracker.lastWCFTData.RightEyebrowUpDown));

				eyes.LeftEye.Openness = wcfTracker.lastWCFTData.LeftEyeBlink;
				eyes.RightEye.Openness = wcfTracker.lastWCFTData.RightEyeBlink;
				eyes.CombinedEye.Openness = MathX.Average(wcfTracker.lastWCFTData.LeftEyeBlink,
														  wcfTracker.lastWCFTData.RightEyeBlink);

				// Dummy eye info
				eyes.LeftEye.Frown = 0f;
				eyes.RightEye.Frown = 0f;
				eyes.CombinedEye.Frown = 0f;

				eyes.LeftEye.PupilDiameter = 0f;
				eyes.RightEye.PupilDiameter = 0f;
				eyes.CombinedEye.PupilDiameter = 0f;

				// Direction uses some cheeky plane to sphere projection... Pimax Style!
				eyes.LeftEye.Direction = new float3(MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookUpDown),
														  MathX.Tan(Beta * wcfTracker.lastWCFTData.LookLeftRight),
														  1f).Normalized;
				eyes.RightEye.Direction = new float3(MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookUpDown),
											  MathX.Tan(Beta * wcfTracker.lastWCFTData.LookLeftRight),
											  1f).Normalized;
				eyes.CombinedEye.Direction = new float3(MathX.Average(MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookUpDown), MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookUpDown)),
						MathX.Average(MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookLeftRight), MathX.Tan(Alpha * wcfTracker.lastWCFTData.LookLeftRight)),
						1f).Normalized;

				eyes.LeftEye.RawPosition = float3.Zero;
				eyes.RightEye.RawPosition = float3.Zero;
				eyes.CombinedEye.RawPosition = float3.Zero;

				eyes.LeftEye.RawRotation = floatQ.Identity;
				eyes.RightEye.RawRotation = floatQ.Identity;
				eyes.CombinedEye.RawRotation = floatQ.Identity;

				eyes.Timestamp = timeStamp;
				timeStamp += deltaTime;

				mouth.IsDeviceActive = !Engine.Current.InputInterface.VR_Active;
				mouth.IsTracking = wcfTracker.lastWCFTData.IsFaceTracking;
				mouth.Jaw = float3.Zero;
				mouth.JawOpen = wcfTracker.lastWCFTData.MouthOpen;
				mouth.Tongue = float3.Zero;
				mouth.TongueRoll = 0f;
				mouth.LipUpperLeftRaise = 0f;
				mouth.LipUpperRightRaise = 0f;
				mouth.LipLowerLeftRaise = 0f;
				mouth.LipLowerRightRaise = 0f;
				mouth.LipUpperHorizontal = 0f;
				mouth.LipLowerHorizontal = 0f;
				mouth.MouthLeftSmileFrown = MathX.Clamp01(wcfTracker.lastWCFTData.MouthWide);
				mouth.MouthRightSmileFrown = MathX.Clamp01(wcfTracker.lastWCFTData.MouthWide);
				mouth.MouthPout = 0f;
				mouth.LipTopOverturn = 0f;
				mouth.LipBottomOverturn = 0f;
				mouth.LipTopOverUnder = 0f;
				mouth.LipBottomOverUnder = 0f;
				mouth.CheekLeftPuffSuck = 0f;
				mouth.CheekRightPuffSuck = 0f;

				/*                Debug(string.Format("DeviceActive: {0} isTracking: {1} JawOpen {2} LeftSmileFrown {3} RightSmileFrown {4}",
									mouth.IsDeviceActive,
									mouth.IsTracking,
									mouth.JawOpen,
									mouth.MouthLeftSmileFrown,
									mouth.MouthRightSmileFrown));*/
			}
		}
	}
}