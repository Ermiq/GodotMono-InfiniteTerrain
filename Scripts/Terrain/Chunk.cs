using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public enum SeamSide
{
	NONE, TOP, RIGHT, BOTTOM, LEFT
}

public class Chunk : Spatial
{
	public Vector2 index;
	public Vector3 position { get; private set; }

	ArrayMesh arrayMesh;
	MeshInstance mesh_instance;
	CollisionShape collision;
	ConcavePolygonShape shape;
	SurfaceTool surfaceTool;
	OpenSimplexNoise noise;
	Material material;
	float size;
	float sizeModifier = 1f;
	int detail;
	Quad[] quads;
	SeamSide seamSide = SeamSide.NONE;
	int[] seamQuads;
	bool addCollision;

	Task task;

	public Chunk(OpenSimplexNoise noise, Material material, Vector2 index, float size, int detail, bool addCollision = false)
	{
		this.noise = noise;
		this.material = material;
		this.index = index;
		this.size = size;
		this.detail = detail;
		this.addCollision = addCollision;
		
		if (!addCollision)
			this.seamSide = GetSeamSide();
		else
			this.seamSide = SeamSide.NONE;

		surfaceTool = new SurfaceTool();
		arrayMesh = new ArrayMesh();

		mesh_instance = new MeshInstance();
		mesh_instance.Visible = true;
		AddChild(mesh_instance);

		StaticBody staticBody = new StaticBody();
		AddChild(staticBody);
		collision = new CollisionShape();
		shape = new ConcavePolygonShape();
		collision.Shape = shape;
		staticBody.AddChild(collision);

		Translation = new Vector3(index.x * (size * sizeModifier), 0, index.y * (size * sizeModifier));

		InitQuads();
	}

	void InitQuads()
	{
		quads = new Quad[detail * detail];
		seamQuads = GetEdgeQuads();
		
		Vector3 center;
		// Calculate half size of the quad's edge. We'll use it to get the quad center position.
		float quadHalfSize = size * sizeModifier / (float)detail * 0.5f;

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
					(size * sizeModifier * -0.5f + quadHalfSize) + x * (quadHalfSize * 2f),
					0,
					(size * sizeModifier * -0.5f + quadHalfSize) + z * (quadHalfSize * 2f));

				quads[detail * z + x] = new Quad(GetQuadSeamSide(detail * z + x), center, quadHalfSize);
			}
		}
	}

	public void GenerateSurface(float offsetX = 0, float offsetY = 1, float offsetZ = 0)
	{
		if (sizeModifier != offsetY)
		{
			sizeModifier = offsetY;
			InitQuads();
		}
		position = new Vector3(index.x * (size * sizeModifier) + offsetX, 0, index.y * (size * sizeModifier) + offsetZ);
		Generate(offsetX, offsetZ);
	}

	// Create a mesh from quads. Each quad is made of 4 triangles (as splitted by 2 diagonal lines).
	void Generate(float offsetX, float offsetZ)
	{
		if (detail <= 0)
		{
			return;
		}

		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.AddSmoothGroup(true);

		foreach (Quad quad in quads)
		{
			for (int v = 0; v < quad.vertices.Length; v++)
			{
				Vector3 vertex = quad.vertices[v];
				AddNoise(ref vertex);
				surfaceTool.AddVertex(vertex);
			}
		}
		surfaceTool.SetMaterial(material);
		surfaceTool.GenerateNormals();

		// Generate a mesh instance data:
		arrayMesh = surfaceTool.Commit();
		surfaceTool.Clear();

		// This causes hick ups, but in a thread it causes crashes... so...
		if (addCollision)
		{
			shape.SetDeferred("data", arrayMesh.GetFaces());
		}
	}

	void ReapplyHeights(float offsetX, float offsetZ)
	{
		MeshDataTool mdt = new MeshDataTool();
		mdt.CreateFromSurface(arrayMesh, 0);
		for (int i = 0; i < mdt.GetVertexCount(); i++)
		{
			Vector3 vertex = mdt.GetVertex(i);
			AddNoise(ref vertex);
			mdt.SetVertex(i, vertex);
		}
		arrayMesh.SurfaceRemove(0);
		mdt.CommitToSurface(arrayMesh);

		// This causes hick ups, but in a thread it causes crashes... so...
		if (addCollision)
		{
			shape.SetDeferred("data", arrayMesh.GetFaces());
		}
	}

	public void Apply()
	{
		Translation = position;
		mesh_instance.Mesh = arrayMesh;
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
				end = detail * detail - detail + 1;
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

	SeamSide GetQuadSeamSide(int quadIndex)
	{
		foreach (int i in seamQuads)
		{
			if (i == quadIndex)
				return seamSide;
		}
		return SeamSide.NONE;
	}

	void AddNoise(ref Vector3 vertex)
	{
		if (noise == null)
			return;
		
		float n = noise.GetNoise2d(vertex.x + position.x, vertex.z + position.z);
		if (n > 0)
			n = Mathf.Pow(n, 2) * 1.5f;
		vertex.y = n * 3000f;
	}
}