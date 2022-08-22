//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: For controlling in-game objects with tracked devices.
// - Edited by jeffcrouse @ github.com/jeffcrouse
// - Added ability to specify a serial number to assign to the TrackedObject
// - I (jashobell @ github.com/jashobell) made minor modifications to fit my specific use-case
//=============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Text;
using UnityEngine.Serialization;


namespace Valve.VR
{
    [System.Serializable]
    public class TimedPose : System.Object
    {
        public HmdMatrix34_t Mat;
        public long time;

        public TimedPose(HmdMatrix34_t mat, long time)
        {
            this.Mat = mat;
            this.time = time; 
        }
    }

    public class SteamVRTrackedObjectPlus : MonoBehaviour
    {
        public enum EIndex
        {
            None = -1,
            Hmd = (int)OpenVR.k_unTrackedDeviceIndex_Hmd,
            Device1,
            Device2,
            Device3,
            Device4,
            Device5,
            Device6,
            Device7,
            Device8,
            Device9,
            Device10,
            Device11,
            Device12,
            Device13,
            Device14,
            Device15,
            Device16
        }

        public EIndex index;

        [Tooltip("If not set, relative to parent")]
        public Transform origin;

        private bool IsValid { get; set; }
        [Tooltip("Whether the tracker has been successfully assigned to an index.")]
        public bool assigned = false;

        private List<TimedPose> _timedPoses = new ();
        [FormerlySerializedAs("DesiredSerialNumber")] [Tooltip("The serial number you wish to be associated with this tracker object")]
        public string desiredSerialNumber = "";
        public int indexOfTracker;

        /// <summary>
        /// Called when SteamVR provides new pose (i.e. position, orientation) data.
        /// </summary>
        /// <param name="poses">An array of poses from the SteamVR devices.</param>
        private void OnNewPoses(TrackedDevicePose_t[] poses)
        {
            if (index == EIndex.None)
                return;

            var i = (int)index;

            IsValid = false;
            if (poses.Length <= i)
                return;

            if (!poses[i].bDeviceIsConnected)
                return;

            if (!poses[i].bPoseIsValid)
                return;

            IsValid = true;

            var pose = new SteamVR_Utils.RigidTransform(poses[i].mDeviceToAbsoluteTracking);
            var originspecified = !ReferenceEquals(origin, null);
            var objtransform = transform;
            if (originspecified)
            {
                objtransform.position = origin.transform.TransformPoint(pose.pos);
                objtransform.rotation = origin.rotation * pose.rot;
            }
            else
            {
                objtransform.localPosition = pose.pos;
                objtransform.localRotation = pose.rot;
            }
        }


        private SteamVR_Events.Action _newPosesAction;

        private SteamVRTrackedObjectPlus()
        {
            _newPosesAction = SteamVR_Events.NewPosesAction(OnNewPoses);
        }

        /// <summary>
        /// Iterates through the list of active SteamVR objects, comparing their SN to the desired one and attaching the one that matches to this object.
        /// </summary>
        public void FindTracker()
        {
            if (assigned) return;
            ETrackedPropertyError error = new();
            StringBuilder sb = new();
            for (var i = 0; i < SteamVR.connected.Length; ++i)
            {

                OpenVR.System.GetStringTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_SerialNumber_String, sb, OpenVR.k_unMaxPropertyStringSize, ref error);
                var serialNumber = sb.ToString();
                if (serialNumber == desiredSerialNumber)
                {
                    UnityEngine.Debug.Log("Assigning device " + i + " to " + gameObject.name + " (" + desiredSerialNumber +")");
                    SetDeviceIndex(i);
                    indexOfTracker = i;
                    assigned = true;
                }
                // If there is nothing connected, SN is blank. Listing SNs may help in identifying the ones you want to assign.
                // SN for vive trackers can be found in SteamVR under "Manage Trackers"
                else if (serialNumber != "")
                {
                    //print("Serial number " + SerialNumber + "found at index " + i);
                }
            }

            if(!assigned)
            {
                UnityEngine.Debug.Log("Couldn't find a device with Serial Number \"" + desiredSerialNumber + "\"");
            }
        }

        private void Start() {
            FindTracker();
        }

        private void Awake()
        {
            OnEnable();
        }

        private void OnEnable()
        {
            var render = SteamVR_Render.instance;
            if (render == null)
            {
                enabled = false;
                return;
            }

            _newPosesAction.enabled = true;
        }

        private void OnDisable()
        {
            _newPosesAction.enabled = false;
            IsValid = false;
        }

        private void SetDeviceIndex(int index)
        {
            if (System.Enum.IsDefined(typeof(EIndex), index))
                this.index = (EIndex)index;
        }

        private void OnApplicationQuit() {
            XRSettings.LoadDeviceByName("None");
        }
    }
}