using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vrpnUpdate
    {
        [DllImport("unityVrpn")]
        private static extern double vrpnTrackerExtern(string address, int channel, int component, int frameCount);
        public static int recordRate = 200;
        public static string[] vrpnTrackerPos(string address, int channel)
        {
            string format = "0.####";
            float x = (float)vrpnTrackerExtern(address, channel, 0, recordRate);
            float y = (float)vrpnTrackerExtern(address, channel, 1, recordRate);
            float z = (float)vrpnTrackerExtern(address, channel, 2, recordRate);

            return new string[3]{
                x.ToString(format),
                y.ToString(format),
                z.ToString(format)};

        }

        public static string[] vrpnTrackerQuat(string address, int channel)
        {
            string format = "0.####";
            float q1 = (float)vrpnTrackerExtern(address, channel, 3, recordRate);
            float q2 = (float)vrpnTrackerExtern(address, channel, 4, recordRate);
            float q3 = (float)vrpnTrackerExtern(address, channel, 5, recordRate);
            float q4 = (float)vrpnTrackerExtern(address, channel, 6, recordRate);
            return new string[4]{
                q1.ToString(format),
                q2.ToString(format),
                q3.ToString(format),
                q4.ToString(format)};
        }
    }