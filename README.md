<img src="https://img.shields.io/badge/Status-Alpha-blueviolet?style=for-the-badge"/>

<div align="center">
    <img src="https://cdn1.vox-cdn.com/uploads/chorus_asset/file/3727854/cortana_rampant.0.gif">
    <h1>Rampancy</h1>
</div>

A new level editor for the Halo games based on a CSG/Brush workflow, similar to Source/Quake level editing.

## Example
https://user-images.githubusercontent.com/5684215/148701369-66ef75eb-520c-46e7-a6d0-914bbc6bbbed.mp4

![sapien_l3i7oo4Zha](https://user-images.githubusercontent.com/5684215/148701019-d1cb7d9d-8a81-4fb8-bf57-98a3e39e9b7a.jpg)
![Unity_kmgOSmG7sh](https://user-images.githubusercontent.com/5684215/148701055-6dfe89b7-73e8-4011-a1dd-50d98bfb6c92.jpg)


## Status
The project is still early in development and some core issues need to be worked out. However, it should work as a solid proof of concept for those interested. Pull Requests for new features and bug fixes are welcome!
 
# How to Setup

* Download and Install [Unity 2022.1.0b16](https://unity3d.com/unity/beta/2022.1.0b16)
* Download and install [Git](https://git-scm.com/downloads) (This is needed for Unity)
* Download the mod tools for the Halo games you want, eg. [Halo 1 on Steam](steam://install/1532190)
* Create a new Unity project. (this will be where your map sources will be)
* Open the project.
  * Go to ``Window > Package Manager``
  * In the top left corner of that new window click the + icon
  * Select ``add package from git url`` and enter ``https://github.com/GoomiiV2/Rampancy.git?path=RampancyUnity/Packages/com.goomiiv2.rampancyinstaller#Halo3``
  * Wait a moment for the packages to be installed, when its done you should see a ``Settings`` menu open askingyou for the Halo tool installs
  * Browse and set the paths for the mod tools you will use
* Click the Rampancy menu at the top and then `Debug > Open Level UI > Materials` and then click `Sync Materials from`
* Click `Rampancy > Create Level` give it a name with no spaces and then click `Create new Level`.
* Click `Window > Realtime CSG Window` and dock it some where.
* Click the `Generate` tab and create a large box in the scene, this will be the level.
* You can then add more brushes to detail the level as you want later.
* Press `F6` to compile
* * The following only need to be done once/whenever you want to specifically edit just them.
* * * In `Guerilla`, open the map scenario to configure the sky
* * * In `Sapien`, open the map to add and configure player spawns
* * Once configured (if needed), press `F5` in Unity to run the map in `halo_tag_test`

A bit more indepth example of creating a level: https://github.com/GoomiiV2/Rampancy/wiki/Creating-a-new-level

# Updating

To update open the package manger ``Window > Package Manager`` and select and update both ``Rampancy`` and ``Rampancy Installer``

## Supported Games
### Halo 1 MCC
- [x] Mesh exporting
- [x] Material Importing
- [ ] Per material surface settings
- [ ] Portal planes
- [ ] Special shape regions (weather polys)
- [ ] Device placements
- [ ] Lightmap baking with Unitys light mapper (not tools, that works already)

### Halo 2 MCC
* soon tm?
  
### Halo 3
- [x] Mesh exporting
- [x] Material Importing
- [x] Brush Based Instances
- [x] Light objects
- [ ] Per material surface settings
- [ ] Portal planes
- [ ] Special shape regions (weather polys)
- [ ] External imported mesh instances (eg, complex non brush meshes from blender)
- [ ] Device xrefs?

### Halo ODST
* See Halo 3 above
  
### Halo Reach
* hopefully

### Halo 4
* hopefully

### Misc
- [ ] Blender Material Exporter (as a helper for importing mesh instances)
- [ ] Terrain tools


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
* [com.ionic.zlib](https://github.com/PixelWizards/com.ionic.zlib)
* [Pfim](https://github.com/nickbabcock/Pfim)
