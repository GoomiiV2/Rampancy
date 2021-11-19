<img src="https://img.shields.io/badge/Status-Alpha-blueviolet?style=for-the-badge"/>

<div align="center">
    <img src="https://cdn1.vox-cdn.com/uploads/chorus_asset/file/3727854/cortana_rampant.0.gif">
    <h1>Rampancy</h1>
</div>

A new level editor for the Halo games based on a CSG/Brush workflow, similar to Source/Quake level editing.

## Example
https://user-images.githubusercontent.com/5684215/142354890-78fd68fd-8048-478d-88af-c18a7746a4b7.mp4


## Status
The project is still early in development and some core issues need to be worked out. However, it should work as a solid proof of concept for those interested. Pull Requests for new features and bug fixes are welcome!
 
# How to Setup

* Download and Install [Unity 2021.2.0b4](https://unity3d.com/unity/beta/2021.2.0b4)
* Download the mod tools for the Halo games you want, eg. [Halo 1 on Steam](steam://install/1532190)
* Clone this project ```git clone --recurse-submodules https://github.com/GoomiiV2/Rampancy.git```
* Open the project (Rampancy/Unity directory) in Unity
* Browse and set the paths for the mod tools you will use
* Click the Rampancy menu at the top and then `Debug > Debug UI` and then click `Import bitmaps`
* Open the test scene at `Scenes/Tests/Test.unity`
* Press `F6` to compile
* * The following only need to be done once/whenever you want to specifically edit just them.
* * * In `Guerilla`, open the map scenario to configure the sky
* * * In `Sapien`, open the map to add and configure player spawns
* * Once configured (if needed), press `F5` in Unity to run the map in `halo_tag_test`

## Supported Games
* Halo 1 MCC
* Halo 3 (Eventually)
* Others (Uncertain, most likely at least Reach eventually)


## But, why?
As much as I love Blender and other dedicated 3D authoring software, I don't find them as fun to use as brush based editors like Hammer for Source. So why not try and bring that to the Halo games now that we have mod tools.
</br>
As well as hopefully simplifying map creation and streamlining the process.

## Yes, but why Unity?
Mostly for Realtime CSG. The awesome CSG library that's the core of this is tied into Unity, which means I am too!

I was looking at [TrenchBroom](https://trenchbroom.github.io/) and a Quake map compiler, but then I wouldn't really have the useful Subtractive Brushes.
</br>
Plus C# is nice <3

## Why the name Rampancy?
I like to do silly things and creating a new level editor for the old Halo games seemed a bit of a wild and silly thing to do. I figured that seemed like something a Rampant AI would do, so it felt fitting for the name of this.

## Libraries used with love! <3
* [Realtime CSG](https://github.com/LogicalError/realtime-CSG-for-unity)
* [inputsimulator](https://github.com/michaelnoonan/inputsimulator)
* [com.ionic.zlib](https://github.com/PixelWizards/com.ionic.zlib)
