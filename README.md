<img src="https://img.shields.io/badge/Status-Alpha-blueviolet?style=for-the-badge"/>
 
<div align="center">
    <img src="https://cdn1.vox-cdn.com/uploads/chorus_asset/file/3727854/cortana_rampant.0.gif">
    <h1>Rampancy</h1>
</div>
 
A new level editor for the Halo games based on a CSG / brush workflow, think Source / Quake.
 
## Example
https://user-images.githubusercontent.com/5684215/142354890-78fd68fd-8048-478d-88af-c18a7746a4b7.mp4




## Status
THe project is still early in dev and some core issues need to be worked out, but works as a proof of concept I think :>
 
# How to setup
This might change later but for now.
 
* Download and install Unity 2021.2.0b4
* Download the mod tools for the Halo games you want, eg. [Halo 1 on Steam](steam://install/1532190)
* Clone this project ```git clone --recurse-submodules https://github.com/GoomiiV2/Rampancy.git```
* Open the project in Unity
* Browse and set the paths for the mod tools you will use
* Open the test scene in ``Scenes/Tests/Test``
* Press f6 to compile
* * Then set it up in guerilla to set the sky
* * And in sapien to set a player spane
* * and when its done f5 to run it in tag test
 
## Supported Games
* Halo 1 MCC
* Halo 3 (Not started yet)
* Most of the others, at some point, maybe (Reach i'm interested in :>)
 
 
## But, why?
Because as much as I love Blender and 3d authoring software I don't find them as fun to use as brush based editors like Hammer for Source, so why not try and bring that to the Halo games now that we have mod tools.
</br>
As well as hopefully simplifying map creation and streamlining the process.
 
## Yes, but why Unity?
Mostly for Realtime CSG the awesome csg lib that's the core of this, it's tied to Unity so then so am I! (I was looking at Trenchbroom and a Quake map compiler, but then I wouldn't really have subtractive brushes).
</br>
Plus C# is nice <3
 
## Why the name Rampency?
Oh, I like to do silly stuff so this is an other one of those, so I figure making a new level editor for the old Halo games is a pretty Ramenet AI thing to do, so guess i'm rampant :D
 
## Libs used with love! <3
* [Realtime CSG](https://github.com/LogicalError/realtime-CSG-for-unity)
* [inputsimulator](https://github.com/michaelnoonan/inputsimulator)
* [com.ionic.zlib](https://github.com/PixelWizards/com.ionic.zlib)

