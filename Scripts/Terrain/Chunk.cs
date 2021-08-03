using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public enum SeamSide
{
	TOP, RIGHT, BOTTOM, LEFT, NONE = -1
}

public class Chunk : Spatial
{
	public Vector2 index;

	MeshInstance mesh_instance;
	SurfaceTool surfaceTool;
	OpenSimplexNoise noise;
	Material material;
	float size;
	int detail;
	List<Vector3> vertices;
	SeamSide seamSide = SeamSide.NONE;
	List<int> seamQuads;

	Task task;

	public Chunk(OpenSimplexNoise noise, Material material, Vector2 index, float size, int detail)
	{
		this.noise = noise;
		this.material = material;
		this.index = index;
		this.size = size;
		this.detail = detail;
		this.seamSide = GetSeamSide();
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
		if (detail <= 0)
		{
			return;
		}

		seamQuads = new List<int>();
		seamQuads.AddRange(GetEdgeQuads());

		vertices = new List<Vector3>();

		Vector3 center;
		SeamSide quadSeamSide;
		Quad quad;

		// Calculate half size of the quad's edge. We'll use it to get the quad center position.
		float quadHalfSize = size / (float)detail * 0.499f;

		for (int z = 0; z < detail; z++)
		{
			for (int x = 0; x < detail; x++)
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

				quadSeamSide = seamQuads.Contains(detail * z + x) ? seamSide : SeamSide.NONE;
				quad = new Quad(quadSeamSide, center, quadHalfSize);
				vertices.AddRange(quad.vertices);
			}
		}

		surfaceTool = new SurfaceTool();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		// Send the vertices to the SurfaceTool:
		for (int v = 0; v < vertices.Count; v++)
		{
			Vector3 vertex = vertices[v];
			ApplyYNoise(ref vertex);
			surfaceTool.AddVertex(vertex);
		}
		surfaceTool.SetMaterial(material);
		surfaceTool.GenerateNormals();

		ApplyToMesh();
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

	void ApplyYNoise(ref Vector3 vertex)
	{
		vertex.y = noise.GetNoise2d(vertex.x + Translation.x, vertex.z + Translation.z) * 80f;
	}
}