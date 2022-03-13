# Milk_Instancer01
 Unity indirect GPU instancing & painting with occlusion culling, frustum culling, and LODs
 
 WARNING - THERE IS A MAJOR UPDATE COMING SOON WHICH ADDS A BUNCH OF FEATURES, BUT BREAKS THE CURRENT SETUP USED WITH THE PROJECT VERION IN THIS REPOSITORY. WHEN THIS UPDATE IS COMPLETE, I WILL RELEASE A VIDEO TUTORIAL, AND LINK IT HERE.
 
 ![painting (2)](https://user-images.githubusercontent.com/59656122/150681820-37e0c5b9-d7b3-4eaa-9f09-4b6b49b648e5.gif)

 This project is a vastly improved version of elliomans indirect rendering with compute shaders. https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders
 
 What is different in this version?
  - gpu bitonic sorting is now no longer reliant on powers of two (thanks to EmmetOT - https://github.com/EmmetOT/BufferSorter)
  - improved user interface
  - instances can now be painted into the scene, much like unitys terrain detail system
  - this version works in HDRP
  - amplify shader editor examples
  - wind shader example

Features
  - HDRP instancing
  - instance density control
  - paint prefabs on any object
  - multiple prefab support
  - per-instance occlusion culling
  - frustum culling
  - up to 3 LODs
  - Amplify Shader Editor examples
  - ShaderGraph examples
  - wind shader example (ASE and SG)
  - Multi-Material Prefab support (for things like trees)


 What am I still working on?
  - find and fix bugs
  - world streaming/detail zones
  - URP shaders
  - more spawning methods, perhaps procedural terrain spawning
  - create a user friendly interface + gizmos
  - built in renderer support
  - idk im sure ill think of something

 Project info:
  - created on unity 2021.2.1f1 HDRP 12.1.0 (tested as low as HDRP 10)

![walking](https://user-images.githubusercontent.com/59656122/143317319-14eb5d2f-3adf-45b2-9dfd-b1ea95af971b.gif)

![grass](https://user-images.githubusercontent.com/59656122/142703484-4bb21330-5e90-4cea-a69a-ff53977d595f.gif)

![spheres](https://user-images.githubusercontent.com/59656122/153914007-831e1b7a-1691-46d4-a8eb-6735d22894cc.gif)

![Screenshot_2](https://user-images.githubusercontent.com/59656122/157997969-45608cbc-daec-4d1a-85d0-aba038485d9f.png)

