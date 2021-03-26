using System.Runtime.InteropServices;
using System;
public class vrpnUpdate
    {

        [DllImport("unityVrpn")]
        private static extern double vrpnTrackerExtern(string address, int channel, int component, int frameCount);
        internal static string[] vrpnTrackerPos(string address, int channel)
        {
            string format = "0.####";
            float x = (float)vrpnTrackerExtern(address, channel, 0, DateTime.Now.Millisecond);
            float y = (float)vrpnTrackerExtern(address, channel, 1, DateTime.Now.Millisecond);
            float z = (float)vrpnTrackerExtern(address, channel, 2, DateTime.Now.Millisecond);

            return new string[3]{
                x.ToString(format),
                y.ToString(format),
                z.ToString(format)};

        }

        internal static string[] vrpnTrackerQuat(string address, int channel)
        {
            string format = "0.####";
            float q1 = (float)vrpnTrackerExtern(address, channel, 3, DateTime.Now.Millisecond);
            float q2 = (float)vrpnTrackerExtern(address, channel, 4, DateTime.Now.Millisecond);
            float q3 = (float)vrpnTrackerExtern(address, channel, 5, DateTime.Now.Millisecond);
            float q4 = (float)vrpnTrackerExtern(address, channel, 6, DateTime.Now.Millisecond);
            return new string[4]{
                q1.ToString(format),
                q2.ToString(format),
                q3.ToString(format),
                q4.ToString(format)};
        }
    }