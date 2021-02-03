# Rapid VRPN Sampling Solution for Unity-based Experiments
Uses VRPN plugin commands to sample positional data independently of Unity's framerate through an additional thread. Records these sampled positions into a list of rows, which are then compiled into a table at the end of an experimental trial.

Made with [UXF 2.1.1](https://github.com/immersivecognition/unity-experiment-framework) and Unity 2020.2.1f1, UnityVrpn dll from [hendrik-schulte/UVRPN](https://github.com/hendrik-schulte/UVRPN). [Virtual Reality Peripheral Network (VRPN)]() is an open-source framework for spatially tracking and representing a wide variety of VR peripherals.

## Why?
Unity, especially with UXF, is an incredibly useful environment for scripting experiments. However, the sampling of data from objects in unity is capped at the refresh rate. Having the sampling rate tied to Unity's framerate (for VR, typically 90hz) is not ideal when attempting to measure certain kinds of data, such as movement characteristics. The standard for movement science (although it may be excessive for certain applications) is typically reported at 200hz.

## Required:
Unity Experiment Framework (UXF)
A running VRPN server

## Quick-Start
Create a Unity project.
Install UXF.
Download .zip, unpack into Unity.


## To Do, Maybe?
Add more scripts for other APIs
