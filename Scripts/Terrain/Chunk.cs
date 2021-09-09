using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Chunk : MeshInstance
{
	Chunk[] children;
	bool isRoot;
	ChunkShape shape;

	Vector3 center, centerE;
	float size;

	public Chunk(Vector3 center, float size, bool isRoot = false)
	{
		this.center = center;
		this.size = size;
		this.isRoot = isRoot;

		centerE = World.EvaluatePosition(center);

		children = new Chunk[0];

		shape = new ChunkShape(center, size, size <= World.chunkSize);
		AddChild(shape);
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

	public void Update(Vector3 viewerPosition)
	{
		float distance = centerE.DistanceSquaredTo(viewerPosition);
		if (distance <= Mathf.Pow(size * 3f, 2) && size > World.chunkSize)
		{
			Divide(viewerPosition);
		}
		else
		{
			Merge();
		}
	}

	void Divide(Vector3 viewerPositionLocal)
	{
		if (children.Length == 0)
		{
			children = new Chunk[4];

			Vector3 chunkCenter;
			chunkCenter = center - Vector3.Right * size * 0.5f + Vector3.Forward * size * 0.5f;
			children[0] = new Chunk(chunkCenter, size * 0.5f);
			AddChild(children[0]);
			chunkCenter = center + Vector3.Right * size * 0.5f + Vector3.Forward * size * 0.5f;
			children[1] = new Chunk(chunkCenter, size * 0.5f);
			AddChild(children[1]);
			chunkCenter = center + Vector3.Right * size * 0.5f - Vector3.Forward * size * 0.5f;
			children[2] = new Chunk(chunkCenter, size * 0.5f);
			AddChild(children[2]);
			chunkCenter = center - Vector3.Right * size * 0.5f - Vector3.Forward * size * 0.5f;
			children[3] = new Chunk(chunkCenter, size * 0.5f);
			AddChild(children[3]);
		}

		bool isAllChildrenUpToDate = true;
		foreach (Chunk child in children)
		{
			child.Update(viewerPositionLocal);
			if (!child.shape.IsUpToDate) isAllChildrenUpToDate = false;
		}
		if (isAllChildrenUpToDate)
		{
			shape.Remove();
		}
	}

	void Merge()
	{
		if (!shape.IsUpToDate)
			shape.Create();
		else
		{
			foreach (Chunk child in children)
			{
				child.QueueFree();
			}
			children = new Chunk[0];
		}
	}
}