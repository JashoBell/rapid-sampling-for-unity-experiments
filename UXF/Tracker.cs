using System.Threading;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Unity.Jobs;

namespace UXF
{
    /// <summary>
    /// Create a new class that inherits from this component to create custom tracking behaviour on a frame-by-frame basis.
    /// </summary>
    public abstract class Tracker : MonoBehaviour
    {
        /// <summary>
        /// Name of the object used in saving
        /// </summary>
        public string objectName;

        /// <summary>
        /// Description of the type of measurement this tracker will perform.
        /// </summary>
        [Tooltip("Description of the type of measurement this tracker will perform.")]
        public string measurementDescriptor;

        /// <summary>
        /// Custom column headers for tracked objects. Time is added automatically
        /// </summary>
        [Tooltip("Custom column headers for each measurement.")]
        public string[] customHeader = new string[] { };
   
        /// <summary>
        /// A name used when saving the data from this tracker.
        /// </summary>
        public string dataName
        {
            get
            {
                Debug.AssertFormat(measurementDescriptor.Length > 0, "No measurement descriptor has been specified for this Tracker!");
                return string.Join("_", new string[]{ objectName, measurementDescriptor });
            }
        }

        public bool recording;

        public bool Recording { get { return recording; } }

        public UXFDataTable data { get; set; } = new UXFDataTable();
        

        /// <summary>
        /// The header that will go at the top of the output file associated with this tracker. fastUpdate requires a conditional, because time is taken with a different variable.
        /// </summary>
        /// <returns></returns>
        public string[] header
        { 
            get
            {
                if(updateType != TrackerUpdateType.fastUpdate){
                var newHeader = new string[customHeader.Length + 1];
                newHeader[0] = "time";
                customHeader.CopyTo(newHeader, 1);
                return newHeader;}
                return customHeader;
            }
        }

        /// <summary>
        /// When the tracker should take measurements.
        /// </summary>
        [Tooltip("When the measurements should be taken.\n\nManual should only be selected if the user is calling the RecordRow method either from another script or a custom Tracker class.")]
        public TrackerUpdateType updateType = TrackerUpdateType.LateUpdate;
        public TrackerType trackerType = TrackerType.Other;

        // called when component is added
        void Reset()
        {
            objectName = gameObject.name.Replace(" ", "_").ToLower();
            SetupDescriptorAndHeader();
        }

        // called by unity just before rendering the frame
        void LateUpdate()
        {
            if (recording && updateType == TrackerUpdateType.LateUpdate) RecordRow();
        }

        // called by unity when physics simulations are run
        void FixedUpdate()
        {
            if (recording && updateType == TrackerUpdateType.FixedUpdate) RecordRow();
        }


        /// <summary>
        /// Records a new row of data at current time.
        /// </summary>
        public void RecordRow()
        {
            if (!recording) throw new System.InvalidOperationException("Tracker measurements cannot be taken when not in a trial!");
            
            if(updateType != TrackerUpdateType.fastUpdate){
                 UXFDataRow newRow = GetCurrentValues();
                 newRow.Add(("time", Time.unscaledTime));
                 data.AddCompleteRow(newRow);}
        }

        /// <summary>
        /// Begins recording.
        /// </summary>
        public virtual void StartRecording()
        {
            data = new UXFDataTable(header);
            recording = true;
            print("Normal Tracker recording.");
        }

        /// <summary>
        /// Stops recording.
        /// </summary>
        public virtual void StopRecording()
        {
            recording = false;
        }

        /// <summary>
        /// Acquire values for this frame and store them in an UXFDataRow. Must return values for ALL columns.
        /// </summary>
        /// <returns></returns>
        protected abstract UXFDataRow GetCurrentValues();


        /// <summary>
        /// Override this method and define your own descriptor and header.
        /// </summary>
        protected abstract void SetupDescriptorAndHeader();

    }

    /// <summary>
    /// When the tracker should collect new measurements. Manual should only be selected if the user is calling the RecordRow method either from another script or a custom Tracker class.
    /// </summary>
    public enum TrackerUpdateType
    {
        LateUpdate, FixedUpdate, Manual, fastUpdate, guiUpdate
    }

        public enum TrackerType
    {
        Positional, Eye, Other
    }
}