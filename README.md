The car model and the vehicles code algorithm is from the awesome [Tobalation's GDCustomRaycastVehicle](https://github.com/Tobalation/GDCustomRaycastVehicle) repository. I just translated it to C# and did very little tweaking.

An 'infinite' procedural terrain implementation with quad-tree LOD system and floating origin.

Technically, the terrain is not actually infinite. It is limited to the `mainChunkSize` value in the `TerrainSettings.cs` which is set 10,000,000, meaning that the terrain is 10,000 km by 10,000 km.

In `World.cs` the root chunk object is created and updated. The root chunk is able to subdivide into 4 children chunks, and each child is also able to subdivide based on the camera distance.
When further subdivision is not needed, the chunk creates the shape (`ChunkShape.cs`) which is inherited from the Godot `MeshInstance` node and it handles all the mesh generation code.

The world script has the `TerrainSettings.cs` object that has the settings:
`mainChunkSize` - the world's initial root chunk size,
`chunkSize` - the minimal chunk size that represents the chunk size for the closest camera position after which the chunk won't be subdivided further,
`detail` - a chunk's plane subdivision level, the amount of the triangle pairs (quads) in 1 chunk. E.g. `detail = 50` means each chunk consists off 50 by 50 quads (triangle pairs) on X and Z axes.

The problem of crack holes between 2 chunks of different size is solved by 'skirts'. Skirts are additional quads on each side of a chunk, that are at first expanded to the sides and then lowered down. These quads effectively disguise the crack seams. Also, since the chunk mesh normals are calculated before the skirts are lowered, it also solves the problem of noticable seams on edges of shaded chunk surfaces.

The floating origin eliminates the problem of precision degradation at growing distances from the 3D world origin point.
It is solved by moving the camera to zero position each time it goes beyond the certain distance from the origin, and shifting all the other objects in the scene by the same offset (the camera position vector before it got reset).
The `FloatingOrigin.cs` broadcasts the event message everytime the camera exceeds the threshold distance. Each object in the scene subscribes to this event and moves itself by the offset vector provided by the event delegate.
