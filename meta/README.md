# 6Dash
An API and an optimization mod

## Features
- Gizmos (via [popcron/gizmos](https://github.com/popcron/gizmos))
- Level rendering optimization
- Curved blocks
- Other utilities useful for 3Dash mod developers

## Manual installation
1. Install any dependencies this mod has (listed above)
2. Install this mod the same way as described in BepInExPack manual installation guide

## Other used repositories
- [popcron/gizmos](https://github.com/popcron/gizmos)
  ([MIT](https://github.com/popcron/gizmos/blob/master/LICENSE))
- [Bunny83/UnityWindowsFileDrag-Drop](https://github.com/Bunny83/UnityWindowsFileDrag-Drop)
  ([MIT](https://github.com/Bunny83/UnityWindowsFileDrag-Drop/blob/master/LICENSE))

## Changelog

#### 0.4.1
* fixed color changers not working in custom levels
* changed mod icon

#### 0.4.0
* added checkpoint api
* added xml documentation
* fixed bug with broken camera animation after respawn on a checkpoint with speedhack lower than ~0.49
* added IsExternalInit for full C# 9 records support
* added online api
* cache downloaded levels
* changed item positions and rotations to floats
* made respawn time modifiable
* added path follower instance
* added World.levelUnload (and fixed playtesting)
* now distributed as a nuget package

#### 0.3.0
* initial Thunderstore release
* disable gizmos camera when not needed
* added music api (includes pulse value)
* added ui api
* added more chunk and world apis
* fixed orb hit effect on >1 attempts
* fixed noticeable z-fighting
