The car model and the vehicles code algorithm is from the awesome [Tobalation's GDCustomRaycastVehicle](https://github.com/Tobalation/GDCustomRaycastVehicle) repository. I just translated it to C# and did very little tweaking.

My implementation of a Quadd-Tree LOD system.  

To fix the cracks between the chunk of different size/detail, the 'skirts' approach is implemented.  

The 'skirt' is a set of additional quads around a chunk. Essentially they go outside the chunk normal boundaries and overlap the neighbouring chunks. Then, the noise function is applied to all the vertices, the normals are calculated, and after that, the skirt vertices are lowered down to go underground, disguising the cracks. Due to the fact that the normals were generated before the lowering, the terrain border edge will look smooth as if the skirt vertices still were aligned with the terrain surface.

This version is not actually infinite. It is limited to the `mainChunkSize` value in the `World.cs`. It is set 1,000,000. However, at that far distance the floating point presicion issues arrive anyway, so without a proper origin shifting the infinity is quite pointless. But the world limits could be expanded by either a naive `mainChunkSize` value increase or a usage of an array of main chunks (placing them with some offset from each other, moving back and forth when the player bypass the boundaries).
