using System.Threading;
using System.Collections.Generic;




namespace UXF
{
    
    public class VRPN_UXF_PosOri : Tracker
    {
        

        /// <summary>
        /// Rate at which you wish to record position (doesn't currently do anything, must set from VRPN_Update).
        /// </summary>
        public int recordRate = 200;

        /// <summary>
        /// Address of the VRPN tracker you are sampling from (default for PPT is 3883)
        /// </summary>
        public string vrpn_Address = "localhost";

        /// <summary>
        /// Should reflect your desired sample rate in the inspector window.
        /// </summary>
        public int ThreadedUpdatesPerSecond;

        /// <summary>
        /// Indicates the number of samples taken during a particular trial.
        /// </summary>
        public int sampleCount;

        /// <summary>
        /// Channel of VRPN tracker you are sampling from (IR light ID for PPT)
        /// </summary>
        public int vrpn_Channel = 0;

        /// <summary>
        /// Contains the UXFDataRows from the fastUpdate loop.
        /// </summary>
        public UXFDataRow[] trialDataArray;
        /// <summary>
        /// The opened thread that does the sampling.
        /// </summary>
        private Thread collectData;


        protected override void SetupDescriptorAndHeader()
        {
            measurementDescriptor = "movement";
            // VRPN outputs Quaternions, not euler angles. Time should be measured outside of the Unity API, by using ticks.
            customHeader = new string[]
            {
                "pos_x",
                "pos_y",
                "pos_z",
                "q1",
                "q2",
                "q3",
                "q4",
                "time_ticks"
            };
        }
        
 
        public void trackPosition()
        {
            // List of UXF Data Rows, with rows added after each sample.
            List<UXFDataRow> dataList = new List<UXFDataRow>();

            // Sets number of ticks for one second, for displaying samples/second.
            const int oneSecond = 10000000;

            // Time in ticks at start of recording.
            var time_2 = System.DateTime.UtcNow.Ticks;

            // Time in ticks for updating samples/second.
            var time_sampleRate = System.DateTime.UtcNow.Ticks;

            // Count the number of samples
            int loopCount = 0;

            // Bool, allowing for repetition and ending of while loop.
            bool trialOngoing = true;

            // Loop while the trial is ongoing- record data if recording, notify that trial has stopped if not recording.
            while(trialOngoing)
            {
            if(Recording)
            {

            // Update samples per second in the inspector.
            if (System.DateTime.UtcNow.Ticks - time_sampleRate >= oneSecond)
            {
                ThreadedUpdatesPerSecond = loopCount;
                loopCount = 0;
                time_sampleRate = System.DateTime.UtcNow.Ticks;
            }
            // Sample data as string arrays
            string[] p = vrpnUpdate.vrpnTrackerPos(vrpn_Address, vrpn_Channel);
            string[] r = vrpnUpdate.vrpnTrackerQuat(vrpn_Address, vrpn_Channel);
            var time = System.DateTime.UtcNow.Ticks - time_2;
            
            //Add sample to list.
            dataList.Add(new UXFDataRow()
            {
                ("pos_x", p[0]),
                ("pos_y", p[1]),
                ("pos_z", p[2]),
                ("q1", r[0]),
                ("q2", r[1]),
                ("q3", r[2]),
                ("q4", r[3]),
                ("time_ticks", time.ToString())
            });
            
            // Sleep for 1000/recording rate (200hz = 5ms)
            Thread.Sleep(1000/recordRate);
            loopCount++;
            sampleCount++;
            } 

            //When recording ends, if data has been collected, report the size and log that the thread has ended.
            else if(!Recording & dataList.Count>0){
            Utilities.UXFDebugLog("Size of dataList:" + dataList.Count.ToString());
            Utilities.UXFDebugLog("Thread finished. Recent position output below");

            // Check that the sampler is, in fact, sampling position.
            Utilities.UXFDebugLog(string.Join(" , ", vrpnUpdate.vrpnTrackerPos(vrpn_Address, vrpn_Channel)));

            // Send dataList clone to a public Array, report size and clear the thread's list.
            trialDataArray = dataList.ToArray();
            Utilities.UXFDebugLog("Size of trialdataList:" + trialDataArray.Length.ToString());
            dataList.Clear();

            // Change bool to break while loop.
            trialOngoing = false;}
            else{
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
            Utilities.UXFDebugLog("Recording Start");
            data = new UXFDataTable(header);
            recording = true;
            if(updateType == TrackerUpdateType.fastUpdate)
            {
                collectData = new Thread(trackPosition);
                collectData.Start();
                Utilities.UXFDebugLog("Thread Started.");
            }
        }
        public override void StopRecording()
        {
            recording = false;
            // Wait for thread to join, to ensure no combined writing of files occurs.
            collectData.Join();

            // Note number of samples taken, compare to length of trialDataList.
            Utilities.UXFDebugLog("Number of samples taken" + sampleCount.ToString());
            Utilities.UXFDebugLog("Size of trialdataList:" + trialDataArray.Length.ToString());

            // For each data row sampled, add to the trial's data. Notify entry into the for loop.
            foreach (UXFDataRow i in trialDataArray){
             if(data.CountRows() < 1){Utilities.UXFDebugLogWarning("Adding rows to data table.");}
             data.AddCompleteRow(i);    
            }
            // Notify end of compilation.
            Utilities.UXFDebugLog("Should be finished adding rows.");
            }
        }
    }