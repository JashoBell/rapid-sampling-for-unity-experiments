# Rapid (VRPN) Sampling for Unity-based Experiments
## What?
At present the purpose of this set of scripts is to use [Virtual Reality Peripheral Network (VRPN)](https://vrpn.github.io/), an open-source framework for spatially tracking and representing a wide variety of VR peripherals, to sample positional data independently of Unity's framerate through a parallel thread. It arranges these sampled positions into a list of rows which are then compiled into a table at the end of an experimental trial. The additional thread does not interact with Unity except to determine when recording begins/ends, and to pass off the recorded positional data. The positional data in the thread can not be used to update Unity transforms, this is explicitly meant for measurement and recording.

The rapid sampling of data outside of Unity's main thread uses imported functions from the [hendrik-schulte/UVRPN](https://github.com/hendrik-schulte/UVRPN) Uvrpn.dll file. The recording of positional data uses classes from the [Unity Experiment Framework (UXF)](https://github.com/immersivecognition/unity-experiment-framework). The scripts were developed in Unity 2020.3, and are likely as backwards-compatible as the aforementioned packages, but I haven't tested this myself. 

## Why?
Unity is an incredibly useful environment for scripting experiments. However, the sampling of data from objects in Unity is capped at the refresh rate and linked to the delivery of frames as opposed to objective time. Having the sampling rate tied to Unity's framerate (for VR, typically 90hz) is not ideal when attempting to measure certain kinds of data, such as movement characteristics or reaction time, where milliseconds and sampling rate can be highly consequential. For example, the standard for movement science (although it may be excessive for certain applications) is typically reported at 200hz. To avoid Unity's limitations, it is necessary to use commands which don't interact with the Unity API.

## Disclaimer of Ineptitude
I made this to fit my dissertation work's specific needs. I am not a programmer by trade, so this code is probably ugly, inefficient and easily broken. I've probably arranged the repository and readme like a dummy. The scripts probably need to be cleaned of comments or code they don't actually use/need. I'll work on these issues when I can and try to help with any specific problems, but I don't have a lot of domain knowledge to draw upon and am learning as I go. At the very least, I hope I'm saving someone at least a portion of the (excessive amount of) time I spent solving this problem by providing an example.

## Required
Unity
[UVRPN](https://github.com/hendrik-schulte/UVRPN)
[UXF](https://github.com/immersivecognition/unity-experiment-framework)
A running VRPN server with at least one position tracker

## Quick-Start
1. Have a VRPN-based tracker.
2. Create a Unity project.
3. Install UXF and UVRPN.
4. Download repo as .zip, unpack into Unity, replacing the relevant UXF files.
5. Create an experiment with UXF, or use one of the example scenes (I used "Move to Target" while making this)
6. Place the "VRPN_UXF_PosOri" script onto any GameObject.
7. Designate the GameObject as a tracker using UXF.
8. Ensure the address and channel are correct.
9. Run the experiment.
    9b. Stop after a couple of trials to ensure the recorded values are correct.


## Common Problems
* Recordings of (-501, -501, -501) at every time point
    * Your script's options might not be pointing to the correct VRPN address/channel.

## Personal To Dos
Generalize to other APIs (e.g., SteamVR, Pupil-labs)
Optimize, make more safe and efficient.
Determine the best route for scalability (multiple trackers).
