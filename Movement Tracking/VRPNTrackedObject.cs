using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UXFTrackingUtilities
{
/// <summary>
/// Updates the position of a GameObject based on the values of a VRPN-based positional tracker.
/// </summary>
public class VRPNTrackedObject : MonoBehaviour
{
    public string device = "PPT0";
    [Tooltip("zero-indexed id of the device you are sampling position data from.")]
    public string address = "localhost";
    public int id = 0;
    public bool position = true;
    public bool orientation = false;
    public Vector3 preTranslate = new Vector3(0, 0, 0);
    public Vector3 preRotate = new Vector3(0, 0, 0);

    /// <summary>
    /// Combines name and address.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="device"></param>
    /// <returns>The address and device name.</returns>
    private static string GetTrackerAddress(string address, string device)
    {
        address = "@" + address;
        var fulladdress = device + address;
        return fulladdress;
    }

    // Update is called once per frame
    private void Update()
    {
    
        Vector3 trackedPosition = position ? 
                                  new (VRPNUpdate.VrpnTrackerPos(GetTrackerAddress(address, device), id)[0],
                                       VRPNUpdate.VrpnTrackerPos(GetTrackerAddress(address, device), id)[1],
                                       VRPNUpdate.VrpnTrackerPos(GetTrackerAddress(address, device), id)[2]) 
                            :     transform.position;
        Quaternion trackedOrientation = orientation ?
                                  new (VRPNUpdate.VrpnTrackerQuat(GetTrackerAddress(address, device), id)[0],
                                       VRPNUpdate.VrpnTrackerQuat(GetTrackerAddress(address, device), id)[1],
                                       VRPNUpdate.VrpnTrackerQuat(GetTrackerAddress(address, device), id)[2],
                                       VRPNUpdate.VrpnTrackerQuat(GetTrackerAddress(address, device), id)[3])
                            :      transform.rotation;

        if(preTranslate != Vector3.zero)
        {
            trackedPosition += preTranslate;
        }
        if(preRotate != Vector3.zero)
        {
            transform.rotation *=  Quaternion.Euler(preRotate);
        }
        
        transform.position = trackedPosition;

    }
}
}