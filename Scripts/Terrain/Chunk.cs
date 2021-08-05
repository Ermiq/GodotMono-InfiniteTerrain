using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public enum SeamSide
{
	NONE, TOP, RIGHT, BOTTOM, LEFT
}

public class Chunk
{
	public List<Vector3> vertices { get; private set; }

	public Vector2 index;
	int detail;
	Quad[] quads;
	SeamSide seamSide = SeamSide.NONE;
	int[] seamQuads;

	Task task;

	public Chunk(Vector2 index, float size, int detail, bool addSeams)
	{
		if (detail <= 0)
		{
			return;
		}
		this.index = index;
		this.detail = detail;
		if (addSeams)
			this.seamSide = GetSeamSide();
		else
			this.seamSide = SeamSide.NONE;

		vertices = new List<Vector3>();
		InitQuads(size);
		foreach (Quad quad in quads)
		{
			vertices.AddRange(quad.vertices);
		}
	}

	void InitQuads(float size)
	{
		quads = new Quad[detail * detail];
		seamQuads = GetEdgeQuads();
		
		Vector3 center;
		// Calculate half size of the quad's edge. We'll use it to get the quad center position.
		float quadHalfSize = size / (float)detail * 0.5f;

		for (int z = 0; z < detail; z++)
		{
			for (int x = 0; x < detail; x++)
			{
				// Each quad center is shifted by its x/z index.
				// E.g., the 2nd quad's X coord = meshCenter - meshHalfSize + quadHalfSize + 1 quadFullSize (index x = 1).
				// The quad which is at index x = 1, index z = 2 has coords:
				// X = (meshCenter - meshHalfSize + quadHalfSize + 1 quadFullSize) by X axis  
				// Z = (meshCenter - meshHalfSize + quadHalfSize + 2 quadFullSize) by Z axis
				center =
					new Vector3(index.x * size, 0, index.y * size)
					+
					new Vector3(
					(size * -0.5f + quadHalfSize) + x * (quadHalfSize * 2f),
					0,
					(size * -0.5f + quadHalfSize) + z * (quadHalfSize * 2f));

				quads[detail * z + x] = new Quad(GetQuadSeamSide(detail * z + x), center, quadHalfSize);
			}
		}
	}

	SeamSide GetSeamSide()
	{
		if (index == Vector2.Up)
			return SeamSide.BOTTOM;
		else if (index == Vector2.Right)
			return SeamSide.LEFT;
		else if (index == Vector2.Down)
			return SeamSide.TOP;
		else if (index == Vector2.Left)
			return SeamSide.RIGHT;
		else return SeamSide.NONE;
	}

	int[] GetEdgeQuads()
	{
		int[] result = new int[detail];
		int count = 0, start = 0, end = 0, step = 0;

		switch (seamSide)
		{
			case SeamSide.TOP:
				start = 0;
				end = detail;
				step = 1;
				break;
			case SeamSide.RIGHT:
				start = detail - 1;
				end = detail * detail;
				step = detail;
				break;
			case SeamSide.BOTTOM:
				start = detail * detail - detail;
				end = detail * detail;
				step = 1;
				break;
			case SeamSide.LEFT:
				start = 0;
				end = detail * detail - detail;
				step = detail;
				break;
		}
		for (int i = start; i < end; i += step)
		{
			result[count] = i;
			count++;
		}
		return result;
	}

	SeamSide GetQuadSeamSide(int quadIndex)
	{
		foreach (int i in seamQuads)
		{
			if (i == quadIndex)
				return seamSide;
		}
		return SeamSide.NONE;
	}
}