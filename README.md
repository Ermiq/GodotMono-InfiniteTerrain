The car model and the vehicles code algorithm is from the awesome [Tobalation's GDCustomRaycastVehicle](https://github.com/Tobalation/GDCustomRaycastVehicle) repository. I just translated it to C# and did very little tweaking.

In this implementation of a Quad-Tree LOD system a method of vertex stitching is applied. The methods in the `Chunk.cs` with finding out neighbours detail level using hash adress and bitmask are not mine. I took it from Simon Holmqvist's incredible video https://www.youtube.com/watch?v=YueAtA_YnSY&t=741s.

A simpler implementation that uses skirts instead could be found in the `quadtreelod-skirts` branch of this repo.

This version is not actually infinite. It is limited to the `mainChunkSize` value in the `World.cs`. It is set 1,000,000. However, at that far distance the floating point presicion issues arrive anyway, so without a proper origin shifting the infinity is quite pointless. But the world limits could be expanded by either a naive `mainChunkSize` value increase or a usage of an array of main chunks (placing them with some offset from each other, moving back and forth when the player bypass the boundaries).
