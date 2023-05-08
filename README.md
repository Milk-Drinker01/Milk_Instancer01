# Milk_Instancer01
 Indirect GPU instancing & painting with occlusion culling, frustum culling, and LODs in Unity
 
 This Project is undergoing a large refactor.
 
 ![painting (2)](https://user-images.githubusercontent.com/59656122/150681820-37e0c5b9-d7b3-4eaa-9f09-4b6b49b648e5.gif)

 This project is built off of elliomans indirect rendering with compute shaders. https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders
 
 What is different in this version?
  - gpu bitonic sorting is now no longer reliant on powers of two (thanks to EmmetOT - https://github.com/EmmetOT/BufferSorter)
  - improved user interface
  - instances can now be painted into the scene, much like unitys terrain detail system
  - amplify shader editor examples
  - wind shader example

Features
  - BIRP support (included in this repo (occlusion culling not setup yet))
  - URP support (soon)
  - HDRP support (https://github.com/Milk-Drinker01/Milk_Instancer_HDRP)
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
  - world streaming/detail zones
  - VR: supports both multi pass and Single Pass Instanced
 
 What am I still working on?
  - Render Pipeline Consolidation
  - built in renderer occlusion culling
  - URP support
  - more spawning methods, perhaps procedural terrain spawning

 Project info:
  - created on unity 2021.3.1f1 (tested as low as 2020.3)

if you find this project useful to you in any way, feel free to leave it a star, or subscribe to my youtube. it helps me out alot! https://www.youtube.com/c/MilkDrinker01

![walking](https://user-images.githubusercontent.com/59656122/143317319-14eb5d2f-3adf-45b2-9dfd-b1ea95af971b.gif)

![grass](https://user-images.githubusercontent.com/59656122/142703484-4bb21330-5e90-4cea-a69a-ff53977d595f.gif)

![spheres](https://user-images.githubusercontent.com/59656122/153914007-831e1b7a-1691-46d4-a8eb-6735d22894cc.gif)

![Screenshot_2](https://user-images.githubusercontent.com/59656122/157997969-45608cbc-daec-4d1a-85d0-aba038485d9f.png)

