using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Chunk : Spatial
{
	public bool isBusy = false;
	public int x;
	public int z;
	public int detail { get; set; }

	MeshInstance mesh_instance;
	SurfaceTool surfaceTool;
	OpenSimplexNoise noise;
	Material material;
	float size;
	int quadsInRow;
	Vector3[] vertices;
	int[] neighboursDetail = new int[4];

	Task task;

	public Chunk(OpenSimplexNoise noise, Material material, int x, int z, float size)
	{
		this.noise = noise;
		this.material = material;
		this.x = x;
		this.z = z;
		this.size = size;
	}

	public void SetDetail(int detail, int[] neighboursDetail)
	{
		this.detail = detail;
		quadsInRow = (int)Mathf.Pow(2, detail);
		this.neighboursDetail = neighboursDetail;
	}

	// Create a mesh from quads. Each quad is made of 4 triangles (as splitted by 2 diagonal lines).
	public void GenerateAsync()
	{
		task = Task.Run(StartGeneration);
	}

	public void Generate()
	{
		StartGeneration();
	}

	void StartGeneration()
	{
		int seamSide = -1;
		for (int i = 0; i < 4; i++)
		{
			if (neighboursDetail[i] < detail)
				seamSide = i;
		}

		int verticesAmount = (int)Mathf.Pow(quadsInRow, 2) * 12; // 12 vertices in each quad
		vertices = new Vector3[verticesAmount];
		int vertexIndex = 0;

		// Calculate a half size of a quad edge.
		float quadHalfSize = (float)size / (float)quadsInRow * 0.499f;
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

				AddVerticesAsNormalQuad(ref vertexIndex, center, bottomLeft, topLeft, topRight, bottomRight);
			}
		}

		surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		// Send the vertices to the SurfaceTool:
		for (int v = 0; v < vertices.Length; v++)
		{
			surfaceTool.AddVertex(vertices[v]);
		}
		surfaceTool.SetMaterial(material);
		surfaceTool.GenerateNormals();

		ApplyToMesh();
	}

	void AddVerticesAsNormalQuad(ref int index, Vector3 center, Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight)
	{
		// Add triangles (as a set of 3 vertices) to the array:
		// 1. Left bottom triangle:
		vertices[index] = center;
		vertices[index + 1] = bottomLeft;
		vertices[index + 2] = topLeft;
		// 2. Left top triangle:
		vertices[index + 3] = center;
		vertices[index + 4] = topLeft;
		vertices[index + 5] = topRight;
		// 3. Right top triangle:
		vertices[index + 6] = center;
		vertices[index + 7] = topRight;
		vertices[index + 8] = bottomRight;
		// 4. Right bottom triangle:
		vertices[index + 9] = center;
		vertices[index + 10] = bottomRight;
		vertices[index + 11] = bottomLeft;
		index += 12;
	}

	MeshInstance ApplyToMesh()
	{
		MeshInstance newMesh = new MeshInstance();
		// Generate a mesh instance data:
		newMesh.Mesh = surfaceTool.Commit();
		newMesh.CreateTrimeshCollision();
		newMesh.CastShadow = GeometryInstance.ShadowCastingSetting.DoubleSided;

		if (mesh_instance != null)
			RemoveChild(mesh_instance);
		AddChild(newMesh);
		mesh_instance = newMesh;

		return newMesh;
	}

	void ApplyYNoise(ref Vector3 vertex)
	{
		vertex.y = noise.GetNoise2d(vertex.x + x * size, vertex.z + z * size) * 80f;
	}
}