using Godot;
using System;

public class Quad
{
    public Vector3[] vertices { get; private set; }
    
	SeamSide seamSide = SeamSide.NONE;
	
	public Quad(SeamSide seamSide, Vector3 center, float halfSize)
	{
		this.seamSide = seamSide;
		vertices = new Vector3[seamSide == SeamSide.NONE ? 12 : 15];

		// Get 4 vertices of the quad (they relate to the center vertex):
		Vector3 bottomLeft = center + new Vector3(-halfSize, 0, halfSize);
		Vector3 topLeft = center + new Vector3(-halfSize, 0, -halfSize);
		Vector3 topRight = center + new Vector3(halfSize, 0, -halfSize);
		Vector3 bottomRight = center + new Vector3(halfSize, 0, halfSize);

		AddTriangles(center, bottomLeft, topLeft, topRight, bottomRight);
	}

	void AddTriangles(Vector3 center, Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight)
	{
        int vertexIndex = 0;
		// Add triangles (as a set of 3 vertices) to the array:
		// 1. Top triangle:
		AddTriangle(ref vertexIndex, center, topLeft, topRight, SeamSide.TOP);
		// 2. Right triangle:
		AddTriangle(ref vertexIndex, center, topRight, bottomRight, SeamSide.RIGHT);
		// 3. Bottom triangle:
		AddTriangle(ref vertexIndex, center, bottomRight, bottomLeft, SeamSide.BOTTOM);
		// 4. Left triangle:
		AddTriangle(ref vertexIndex, center, bottomLeft, topLeft, SeamSide.LEFT);
	}

	void AddTriangle(ref int index, Vector3 center, Vector3 corner1, Vector3 corner2, SeamSide side)
	{
		if (side == seamSide)
		{
			Vector3 seamVertex = (corner1 + corner2) * 0.5f;

			vertices[index] = center;
			vertices[index + 1] = corner1;
			vertices[index + 2] = seamVertex;
			vertices[index + 3] = center;
			vertices[index + 4] = seamVertex;
			vertices[index + 5] = corner2;
			index += 6;
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