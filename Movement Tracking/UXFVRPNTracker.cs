using System;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UXFTrackingUtilities;


namespace UXF
{
    /// <summary>
	/// Records the position of a VRPN-based positional tracker at 200hz using a separate thread, and saves it via the Unity Experiment Framework.
	/// </summary>
    public class UXFVRPNTracker : Tracker
    {
        

        /// <summary>
        /// Rate at which you wish to record position.
        /// </summary>
        public int recordRate = 200;

        /// <summary>
        /// Name of VRPN tracker you are sampling from (default for Precision Point Tracking is PPT0)
        /// </summary>
        public string trackerName = "PPT0";

        /// <summary>
        /// Address of the VRPN tracker you are sampling from (default for Precision Point Tracking is 3883)
        /// </summary>
        [FormerlySerializedAs("vrpn_Address")] public string vrpnAddress = "localhost";
        private string _address;

        /// <summary>
        /// Should reflect your desired sample rate in the inspector window.
        /// </summary>
        public int threadedUpdatesPerSecond;

        /// <summary>
        /// Indicates the number of samples taken during a particular trial.
        /// </summary>
        public int sampleCount;
        public int[] perTrackerSamples;
        public int[] numRepeats;

        /// <summary>
        /// Zero-indexed Sensor ID of VRPN tracker you are sampling from (IR light ID - 1 for PPT)
        /// </summary>
        [FormerlySerializedAs("vrpn_Channel")] public int[] vrpnChannel;
        public string[] tracked;
        /// <summary>
        /// Zero-indexed Sensor ID of VRPN trackers you want to calculate aperture (distance) for
        /// </summary>
        public int[] apertureIndex;

        /// <summary>
        /// The UXF session that this tracker is sampling for.
        /// </summary>
        public Session session;
        /// <summary>
        /// Contains the UXFDataRows from the fastUpdate loop.
        /// </summary>
        private UXFDataRow[] _trialDataArray;
        public List<UXFDataRow[]> trialDataArrayList;

        /// <summary>
        /// Contains the focused objects from each trial, to be joined on the back end.
        /// </summary>
        private UXFDataRow[] _focusArray;
        public List<UXFDataRow[]> focusArrayList;

        [Tooltip("Thread object that handles collection/saving of data.")]
        private Thread _collectData, _saveData;

        /// <summary>
        /// Creates the data name.
        /// </summary>
        /// <value></value>
        private string DataName
        {
            get
            {
                Debug.AssertFormat(measurementDescriptor.Length > 0, "No measurement descriptor has been specified for this Tracker!");
                return string.Join("_", new string[]{session.CurrentBlock.settings.GetString("task"), objectName, measurementDescriptor});
            }
        }
 
        /// <summary>
        /// Combines name and address.
        /// </summary>
        /// <param name="trackerName"></param>
        /// <returns></returns>
        private string GetTrackerAddress(string trackerName)
        {
            _address = "@" + vrpnAddress;
            var fulladdress = trackerName + _address;
            return fulladdress;
        }

        /// <summary>
        /// Establishes header for the collected data.
        /// </summary>
  
        protected override void SetupDescriptorAndHeader()
        {
            measurementDescriptor = "movement";
            customHeader = new string[]
            {
                "participant",
                "block",
                "trial",
                "tracked",
                "pos_x",
                "pos_y",
                "pos_z",
                "aperture",
                "time_ms",
                "phase"
            };

        }
        
         /// <summary>
        /// Sampling of position, to be used in a parallel thread.
        /// </summary>
         private void TrackPosition()
        {
            // List of UXF Data Rows, with rows added after each sample.
            List<List<UXFDataRow>> dataList = new ();
            trialDataArrayList = new List<UXFDataRow[]>();


            for(var i = 0; i < vrpnChannel.Length; i++)
            {
                dataList.Add(new List<UXFDataRow>());
            }
            
            const string format = "F4";

            // Sets number of Millisecond for one second, for displaying samples/second.
            const int oneSecond = 1000;
        

            // Time in Millisecond at start of recording.
            var time2 = System.DateTime.UtcNow;

            // Time in Millisecond for updating samples/second.
            var timeSampleRate = System.DateTime.UtcNow.Millisecond;

            // Count the number of samples
            var loopCount = 0;

            // Bool, allowing for repetition and ending of while loop.
            var trialOngoing = true;

            //vector3 variables for storing position and calculating velocity
            var previous = new System.Numerics.Vector3[vrpnChannel.Length];
            var current = new System.Numerics.Vector3[vrpnChannel.Length];
            numRepeats = new int[vrpnChannel.Length];
            perTrackerSamples = new int[vrpnChannel.Length];

            var startBlock = Convert.ToInt32(session.participantDetails["startblock"]);
            var block = session.currentBlockNum + (startBlock - 1);

            var startTrial = Convert.ToInt32(session.participantDetails["starttrial"]);
            var trial = session.currentTrialNum;
            if(startTrial > 1 & startBlock == block)
            {
                trial += startTrial-1;
            }

            //Input 0 for each of these values, as they can't be calculated on the first round.
            for(var i = 0; i < vrpnChannel.Length; i++)
            {
                previous[i] = new System.Numerics.Vector3(0, 0, 0);
                numRepeats[i] = 0;
                perTrackerSamples[i] = 0;
            }


            // Loop while the trial is ongoing- record data if recording, notify that trial has stopped if not recording.
            while(trialOngoing)
            {
                if(Recording)
                {

                    // Update samples per second in the inspector.
                    if (System.DateTime.UtcNow.Millisecond - timeSampleRate >= oneSecond)
                    {
                        threadedUpdatesPerSecond = loopCount;
                        loopCount = 0;
                        timeSampleRate = System.DateTime.UtcNow.Millisecond;
                    }


                    // Sample data
                    var time = System.DateTime.UtcNow - time2;
                    var n = 0;
                    var trackerPosition = new System.Numerics.Vector3[vrpnChannel.Length];


                    foreach(var i in vrpnChannel)
                    {
                        var p = VRPNUpdate.VrpnTrackerPos(GetTrackerAddress(trackerName), i);
                        trackerPosition[n] = new System.Numerics.Vector3(p[0], p[1], p[2]);

                        n++;
                    }



                    //Calculate aperture
                    float aperture;
                    try
                    {
                        aperture = System.Numerics.Vector3.Distance(trackerPosition[Array.IndexOf(vrpnChannel, apertureIndex[0])], 
                                                                    trackerPosition[Array.IndexOf(vrpnChannel, apertureIndex[1])]);
                    } 
                    catch(NullReferenceException e)
                    {
                        aperture = 0;
                        UXF.Utilities.UXFDebugLog(e.Message);
                    } 
                    catch(IndexOutOfRangeException e)
                    {
                        aperture = 0;
                        UXF.Utilities.UXFDebugLog(e.Message);
                    }


                    n = 0;
                    foreach(var t in dataList)
                    {
                      float tempAperture;
                      //If the VRPN channel is not specified as one used to calculate the aperture, impute 0. Otherwise,
                      //impute the calculated aperture.
                      if(vrpnChannel[n]!= apertureIndex[0] & vrpnChannel[n] != apertureIndex[1])
                      {
                          tempAperture = 0;
                      }
                      else
                      {
                          tempAperture = aperture;
                      }
                      //Only record samples that differ from the previous frame.
                      if(sampleCount == 0 || (current[n] != previous[n]))
                      {
                        t.Add(new UXFDataRow()
                        {
                            ("participant", session.ppid),
                            ("block", block.ToString()),
                            ("trial", trial.ToString()),
                            ("tracked", tracked[n]),
                            ("pos_x", trackerPosition[n].X.ToString(format)),
                            ("pos_y", trackerPosition[n].Y.ToString(format)),
                            ("pos_z", trackerPosition[n].Z.ToString(format)),
                            ("aperture", tempAperture.ToString(format)),
                            ("time_ms", time.TotalMilliseconds.ToString()),
                            ("phase", session.CurrentTrial.settings.GetString("phase"))
                        });
                        perTrackerSamples[n] ++;
                      }
                      else
                      {
                        numRepeats[n] ++;
                      }
                      n++;
                    }

                    trackerPosition.CopyTo(previous, 0);

                    // Wait for 1000/recording rate ms (200hz = 5ms)
                    Thread.Sleep(1000/recordRate);
                    loopCount++;
                    sampleCount++;
                }

                //When recording ends, if data has been collected, report the size and log that the thread has ended.
                else if(!Recording && dataList.Count > 0)
                {
                    sampleCount = 0;

                    // Send dataList clone to a public Array, report size and clear the thread's list.
                    var n = 0;
                    foreach(var t in dataList)
                    {
                        trialDataArrayList.Add(t.ToArray());
                        Utilities.UXFDebugLog(tracked[n] + " tracker took " + t.Count.ToString() + " samples with " + numRepeats[n].ToString() + "repeated samples.");
                        n++;
                    }

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
            // Replaces top-level StartRecording().
            Utilities.UXFDebugLog("Recording Start. Connecting to " + GetTrackerAddress(trackerName) + " on channel " + vrpnChannel.ToString());

            // Prepare a data table with column names to save. Having it here ensures it is updated when the columns change.
            SetupDescriptorAndHeader();
            data = new UXFDataTable(header);

            recording = true;
            if (!(updateType == TrackerUpdateType.fastUpdate & vrpnChannel.Length > 0)) return;
            _collectData = new Thread(TrackPosition);
            _collectData.Start();
            Utilities.UXFDebugLog("Thread Started.");
        }
        public override void StopRecording()
        {
            recording = false;
            // Wait for thread to join, to ensure no combined writing of files occurs.
            try
            {
                _collectData.Join();
            }
            catch(NullReferenceException e)
            {
                UXF.Utilities.UXFDebugLog("UXF VRPN Tracker: No thread to Join" + e);
            }
            _saveData = new Thread(SaveMovementData);
            _saveData.Start();
        }

        private void SaveMovementData()
        {
            foreach (var i in trialDataArrayList)
            {
                foreach(var r in i)
                {
                    data.AddCompleteRow(r);
                }
            }
            session.CurrentTrial.SaveDataTable(data, DataName, UXFDataType.Trackers);
        }

        // Abort threads when no longer needed, otherwise Unity seems to keep them open.

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