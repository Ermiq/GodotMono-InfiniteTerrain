using Godot;
using System;
using System.Collections.Generic;

public class Chunk : Spatial
{
	public bool isBusy = false;
	public int x;
	public int z;
	public int detail { get; set; }

	MeshInstance mesh_instance;
	OpenSimplexNoise noise;
	float size;
	int quadsInRow;
	Vector3[] vertices;

	public Chunk(OpenSimplexNoise noise, int x, int z, float size)
	{
		this.noise = noise;
		this.x = x;
		this.z = z;
		this.size = size;

		mesh_instance = new MeshInstance();
		mesh_instance.Name = "mesh";
		AddChild(mesh_instance);
	}

	public void SetDetail(int detail)
	{
		this.detail = detail;
		quadsInRow = (int)Mathf.Pow(2, detail);
	}

	// Create a mesh from quads. Each quad is made of 4 triangles (as splitted by 2 diagonal lines).
	public void Generate(object[] arr = null)
	{
		int verticesAmount = (int)Mathf.Pow(quadsInRow, 2) * 12; // 12 vertices in each quad
		vertices = new Vector3[verticesAmount];
		int vertexIndex = 0;

		// Calculate a half size of a quad edge.
		float quadHalfSize = (float)size / (float)quadsInRow * 0.5f;
		// Get the starting position from which the quads generation will begin.
		// This position is the center of the first (left-top) quad.
		Vector3 center, bottomLeft, topLeft, topRight, bottomRight;
		for (int x = 0; x < quadsInRow; x++)
		{
			for (int z = 0; z < quadsInRow; z++)
			{
				// Each quad center is shifted by its x/z index.
				// E.g., the 2nd quad's X coord = meshCenter - meshHalfSize + quadHalfSize + 1 quadFullSize (index x = 1).
				// The quad which is at index x = 1, index z = 2 has coords:
				// X = (meshCenter - meshHalfSize + quadHalfSize + 1 quadFullSize) by X axis  
				// Z = (meshCenter - meshHalfSize + quadHalfSize + 2 quadFullSize) by Z axis
				center = new Vector3(
					(size * -0.5f + quadHalfSize) + x * (quadHalfSize * 2f),
					0,
					(size * -0.5f + quadHalfSize) + z * (quadHalfSize * 2f));
				// Get 4 vertices of the quad (they relate to the center vertex):
				bottomLeft = center + new Vector3(-quadHalfSize, 0, quadHalfSize);
				topLeft = center + new Vector3(-quadHalfSize, 0, -quadHalfSize);
				topRight = center + new Vector3(quadHalfSize, 0, -quadHalfSize);
				bottomRight = center + new Vector3(quadHalfSize, 0, quadHalfSize);

				ApplyYNoise(ref center);
				ApplyYNoise(ref bottomLeft);
				ApplyYNoise(ref topLeft);
				ApplyYNoise(ref topRight);
				ApplyYNoise(ref bottomRight);

				// Add triangles (as a set of 3 vertices) to the array:
				// 1. Left bottom triangle:
				vertices[vertexIndex] = center;
				vertices[vertexIndex + 1] = bottomLeft;
				vertices[vertexIndex + 2] = topLeft;
				// 2. Left top triangle:
				vertices[vertexIndex + 3] = center;
				vertices[vertexIndex + 4] = topLeft;
				vertices[vertexIndex + 5] = topRight;
				// 3. Right top triangle:
				vertices[vertexIndex + 6] = center;
				vertices[vertexIndex + 7] = topRight;
				vertices[vertexIndex + 8] = bottomRight;
				// 4. Right bottom triangle:
				vertices[vertexIndex + 9] = center;
				vertices[vertexIndex + 10] = bottomRight;
				vertices[vertexIndex + 11] = bottomLeft;
				vertexIndex += 12;
			}
		}

		var surface_tool = new SurfaceTool();
		surface_tool.Begin(Mesh.PrimitiveType.Triangles);

		// Send the vertices to the SurfaceTool:
		for (int v = 0; v < vertices.Length; v++)
		{
			surface_tool.AddVertex(vertices[v]);
		}
		surface_tool.SetMaterial(ResourceLoader.Load("res://Terrain.material") as Material);
		surface_tool.GenerateNormals();

		RemoveChild(mesh_instance);
		mesh_instance = new MeshInstance();
		AddChild(mesh_instance);

		// Generate a mesh instance data:
		mesh_instance.Mesh = surface_tool.Commit();
		mesh_instance.CreateTrimeshCollision();
		mesh_instance.CastShadow = GeometryInstance.ShadowCastingSetting.DoubleSided;
	}

	void ApplyYNoise(ref Vector3 vertex)
	{
		vertex.y = noise.GetNoise2d(vertex.x + x * size, vertex.z + z * size) * 80f;
	}
}