#Introduction 
In this project I have implemeted several ways of rendering billboarded GPU particles and measured the performance of each system in Unity3d engine.
Measurments come from Unity Stats(in game window) which displays the framerate of the program.

#Geometry shader, RWStructured Buffer, Structured Buffer.
8192x4096 particles
39 fps

#Geometry shader, Append Buffer, Structured Buffer.
8192x4096 particles
40 fps

#Geometry shader, Append Buffer, Consume Buffer.
8192x4096 particles
19 fps

#Vertex Shader(Instanced), Structured Buffer.
8192x4096 particles
20 fps