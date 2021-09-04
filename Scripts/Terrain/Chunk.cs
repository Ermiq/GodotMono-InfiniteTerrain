using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Chunk : Spatial
{
	Chunk[] children;
	ChunkShape shape;

	Vector3 center, centerE;
	float size;
	bool isRoot;

	public Chunk(Vector3 center, float size, bool isRoot = false)
	{
		this.center = center;
		this.size = size;
		this.isRoot = isRoot;

		// The centerE is the chunk center vertex with noise applied to it.
		// We will use it to calculate the distance from the camera to the chunk's actual canter vertex.
		// The center (without E) stays a center point on flat XZ plane (y is 0).
		centerE = World.EvaluatePosition(center);
		children = new Chunk[0];

		shape = new ChunkShape(center, size);
		AddChild(shape);
	}

	public void Update(Vector3 viewerPosition)
	{
		float distance = centerE.DistanceSquaredTo(viewerPosition);
		if (distance <= Mathf.Pow(size * 2f, 2) && size > World.chunkSize)
		{
			Divide(viewerPosition);
		}
		else
		{
			Merge();
		}
	}

	void Divide(Vector3 viewerPosition)
	{
		// Divide is called when the distance to the camera is close enough for the chunk to be subdivided
		// So, create children chunks if there's no children yet.
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

		// Send the update signal to the children so they also could check the camera distance
		//and dicide whether they should be divided further or not:
		foreach (Chunk child in children)
		{
			child.Update(viewerPosition);
		}

		// Remove self shape, because the children now represent the terrain:
		shape.Remove();
	}

	void Merge()
	{
		// Merge is called when no further subdivision is needed, meaninf that this chunk is the most deep one
		// and its shape will represent the terrain:
		shape.Create();

		// Delete any children:
		if (children.Length > 0)
		{
			foreach (Chunk child in children)
			{
				child.QueueFree();
			}
			children = new Chunk[0];
		}
	}

	// Not used currently:
	Chunk[] GetAllChildren()
	{
		List<Chunk> result = new List<Chunk>();
		if (children.Length > 0)
			foreach (Chunk child in children)
				result.AddRange(child.GetAllChildren());
		else
			result.Add(this);
		return result.ToArray();
	}
}