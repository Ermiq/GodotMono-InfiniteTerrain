using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Chunk : Spatial
{
	public const int N = 0, E = 1, S = 2, W = 3;
	public const int NW = 0, NE = 1, SE = 2, SW = 3;

	Chunk[] children;
	bool[] childrenReady;
	bool[] neighbours;

	ChunkShape shape;
	bool divide;

	Chunk parent;
	Basis faceBasis;
	Vector3 centerC, centerE;
	float size;
	int index;
	uint hashValue;
	int depth;
	Chunk root;

	public Chunk(Chunk parent, Basis faceBasis, Vector3 centerC, float size, int index, uint hashvalue, int depth, Chunk root = null)
	{
		this.parent = parent;
		this.faceBasis = faceBasis;
		this.centerC = centerC;
		this.size = size;
		this.index = index;
		this.hashValue = hashvalue;
		this.depth = depth;
		this.root = root == null ? this : root;

		centerE = World.EvaluatePosition(centerC);

		// Chunks are getting attached to the parent chunk:
		parent?.AddChild(this);

		// The shape mesh creator is a child of the chunk. Since the chunk is positioned at the actual planet surface
		// the mesh vertices won't have the issue with too big offset from the planet center.
		// They will have offset from the chunk center on sphere, not the planet.
		shape = new ChunkShape(faceBasis, centerC, size);
		AddChild(shape);
		//shape.Translation = centerC;

		children = new Chunk[0];
		childrenReady = new bool[4];
	}

	public Chunk[] GetAllChildren()
	{
		List<Chunk> result = new List<Chunk>();
		if (children.Length > 0)
			foreach (Chunk child in children)
				result.AddRange(child.GetAllChildren());
		else
			result.Add(this);
		return result.ToArray();
	}

	/// <param name="viewerPositionLocal">Camera position in the planet's local space.</param>
	public void Check(Vector3 viewerPositionLocal)
	{
		float dot = centerC.Normalized().Dot((viewerPositionLocal - centerE).Normalized());
		if (dot < -0.5f)
		{
			//shape.Remove();
			//return;
		}
		float distanceSq = centerE.DistanceSquaredTo(viewerPositionLocal);
		float hypetenuzeSq = Mathf.Pow(size, 2) + Mathf.Pow(size, 2);
		// When the distance is compared to the chunk hypotenuze*2, it gives the most consistent result where
		// two neighboring chunks with different detail levels always have difference as 2x (seems like always).
		if (distanceSq <= hypetenuzeSq * 2 && size > World.chunkSize)
		{
			divide = true;
			neighbours = null;
			if (children.Length == 0)
				CreateChildren();
			foreach (Chunk child in children)
				child.Check(viewerPositionLocal);
		}
		else
			divide = false;
	}

	public void Update()
	{
		if (divide && children.Length > 0)
		{
			foreach (Chunk child in children)
				child.Update();
		}
		else
		{
			if (!shape.isInProcess && NeighboursUpdate())
				shape.Create(neighbours, OnReady);
		}
	}

	void OnReady()
	{
		foreach (Chunk child in children)
		{
			if (IsInstanceValid(child))
				child.QueueFree();
		}
		children = new Chunk[0];
		childrenReady.Initialize();

		parent?.OnChildReady(index);
	}

	void OnChildReady(int childIndex)
	{
		childrenReady[childIndex] = true;

		bool all = true;
		foreach (bool c in childrenReady)
			if (!c)
				all = false;
		if (all)
			shape.Remove();
	}

	void CreateChildren()
	{
		children = new Chunk[4];
		Vector3 childCenter;
		// Top left:
		childCenter = centerC - faceBasis.x * size * 0.25f + faceBasis.z * size * 0.25f;
		children[NW] = new Chunk(this, faceBasis, childCenter, size * 0.5f, NW, hashValue * 4, depth + 1, root);
		// Top right:
		childCenter = centerC + faceBasis.x * size * 0.25f + faceBasis.z * size * 0.25f;
		children[NE] = new Chunk(this, faceBasis, childCenter, size * 0.5f, NE, hashValue * 4 + 1, depth + 1, root);
		// Bottom right:
		childCenter = centerC + faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[SE] = new Chunk(this, faceBasis, childCenter, size * 0.5f, SE, hashValue * 4 + 2, depth + 1, root);
		// Bottom left:
		childCenter = centerC - faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[SW] = new Chunk(this, faceBasis, childCenter, size * 0.5f, SW, hashValue * 4 + 3, depth + 1, root);
	}

	public bool NeighboursUpdate()
	{
		bool changed = false;
		if (neighbours == null)
		{
			changed = true;
			neighbours = new bool[4];
		}
		for (int i = 0; i < 4; i++)
		{
			bool b = CheckNeighbourLOD(i, hashValue);
			if (!changed && b != neighbours[i])
				changed = true;
			neighbours[i] = b;
		}
		return changed;
	}

	// Find neighbouring chunks LOD at slot by applying a partial inverse bitmask to the hash.
	private bool CheckNeighbourLOD(int direction, uint hash)
	{
		uint bitmask = 0;
		byte count = 0;
		uint localChunkQuadrant;

		// WILL A FOR LOOP RUN FASTER?
		while (count < depth) // 0 through 3 can be represented as a two bit number
		{
			count++;
			localChunkQuadrant = (hash & 3); // Get the two last bits of the hash. 0b_10011 --> 0b_11

			bitmask = bitmask * 4; // Add zeroes to the end of the bitmask. 0b_10011 --> 0b_1001100

			// Create mask to get the quad on the opposite side. 2 = 0b_10 and generates the mask 0b_11 which flips it to 1 = 0b_01
			if (direction == N || direction == S)
			{
				bitmask += 3; // Add 0b_11 to the bitmask
			}
			else
			{
				bitmask += 1; // Add 0b_01 to the bitmask
			}

			// Break if the hash goes in the opposite direction
			if ((direction == E && (localChunkQuadrant == NW || localChunkQuadrant == SW)) ||
				(direction == W && (localChunkQuadrant == NE || localChunkQuadrant == SE)) ||
				(direction == N && (localChunkQuadrant == SW || localChunkQuadrant == SE)) ||
				(direction == S && (localChunkQuadrant == NW || localChunkQuadrant == NE)))
			{
				break;
			}

			// Remove already processed bits. 0b_1001100 --> 0b_10011
			hash = hash >> 2;
		}

		// Return true if the quad in quadstorage is less detailed. REACH BEYOND THIS FACE IF THE CHUNK IS ON THE FACE'S BORDER.
		return root.GetNeighbourDetailLevel(hashValue ^ bitmask, depth) > depth;
	}

	// Find the detail level of the neighbouring quad using the querryHash as a map
	public int GetNeighbourDetailLevel(uint querryHash, int dl)
	{
		int dlResult = 0; // dl = detail level

		if (hashValue == querryHash)
		{
			if (children.Length > 0)
				dlResult = depth + 1;
			else
				dlResult = depth;
		}
		else
		{
			if (children.Length > 0)
			{
				dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetNeighbourDetailLevel(querryHash, dl - 1);
			}
		}

		/*
		if (children.Length > 0 && hashValue != querryHash)
			dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetNeighbourDetailLevel(querryHash, dl - 1);
		else if (hashValue == querryHash)
			dlResult = depth;
		*/
		return dlResult; // Returns 0 if no quad with the given hash is found
	}
}