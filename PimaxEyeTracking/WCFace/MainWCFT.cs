using System;
using System.Collections.Generic;
using System.Threading;
using WebSocketSharp;
using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using BaseX;

namespace WCFace
{
    public class MainWCFT // : ITrackingModule
    {
        private Thread _worker = null;
        private CancellationTokenSource _worker_ct = new CancellationTokenSource();

        private string URIString = "ws://localhost:7010/";
        private WebSocket cws = null;
        public bool didLoad = false;

        public string lastData = String.Empty;
        public WCFTData lastWCFTData = new WCFTData();

        public class WCFTData
        {
            public bool IsFaceTracking = false; // If the Webcam can see the person's face
            public floatQ HeadRotation = new BaseX.floatQ(0, 0, 0, 0); // Head Rotation
            public float3 HeadPosition = new BaseX.float3(); // Head Position, sets to (0, 0, 0)
            public float LeftEyeBlink = 0, RightEyeBlink = 0; // Left-Right
            public float MouthOpen = 0, MouthWide = 0; // MouthOpen is obvious, MouthWide is like smile
            public float LeftEyebrowUpDown = 0, RightEyebrowUpDown = 0; // Left-Right
            public float LookUpDown = 0, LookLeftRight = 0; // Combined (a Fork of NeosWCFaceTrack could change this)
            public float EyebrowSteepness = 0; // Combined (a Fork of NeosWCFaceTrack could change this)

            public static string DebugWCFTData(WCFTData data) => $"IFT:{data.IsFaceTracking}," +
                                                                 $"HR:({data.HeadRotation.x},{data.HeadRotation.y},{data.HeadRotation.z},{data.HeadRotation.w})," +
                                                                 $"HP:({data.HeadPosition.x},{data.HeadPosition.y},{data.HeadPosition.z})," +
                                                                 $"LEB:{data.LeftEyeBlink},REB:{data.RightEyeBlink}," +
                                                                 $"MO:{data.MouthOpen},MW:{data.MouthWide}," +
                                                                 $"LEUD:{data.LeftEyebrowUpDown},REUD:{data.RightEyebrowUpDown}," +
                                                                 $"LUD:{data.LookUpDown},LLR:{data.LookLeftRight}";
        }

        private void VerifyDeadThread()
        {
            if (_worker != null)
            {
                if (_worker.IsAlive)
                    _worker.Abort();
            }
            _worker_ct = new CancellationTokenSource();
            _worker = null;
        }

        private void VerifyClosedSocket()
        {
            if (cws != null)
            {
                if (cws.ReadyState == WebSocketState.Open)
                    cws.Close();
            }
            cws = null;
        }

        // Have to do some sort of Websocket Connection Test
        public void Initialize()
        {
            bool isConnected = false;
            VerifyClosedSocket();
            cws = new WebSocket(URIString);
            cws.Connect();
            isConnected = cws.ReadyState == WebSocketState.Open;
            didLoad = isConnected;
            VerifyClosedSocket();
        }

        public void StartThread()
        {
            VerifyDeadThread();
            _worker = new Thread(() =>
            {
                // IL2CPP.il2cpp_thread_attach(IL2CPP.il2cpp_domain_get());
                // Start the Socket
                VerifyClosedSocket();
                cws = new WebSocket(URIString);
                cws.OnMessage += (sender, args) => lastData = args.Data.ToString();
                cws.Connect();
                Thread.Sleep(2500);
                if (cws.ReadyState == WebSocketState.Open)
                {
                    // Start the loop
                    bool isLoading = false;
                    while (!_worker_ct.IsCancellationRequested)
                    {
                        if (cws.ReadyState == WebSocketState.Open)
                        {
                            isLoading = false;
                            // Send a Ping Message
                            cws.Send("");
                            Update();
                        }
                        else
                        {
                            if (didLoad && !isLoading)
                            {
                                // Socket will randomly force close because it thinks its being DOSsed
                                // We'll just re-open it if it thinks this
                                // (thanks python websockets)
                                isLoading = true;
                                cws = new WebSocket(URIString);
                                cws.OnMessage += (sender, args) => lastData = args.Data.ToString();
                                cws.Connect();
                            }
                        }
                        // Please don't change this to anything lower than 50
                        Thread.Sleep(50);
                    }
                }
                // Close the Socket
                VerifyClosedSocket();
                // The thread will abort on its own
            });
            _worker.Start();
        }

        private static float ForceAboveNegativeBelowOne(float value)
        {
            float fTR = 0;
            if (value > 0)
            {
                if (value > 1)
                    fTR = 1;
                else
                    fTR = value;
            }

            return fTR;
        }

        private static float NegativeToPositive(float value)
        {
            float fTR = 0;
            if (value < 0)
                fTR = value * -1;

            return fTR;
        }

        private Eye MakeEye(string eye, WCFTData data)
        {
            float2 Look = new float2(data.LookLeftRight, data.LookUpDown);
            float Openness = 0, Squeeze = 0, Widen = 0;
            switch (eye.ToLower())
            {
                case "left":
                    Openness = data.LeftEyeBlink;
                    Widen = ForceAboveNegativeBelowOne(data.LeftEyebrowUpDown);
                    Squeeze = NegativeToPositive(data.LeftEyebrowUpDown);
                    break;
                case "right":
                    Openness = data.RightEyeBlink;
                    Widen = ForceAboveNegativeBelowOne(data.RightEyebrowUpDown);
                    Squeeze = NegativeToPositive(data.RightEyebrowUpDown);
                    break;
                case "combined":
                    Openness = (data.LeftEyeBlink + data.RightEyeBlink) / 2;
                    Widen = (ForceAboveNegativeBelowOne(data.LeftEyebrowUpDown) + ForceAboveNegativeBelowOne(data.RightEyebrowUpDown)) / 2;
                    Squeeze = (NegativeToPositive(data.LeftEyebrowUpDown) + NegativeToPositive(data.RightEyebrowUpDown)) / 2;
                    break;
            }

            // Change this to be a different namespace?
            return new Eye()
            {
                Look = Look,
                Openness = Openness,
                Squeeze = Squeeze,
                Widen = Widen
            };
        }

        public void Update()
        {
            // Verify the Socket is Alive
            if (cws.ReadyState == WebSocketState.Open)
            {
                // Create new Data
                WCFTData newWCFTData = new WCFTData();
                string[] splitData = WCFTParser.SplitMessage(lastData);
                // Make sure it's valid data, then begin parsing!
                if (WCFTParser.IsMessageValid(splitData))
                {
                    for (int i = 0; i < splitData.Length; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                newWCFTData.IsFaceTracking = WCFTParser.GetValueFromNeosArray<bool>(splitData[i]);
                                break;
                            case 1:
                                floatQ newRotation = new floatQ(
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0),
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1),
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 2),
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 3)
                                );
                                newWCFTData.HeadRotation = newRotation;
                                break;
                            case 2:
                                float3 newHeadPosition = new float3(
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0),
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1),
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 2)
                                );
                                newWCFTData.HeadPosition = newHeadPosition;
                                break;
                            case 3:
                                newWCFTData.LeftEyeBlink =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0);
                                newWCFTData.RightEyeBlink =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1);
                                break;
                            case 4:
                                newWCFTData.MouthOpen =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0);
                                newWCFTData.MouthWide =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1);
                                break;
                            case 5:
                                newWCFTData.LeftEyebrowUpDown =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0);
                                newWCFTData.RightEyebrowUpDown =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1);
                                break;
                            case 6:
                                newWCFTData.LookUpDown =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0);
                                newWCFTData.LookLeftRight =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 1);
                                break;
                            case 7:
                                newWCFTData.EyebrowSteepness =
                                    (float)WCFTParser.GetValueFromNeosArray<double>(splitData[i], 0);
                                break;
                        }
                    }
                    // Push data
                    lastWCFTData = newWCFTData;

                    // Eye Data
                    MakeEye("left", newWCFTData);
                    MakeEye("right", newWCFTData);
                    MakeEye("combined", newWCFTData);

                    // Lip Data (it has to be faked)
                    newWCFTData.MouthOpen;
                    newWCFTData.MouthWide;
                    newWCFTData.MouthWide;

                }
                else
                    Warning("Invalid NeosWCFT data received! Data: " + lastData);
            }
        }

        // How intuitive! Everything just stops with one line!
        public void Teardown() => _worker_ct.Cancel();
    }
}