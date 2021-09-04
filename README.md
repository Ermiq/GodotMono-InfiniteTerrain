The car model and the vehicles code algorithm is from the awesome [Tobalation's GDCustomRaycastVehicle](https://github.com/Tobalation/GDCustomRaycastVehicle) repository. I just translated it to C# and did very little tweaking.

My implementation of a Quadd-Tree LOD system.  

To fix the cracks between the chunk of different size/detail, the 'skirts' approach is implemented.  

The 'skirt' is a set of additional quads around a chunk. Essentially they go outside the chunk normal boundaries and overlap the neighbouring chunks. Then, the noise function is applied to all the vertices, the normals are calculated, and after that, the skirt vertices are lowered down to go underground, disguising the cracks. Due to the fact that the normals were generated before the lowering, the terrain border edge will look smooth as if the skirt vertices still were aligned with the terrain surface.
