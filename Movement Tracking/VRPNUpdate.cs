using System.Runtime.InteropServices;
using System;
using System.Numerics;

namespace UXFTrackingUtilities
{
public static class VRPNUpdate
    {

        [DllImport("unityVrpn")]
        private static extern double vrpnTrackerExtern(string address, int channel, int component, int frameCount);

        /// <summary>
        /// Reads in position data from VRPN server.
        /// </summary>
        /// <param name="address">The address of the VRPN server</param>
        /// <param name="channel">The channel of the VRPN tracker</param>
        /// <returns>A float array of size 3 corresponding to position values in a three-dimensional coordinate space (i.e., a vector3)</returns>
        internal static float[] VrpnTrackerPos(string address, int channel)
        {
            var x = (float)vrpnTrackerExtern(address, channel, 0, DateTime.Now.Millisecond);
            var y = (float)vrpnTrackerExtern(address, channel, 1, DateTime.Now.Millisecond);
            var z = (float)vrpnTrackerExtern(address, channel, 2, DateTime.Now.Millisecond);

            return new float[3]
            {
                x,
                y,
                z
            };

        }
        /// <summary>
        /// Reads in position data from VRPN server.
        /// </summary>
        /// <param name="address">The address of the VRPN server</param>
        /// <param name="channel">The channel of the VRPN tracker</param>
        /// <returns>A System.Numerics Vector3</returns>
        internal static Vector3 VrpnTrackerVector3(string address, int channel)
        {
            var x = (float)vrpnTrackerExtern(address, channel, 0, DateTime.Now.Millisecond);
            var y = (float)vrpnTrackerExtern(address, channel, 1, DateTime.Now.Millisecond);
            var z = (float)vrpnTrackerExtern(address, channel, 2, DateTime.Now.Millisecond);

            return new Vector3
            (
                x,
                y,
                z
            );

        }

        /// <summary>
        /// Reads in orientation data from VRPN server.
        /// </summary>
        /// <param name="address">The address of the VRPN server</param>
        /// <param name="channel">The channel of the VRPN tracker</param>
        /// <returns>A float array of size 4 (i.e., a quaternion array)</returns>
        internal static float[] VrpnTrackerQuat(string address, int channel)
        {
            var q1 = (float)vrpnTrackerExtern(address, channel, 3, DateTime.Now.Millisecond);
            var q2 = (float)vrpnTrackerExtern(address, channel, 4, DateTime.Now.Millisecond);
            var q3 = (float)vrpnTrackerExtern(address, channel, 5, DateTime.Now.Millisecond);
            var q4 = (float)vrpnTrackerExtern(address, channel, 6, DateTime.Now.Millisecond);
            return new float[4]
            {
                q1,
                q2,
                q3,
                q4
            };
        }
    }
}