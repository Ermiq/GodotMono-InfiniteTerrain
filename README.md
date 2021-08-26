The car model and the vehicles code algorithm is from the awesome [Tobalation's GDCustomRaycastVehicle](https://github.com/Tobalation/GDCustomRaycastVehicle) repository. I just translated it to C# and did very little tweaking.

My approach is a bit different than the commonly used chunk tree LOD system. It is due to I struggled a lot trying to implement a fix for the cracks between 2 chunks with different detail level. Like these ones:

![cracks](https://user-images.githubusercontent.com/58442318/131003518-bc9af4c1-43cb-476a-98a6-43cc4aed2da3.png)

...and finally gave up on it because it was very hard to find out where the cracks are relative to the chunk. I decided to do it the easier way. The way that makes it very clear which chunk has the cracks and at which side exactly.

# Basic principle:

The terrain still consists of the chunks. But the trick is, the chunks are stored in a `Ring` that handles the chunks positions and sizes. Also, the `Ring` sets the relative XZ index that helps to determine at which side from the origin point the chunk is positioned.

A ring handles either 1 or 8 chunks. The central ring at the player position has only 1 chunk with size `chunkSize * 3`. And the next rings suround each previous ring with 8 chunks. The size of a chunk in each ring farther from the origin is increased by 3. The `detail` stays the same for any chunk since the 'resolution' of the chunk is affected by it's size, not the detail variable.

It looks like this if we set chunk detail level to 1:

![3x8grid](https://user-images.githubusercontent.com/58442318/131001720-43f9d452-f2a3-4c23-be74-d8b37fa8a009.png)

In this setup we always know that if the chunk has index (X:-1, Z:0), i.e., is positioned to the left from the origin, then it will have the cracks at the right side, so we create the additional vertices at every quad on the right edge of the chunk:
![seam_vertices](https://user-images.githubusercontent.com/58442318/131010063-120b5e9c-3f78-4428-bf7e-26a2ae91a0b4.png)

# Disadvantages:

This setup has a flow though. The detail level decreases quite significantly by the distance from the center. In the traditional chunk tree it is decresed by 2 meaning that each chunk detail level decrease makes the chunk to have 2 times less quads than the previous one. And in the approch I have choosed we have 3 times detail decrese rate.

But on the other hand, we don't need to separately update and determine what detail level each chunk should to have as commonly done in the chunk tree LOD setups. 

In this centrilized setup all we need to care about is just the original reference size of a chunk and the original detail level. All other chunks and details will be managed automatically when we trigger the terrain update function.
