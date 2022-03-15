# Changelog

All notable changes to this project will be documented in this file

| modName | Conformal Decals                                                                     |
| ------- |:-------------------------------------------------------------------------------------|
| license | CC-By-SA & GPL3                                                                      |
| website | https://forum.kerbalspaceprogram.com/index.php?/topic/194802-18-111-conformal-decals |
| author  | Andrew Cassidy                                                                       |

## 0.2.10 - 2022-03-14

### Fixed

- Fixed decals not projecting on loading prefabs

### Changed

- Re-enabled projecting onto TransparentFX layer

### Added

- Allowed for regular expressions to be used when blacklisting shaders
- Added all Waterfall shaders to the shader blacklist when Waterfall is present


## 0.2.9 - 2022-03-12

### Fixed

- Fixed text decals breaking when used in symmetry
- Fixed decals projecting onto the TransparentFX layer, such as Waterfall plumes


## 0.2.8

- Update bundled Shabby to support Harmony 2 for compatibility with other mods
- Update bundled B9PartSwitch to 2.18.0


## 0.2.7

- Supported KSP versions: 1.8.x to 1.11.x

### Notes:

- Attaching decal parts in flight using engineer kerbals is not supported.

### Fixed:

- Fixed certain non-ascii strings not rendering correctly under certain circumstances.
- Yet another attempted fix for the planet text glitch.


## 0.2.6

### Fixed:

- Fixed stock flags appearing stretched by forcing their aspect ratio to be correct.
- Another attempted fix for the planet text glitch.


## 0.2.5

### Fixed:

- Fixed line spacing, character spacing, and vertical settings not applying to symmetry counterparts


## 0.2.4

### Fixed:

- Fixed red text appearing on planets due to KSP bug by clearing render textures afterwards.
- Fixed fonts not saving correctly.

### Changed:

- Lowered step size for decal size and depth to 1cm.
- Changed default max size to 5m.
- Changed default text decal size to 0.2m
- Text decals now show as a circle if they contain only whitespace.


## 0.2.3

### Fixed:

- Fixed TMP subobjects being deleted, causing fallback fonts to fail in some situations.
- Started using URL-style encoding for text decals behind the scenes to prevent issues with certain characters.
- Fixed text decals having zero size when they had only whitespace or an empty string.
- Fixed decals having drag and causing issues when using FAR.
- Fixed broken saving of text decals in certain circumstances.


## 0.2.2

### Fixed:

- Fixed corrupted text rendering when a vessel loads during a scene change.


## 0.2.1

### Changed:

- Pressing enter in the text entry window now types a newline.

### Fixed:

- Renamed font assetbundle. The old extension was causing the game to try to load it twice on Windows due to legacy compatability features.
- Fixed text rendering on DirectX resulting in black boxes by using ARGB32 instead of RG16 for the render texture in DirectX.


## 0.2.0

### New Parts:

- CDL-3 Surface Base Decal: A set of conformal decals based on the symbols from the movie Moon (2009) designed by Gavin Rothery
- CDL-T Custom Text Decal: A customizable text decal with a variety of fonts 

### Changed:

- New ModuleConformalText module for customizable text
- Text, font, and style can all be customized, as well as text fill and outline colors and widths
- Same projection and opacity options as other conformal decals
- New StandardText decal shader supporting the text module
- Unified all decal shaders into a single "StandardDecal" shader with variants supporting any combination of bump, specular and emissive maps, plus SDF alphas.
- Old shaders are remapped to Standard shader plus keywords automatically.
- New SDF-based antialiasing for when decals extend to their borders, e.g. on opaque flags.
- New "KEYWORD" material modifier, allowing for shader features to be enabled and disabled.
- Material modifiers can now be removed in variants by setting `remove = true` inside them.

### Fixed:

- Fixed WIDTH and HEIGHT scale modes being flipped
- Removed some debug log statements
- Dependencies:
- Updated ModuleManager to version 4.1.4


## 0.1.4

Supported KSP versions: 1.8.x to 1.10.x

### Fixed:

- Fixed decals rendering onto disabled B9PS part variants
    - Decals will still not update whan their parent part's B9PS variant is changed, both in flight and in the editor. This is known and awaiting a change to B9PS to be fixed.
- Fixed decal bounds rendering as dark cubes when shadowed by EVE clouds.
- Fixed decals being shadowed by EVE clouds, causing the part underneath to appear overly dark.


## 0.1.3

### Fixe:

- Fixed decals being able to be scaled down to 0

### Changed:

- Made decal bounds no longer collide in flight, this is done by repurposing layer 31 (which is configurable in the ConformalDecals.cfg file)
- Decals will now be unselectable in flight by default. This can be disabled with the `selectableInFlight` value in ConformalDecals.cfg, or in the module config itself.
- Decal parts will now destroy themselves automatically when the parent part is destroyed
- Small refactor of node parsing code
    - Colors can now be specified in hex (#RGB, #RGBA, #RRGGBB, or #RRGGBBAA) or using the colors specified in the XKCDColors class


## 0.1.2

### Fixed:

- Disabled writing to the zbuffer in the decal bounds shader. Should fix any issues with Scatterer or EVE.


## 0.1.1

### Fixed:

- Fixed flag decal not adjusting to new texture sizes immediately.
- Fixed decal bounds being visible on launch.
- Fixed decal bounds being visible in the part icon.


## 0.1.0

Initial release!

### New Parts:

- CDL-F Flag Decal: Conformal flag decal, which uses either the mission flag or a flag of your choosing.
- CDL-1 Generic Decal: A set of conformal generic decals for planes and rockets
- CDL-2 Semiotic Standard Decal: A set of conformal decals based on the Semiotic Standard for All Commercial Trans-Stellar Utility Lifter and Transport Spacecraft designed by Ron Cobb for the movie Alien