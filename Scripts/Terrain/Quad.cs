using Godot;
using System;

public class Quad
{
	public Vector3[] vertices { get; private set; }

	// simplified = true -> quad is made of 2 triangles (if the quad is not a seam side quad)
	// simplified = false -> quad is made of 4 triangles
	bool simplified = false;

	public Quad(SeamSide seamSide, Vector3 center, float halfSize)
	{
		vertices = new Vector3[seamSide == SeamSide.NONE ? 12 : 18];

		// Get 4 vertices of the quad (they relate to the center vertex):
		Vector3 bottomLeft = center + new Vector3(-halfSize, 0, halfSize);
		Vector3 topLeft = center + new Vector3(-halfSize, 0, -halfSize);
		Vector3 topRight = center + new Vector3(halfSize, 0, -halfSize);
		Vector3 bottomRight = center + new Vector3(halfSize, 0, halfSize);

		int vertexIndex = 0;
		if (simplified && seamSide == SeamSide.NONE)
		{
			// 1st triangle:
			AddTriangle(ref vertexIndex, bottomLeft, topLeft, topRight, false);
			// 2nd triangle:
			AddTriangle(ref vertexIndex, topRight, bottomRight, bottomLeft, false);
		}
		else
		{
			// 1. Top triangle:
			AddTriangle(ref vertexIndex, center, topLeft, topRight, seamSide == SeamSide.TOP);
			// 2. Right triangle:
			AddTriangle(ref vertexIndex, center, topRight, bottomRight, seamSide == SeamSide.RIGHT);
			// 3. Bottom triangle:
			AddTriangle(ref vertexIndex, center, bottomRight, bottomLeft, seamSide == SeamSide.BOTTOM);
			// 4. Left triangle:
			AddTriangle(ref vertexIndex, center, bottomLeft, topLeft, seamSide == SeamSide.LEFT);
		}
	}

	void AddTriangle(ref int index, Vector3 center, Vector3 corner1, Vector3 corner2, bool addSeam)
	{
		if (addSeam)
		{
			Vector3 seamVertex1 = corner1 + (corner2 - corner1) * 0.333333f;
			Vector3 seamVertex2 = corner1 + (corner2 - corner1) * 0.666666f;

			vertices[index] = center;
			vertices[index + 1] = corner1;
			vertices[index + 2] = seamVertex1;
			vertices[index + 3] = center;
			vertices[index + 4] = seamVertex1;
			vertices[index + 5] = seamVertex2;
			vertices[index + 6] = center;
			vertices[index + 7] = seamVertex2;
			vertices[index + 8] = corner2;
			index += 9;
		}
		else
		{
			vertices[index] = center;
			vertices[index + 1] = corner1;
			vertices[index + 2] = corner2;
			index += 3;
		}
	}
}