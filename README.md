# NeosWCFace

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/)  
Integrates WCFace's screen-mode face tracking. Tracks the following (currently), per eye and combined:
- Eye Gaze
- Eye Openess
- Eye Wideness

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader) and [mixNmatch_lipsNmouth](https://github.com/dfgHiatus/mixNmatch_lipsNmouth/releases/tag/v1.0.1).
2. Install [WCFace](https://github.com/Ruz-eh/NeosWCFaceTrack/releases/tag/1.0.2). In the Binaries folder, run the "run.bat" and follow the prompts.
3. Place [Neos-WCFace-Integration.dll](https://github.com/dfgHiatus/Neos-WCFace-Integration/releases/tag/v1.0.0) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
5. Start the game!

If you want to verify that the mod is working you can check your Neos logs, or create an EmptyObject with an AvatarRawEyeData Component (Found under Users -> Common Avatar System -> Face -> AvatarRawEyeData).

Thanks to those who helped me test this!
