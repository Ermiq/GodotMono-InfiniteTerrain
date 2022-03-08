using Godot;
using System.Collections.Generic;

public class Chunk : MeshInstance
{
	Chunk parent;
	Chunk[] children;
	bool[] childrenReady;

	Basis faceBasis;
	Vector3 center;
	float size;
	TerrainSettings settings;
	int index;
	
	ChunkShape shape;

	public Chunk(Chunk parent, Basis faceBasis, Vector3 center, float size, TerrainSettings settings, int index)
	{
		this.parent = parent;
		this.faceBasis = faceBasis;
		this.center = center;
		this.size = size;
		this.settings = settings;
		this.index = index;

		// Chunks are getting attached to the parent chunk:
		parent?.AddChild(this);

		// The shape mesh creator is a child of the chunk.
		shape = new ChunkShape(faceBasis, center, size, settings);
		AddChild(shape);

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

	/// <summary>
	/// Check the distance from the viwer (camera) to decide whether the chunk should be divided into sub-chunks or not.
	/// </summary>
	/// <param name="viewerPositionLocal">Camera position in the planet's local space (relative to the planet's origin point).</param>
	public void Update(Vector3 viewerPositionLocal)
	{
		float distanceSq = shape.centerA.DistanceSquaredTo(viewerPositionLocal);
		float hypetenuzeSq = Mathf.Pow(size, 2);

		// When the distance is compared to the chunk hypotenuze*2, it gives the most consistent result where
		// two neighboring chunks with different detail levels always have difference as 2x (seems like always).
		if (distanceSq <= hypetenuzeSq * 2 && size > settings.chunkSize)
		{
			if (children.Length == 0)
				CreateChildren();
			foreach (Chunk child in children)
				child.Update(viewerPositionLocal);
		}
		else
			shape.CreateAsync(OnReady);
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
		// Forward left:
		childCenter = center - faceBasis.x * size * 0.25f + faceBasis.z * size * 0.25f;
		children[0] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 0);
		// Forward right:
		childCenter = center + faceBasis.x * size * 0.25f + faceBasis.z * size * 0.25f;
		children[1] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 1);
		// Back left:
		childCenter = center - faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[3] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 2);
		// Back right:
		childCenter = center + faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[2] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 3);
	}
}