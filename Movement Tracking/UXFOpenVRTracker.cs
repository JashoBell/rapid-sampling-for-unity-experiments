using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Valve.VR;
using BOLL7708;
using UnityEngine;
using UnityEngine.Serialization;


namespace UXF
{
    /// <summary>
	/// Records the position of a SteamVR-based positional tracker at 200hz using a separate thread, and saves it via the Unity Experiment Framework.
	/// </summary>
    public class UXFOpenVRTracker : Tracker
    {


        /// <summary>
        /// Rate at which you wish to record position (doesn't currently do anything, must set from VRPN_Update).
        /// </summary>
        public int recordRate = 240;

        /// <summary>
        /// Name of Vive tracker you are sampling from, taken from the tracked object
        /// </summary>
        public int openVRTrackerIndex;

        /// <summary>
        /// Address of the VRPN tracker you are sampling from (default for PPT is 3883)
        /// </summary>

        /// <summary>
        /// Should reflect your desired sample rate in the inspector window.
        /// </summary>
        [FormerlySerializedAs("ThreadedUpdatesPerSecond")] public int threadedUpdatesPerSecond;

        /// <summary>
        /// Indicates the number of samples taken during a particular trial.
        /// </summary>
        public int sampleCount;
        public int numRepeats;
        private UXFDataRow _lastRow;
        private UXFDataRow _newRow;

        public Session session;
        /// <summary>
        /// Contains the UXFDataRows from the fastUpdate loop.
        /// </summary>
        public UXFDataRow[] TrialDataArray;
        /// <summary>
        /// The opened thread that does the sampling.
        /// </summary>
        private Thread _collectData, _saveData;
        public String serialNumber;
        private int _indexOfTracker;

        private EasyOpenVRSingleton _trackingInstance;
 
         /// <summary>
        /// Find the tracker index which matches the serialNumber variable.
        /// </summary>
        private int GetTrackerIndex()
        {
            ETrackedPropertyError error = new();
            StringBuilder sb = new();
            for (var i = 0; i < SteamVR.connected.Length; ++i)
            {

                OpenVR.System.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_SerialNumber_String, sb, OpenVR.k_unMaxPropertyStringSize, ref error);
                var serialNumber = sb.ToString();
                if (serialNumber == this.serialNumber)
                {
                    UnityEngine.Debug.Log("Assigning device " + i + " to " + gameObject.name + " (" + this.serialNumber +")");
                    _indexOfTracker = i;
                }
                else if (serialNumber != "")
                {
                    print("Serial number " + serialNumber + "found at index " + i);
                }
            }
            
            return _indexOfTracker;
        }

         private string DataName
        {
            get
            {
                Debug.AssertFormat(measurementDescriptor.Length > 0, "No measurement descriptor has been specified for this Tracker!");
                return string.Join("_", new string[]{session.CurrentBlock.settings.GetString("task"), objectName, measurementDescriptor});
            }
        }        


  
        protected override void SetupDescriptorAndHeader()
        {
            measurementDescriptor = "movement_steamvr";
            // Quaternions, not euler angles. Time should be measured outside of the Unity API.
            customHeader = new string[]
            {
                "participant",
                "block",
                "trial",
                "pos_x",
                "pos_y",
                "pos_z",
                "rot_w",
                "rot_x",
                "rot_y",
                "rot_z",
                "velocity",
                "time_ms",
                "phase"
            };
        }
        
        /// <summary>
        /// Sampling of position and orientation, to be used in a parallel thread.
        /// </summary>
        private void TrackPosition()
        {
            // List of UXF Data Rows, with rows added after each sample.
            List<UXFDataRow> dataList = new ();

            // Sets number of ms for one second, for displaying samples/second.
            const int oneSecond = 10000000;
            numRepeats = 0;

            // Time in ms at start of recording.
            var time2 = System.DateTime.UtcNow;

            // Time in ms for updating samples/second.
            var timeSampleRate = System.DateTime.UtcNow.Millisecond;

            // Count the number of samples
            var loopCount = 0;

            // Bool, allowing for repetition and ending of while loop.
            var trialOngoing = true;
            const string format = "F4";

            var startBlock = Convert.ToInt32(session.participantDetails["startblock"]);
            var block = session.currentBlockNum + (startBlock - 1);

            var startTrial = Convert.ToInt32(session.participantDetails["starttrial"]);
            var trial = session.currentTrialNum;
            if(startTrial > 1 && startBlock == block)
            {
                trial += startTrial-1;
            }

            // Loop while the trial is ongoing- record data if recording, notify that trial has stopped if not recording.
            while(trialOngoing)
            {
                if(Recording)
                {
                    // Keep track of time once recording starts.
                    var time = System.DateTime.UtcNow - time2;
                    
                    // Sample data with OpenVR API via EasyOpenVRSingleton convenience functions
                    var trackerPoses = _trackingInstance.GetDeviceToAbsoluteTrackingPose();
                    var trackerPosition = EasyOpenVRSingleton.UnityUtils.MatrixToPosition(trackerPoses[openVRTrackerIndex].mDeviceToAbsoluteTracking);
                    var trackerRotation = EasyOpenVRSingleton.UnityUtils.MatrixToRotation(trackerPoses[openVRTrackerIndex].mDeviceToAbsoluteTracking);
                    var trackerVelocity = trackerPoses[openVRTrackerIndex].vVelocity;
                    var velocity = Math.Abs(trackerVelocity.v0) + Math.Abs(trackerVelocity.v1) + Math.Abs(trackerVelocity.v2);
                    

                    // Update samples per second in the inspector each time one second passes.
                    if (System.DateTime.UtcNow.Millisecond - timeSampleRate >= oneSecond)
                    {
                        threadedUpdatesPerSecond = loopCount;
                        loopCount = 0;
                        timeSampleRate = System.DateTime.UtcNow.Millisecond;
                    }

                    _newRow = new UXFDataRow()
                    {
                        ("pos_x", trackerPosition.v0.ToString(format)),
                        ("pos_y", trackerPosition.v1.ToString(format)),
                        ("pos_z", trackerPosition.v2.ToString(format)),
                        ("rot_w", trackerRotation.w.ToString(format)),
                        ("rot_x", trackerRotation.x.ToString(format)),
                        ("rot_y", trackerRotation.y.ToString(format)),
                        ("rot_z", trackerRotation.z.ToString(format))
                    };

                    if(sampleCount == 0)
                    {
                        dataList.Add(new UXFDataRow()
                            {
                                ("participant", session.ppid),
                                ("block", block.ToString()),
                                ("trial", trial.ToString()),
                                ("pos_x", trackerPosition.v0.ToString(format)),
                                ("pos_y", trackerPosition.v1.ToString(format)),
                                ("pos_z", trackerPosition.v2.ToString(format)),
                                ("rot_w", trackerRotation.w.ToString(format)),
                                ("rot_x", trackerRotation.x.ToString(format)),
                                ("rot_y", trackerRotation.y.ToString(format)),
                                ("rot_z", trackerRotation.z.ToString(format)),
                                ("velocity", velocity.ToString(format)),
                                ("time_ms", time.TotalMilliseconds.ToString(format)),
                                ("phase", session.CurrentTrial.settings.GetString("phase"))
                            });
                    }

                    //Make sure this isn't a repeated measurement.
                    else if(sampleCount >= 1 & _newRow != _lastRow)
                    {
                        //Add sample to list.

                        dataList.Add(new UXFDataRow()
                        {
                            ("participant", session.ppid),
                            ("block", session.currentBlockNum.ToString()),
                            ("trial", session.CurrentTrial.numberInBlock.ToString()),
                            ("pos_x", trackerPosition.v0.ToString(format)),
                            ("pos_y", trackerPosition.v1.ToString(format)),
                            ("pos_z", trackerPosition.v2.ToString(format)),
                            ("rot_w", trackerRotation.w.ToString(format)),
                            ("rot_x", trackerRotation.x.ToString(format)),
                            ("rot_y", trackerRotation.y.ToString(format)),
                            ("rot_z", trackerRotation.z.ToString(format)),
                            ("velocity", velocity.ToString(format)),
                            ("time_ms", time.Milliseconds.ToString()),
                            ("phase", session.CurrentTrial.settings.GetString("phase"))
                        });
                    }

                    else
                    {
                        numRepeats++;
                    }

                    _lastRow = new UXFDataRow()
                    {
                        ("pos_x", trackerPosition.v0.ToString(format)),
                        ("pos_y", trackerPosition.v1.ToString(format)),
                        ("pos_z", trackerPosition.v2.ToString(format)),
                        ("rot_w", trackerRotation.w.ToString(format)),
                        ("rot_x", trackerRotation.x.ToString(format)),
                        ("rot_y", trackerRotation.y.ToString(format)),
                        ("rot_z", trackerRotation.z.ToString(format))
                    };

                    // Iterate counters and sleep for 1000/recording rate (200(hz) = 5ms)
                    loopCount++;
                    sampleCount++;
                    Thread.Sleep(1000/recordRate);
                }

                //When recording ends, if data has been collected, report the size and log that the thread has ended.
                else if(dataList.Count>0)
                {
                    Utilities.UXFDebugLog("Number of repeats: " + numRepeats.ToString());


                    // Send dataList clone to a public Array, report size and clear the thread's list.
                    TrialDataArray = dataList.ToArray();
                    dataList.Clear();

                    // Change bool to break while loop.
                    trialOngoing = false;
                }
                else
                {
                    Utilities.UXFDebugLogWarning("Data from thread contains no rows.");
                    trialOngoing = false;
                }

            }
        }


        protected override UXFDataRow GetCurrentValues()
        {
            // Put here to prevent errors, but this tracker uses something different from GetCurrentValues. Should not run.
            Utilities.UXFDebugLogWarning("GetCurrentValues() is running, but shouldn't be.");
            return new UXFDataRow();

        }
        public override void StartRecording()
        {
            //Only initialize a trackingInstance if it doesn't exist yet.
            try{_trackingInstance.IsInitialized();}
            catch(NullReferenceException err) {
                UXF.Utilities.UXFDebugLog(err + " OpenVR singleton not present. Starting an instance.");
                _trackingInstance = EasyOpenVRSingleton.Instance;
                _trackingInstance.Init();
                openVRTrackerIndex = GetTrackerIndex();
            }
            // Replaces top-level StartRecording(), updating headers and initiating sampling in a separate thread.
            Utilities.UXFDebugLog("Recording Start. Connected to steamVR tracker at index " + openVRTrackerIndex.ToString());
            SetupDescriptorAndHeader();
            data = new UXFDataTable(header);
            recording = true;
            if (updateType != TrackerUpdateType.fastUpdate) return;
            _collectData = new Thread(new ThreadStart(TrackPosition));
            _collectData.Start();
            Utilities.UXFDebugLog("Thread Started.");
        }
        
        public override void StopRecording()
        {
            recording = false;
            // Wait for thread to join, to ensure no combined writing of files occurs.
            _collectData.Join();
            sampleCount = 0;

            //Save movement data in a separate thread.
            _saveData = new Thread(SaveMovementData);
                    _saveData.Start();            
        }

        /// <summary>
        /// Save data from trial.
        /// </summary>
        private void SaveMovementData()
        {
            // For each data row sampled, add to the trial's data.
            foreach (var i in TrialDataArray)
            {
             data.AddCompleteRow(i);    
            }

            session.CurrentTrial.SaveDataTable(data, DataName, UXFDataType.Trackers);
        }


        //Make sure to abort threads if possible.

        private void OnDisable()
        {
            try
            {
                _collectData.Abort();
            }
            catch(NullReferenceException e)
            {
                UXF.Utilities.UXFDebugLog("UXF VRPN Tracker: No thread to abort" + e);
            }
            try
            {
                _saveData.Abort();
            }
            catch(NullReferenceException e)
            {
                UXF.Utilities.UXFDebugLog("UXF VRPN Tracker: No thread to abort" + e);
            }
        }

        private void OnApplicationQuit()
        {
            try
            {
                _collectData.Abort();
            }
            catch(NullReferenceException e)
            {
                UXF.Utilities.UXFDebugLog("UXF VRPN Tracker: No thread to abort" + e);
            }
            try
            {
                _saveData.Abort();
            }
            catch(NullReferenceException e)
            {
                UXF.Utilities.UXFDebugLog("UXF VRPN Tracker: No thread to abort" + e);
            }
        }

        }

    }