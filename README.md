# Milk_Instancer01
 Unity indirect GPU instancing & painting with occlusion culling, frustum culling, and LODs
 
 ![painting2](https://user-images.githubusercontent.com/59656122/143315069-8fce511a-4012-48f8-8a07-b9140d8e5ca1.gif)

 This project is a vastly improved version of elliomans indirect rendering with compute shaders. https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders
 
 What is different in this version?
  - gpu bitonic sorting is now no longer reliant on powers of two (thanks to EmmetOT - https://github.com/EmmetOT/BufferSorter)
  - improved user interface
  - instances can now be painted into the scene, much like unitys terrain detail system
  - this version works in HDRP
  - amplify shader editor examples
  - wind shader example


 What am I still working on?
  - find and fix bugs
  - instance density control
  - shadergraph examples
  - support more that 256 * 256 (65536) instances
  - URP shaders
  - more spawning methods, perhaps procedural terrain spawning
  - create a user friendly interface
  - idk im sure ill think of something

 What might i consider in the future?
  - built in renderer support

 Project info:
  - created on unity 2021.2.1f1 HDRP 12.1.0

![walking](https://user-images.githubusercontent.com/59656122/143317319-14eb5d2f-3adf-45b2-9dfd-b1ea95af971b.gif)

![grass](https://user-images.githubusercontent.com/59656122/142703484-4bb21330-5e90-4cea-a69a-ff53977d595f.gif)
