using Godot;
using System.Collections.Generic;

public class Chunk : MeshInstance
{
	Chunk[] children;
	bool[] childrenReady;
	ChunkShape shape;
	bool divide;

	Chunk parent;
	Basis faceBasis;
	Vector3 centerC;
	float size;
	TerrainSettings settings;
	int index;

	public Chunk(Chunk parent, Basis faceBasis, Vector3 centerC, float size, TerrainSettings settings, int index)
	{
		this.parent = parent;
		this.faceBasis = faceBasis;
		this.centerC = centerC;
		this.size = size;
		this.settings = settings;
		this.index = index;

		// Chunks are getting attached to the parent chunk:
		parent?.AddChild(this);

		// The shape mesh creator is a child of the chunk.
		shape = new ChunkShape(faceBasis, centerC, size, settings);
		AddChild(shape);
		// To prevent the floating precision related distortion we setup the shape mesh position as offset from the world origin.
		// So the mesh vertices will be positioned relative to this chunk center position instead of the position from the world origin.
		// The offset from the chunk center is not too high and fits in the float precision limits, therefore no camera render distortion.
		shape.Translation = centerC;

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
	public void Check(Vector3 viewerPositionLocal)
	{
		float distanceSq = (shape.centerA + centerC).DistanceSquaredTo(viewerPositionLocal);
		float hypetenuzeSq = Mathf.Pow(size, 2) * 2;
		
		// When the distance is compared to the chunk hypotenuze*2, it gives the most consistent result where
		// two neighboring chunks with different detail levels always have difference as 2x (seems like always).
		if (distanceSq <= hypetenuzeSq * 2 && size > settings.chunkSize)
		{
			divide = true;
			if (children.Length == 0)
				CreateChildren();
			foreach (Chunk child in children)
				child.Check(viewerPositionLocal);
		}
		else
			divide = false;
	}

	/// <summary>
	/// If the chunk has children sub-chunks, they will be recursively updated.
	/// If the chunk has no children, the shape mesh will be generated.
	/// </summary>
	public void Update()
	{
		if (divide && children.Length > 0)
		{
			foreach (Chunk child in children)
				child.Update();
		}
		else
		{
			if (!shape.isInProcess && !shape.isCreated)
				shape.Create(OnReady);
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
		children[0] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 0);
		// Top right:
		childCenter = centerC + faceBasis.x * size * 0.25f + faceBasis.z * size * 0.25f;
		children[1] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 1);
		// Bottom right:
		childCenter = centerC + faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[2] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 2);
		// Bottom left:
		childCenter = centerC - faceBasis.x * size * 0.25f - faceBasis.z * size * 0.25f;
		children[3] = new Chunk(this, faceBasis, childCenter, size * 0.5f, settings, 3);
	}
}