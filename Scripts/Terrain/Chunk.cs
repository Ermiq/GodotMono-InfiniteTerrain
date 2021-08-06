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
	public Vector3 prePosition { get; private set; }

	MeshInstance mesh_instance1;
	MeshInstance mesh_instance2;
	CollisionShape collision;
	ConcavePolygonShape shape1;
	ConcavePolygonShape shape2;
	MeshInstance mesh_instanceCurrent;
	SurfaceTool surfaceTool;
	OpenSimplexNoise noise;
	Material material;
	float size;
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
		this.detail = detail;// (int)Mathf.Pow(2, detail);
		this.addCollision = addCollision;
		
		if (!addCollision)
			this.seamSide = GetSeamSide();
		else
			this.seamSide = SeamSide.NONE;

		surfaceTool = new SurfaceTool();
		
		mesh_instance1 = new MeshInstance();
		mesh_instance1.Visible = true;
		mesh_instance1.CastShadow = GeometryInstance.ShadowCastingSetting.DoubleSided;
		AddChild(mesh_instance1);

		mesh_instance2 = new MeshInstance();
		mesh_instance2.Visible = false;
		mesh_instance2.CastShadow = GeometryInstance.ShadowCastingSetting.DoubleSided;
		AddChild(mesh_instance2);

		StaticBody staticBody = new StaticBody();
		AddChild(staticBody);
		collision = new CollisionShape();
		shape1 = new ConcavePolygonShape();
		shape2 = new ConcavePolygonShape();
		staticBody.AddChild(collision);

		mesh_instanceCurrent = mesh_instance1;

		InitQuads();
	}

	void InitQuads()
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
				center = new Vector3(
					(size * -0.5f + quadHalfSize) + x * (quadHalfSize * 2f),
					0,
					(size * -0.5f + quadHalfSize) + z * (quadHalfSize * 2f));

				quads[detail * z + x] = new Quad(GetQuadSeamSide(detail * z + x), center, quadHalfSize);
			}
		}
	}

	public void Prepair(float x, float z)
	{
		prePosition = new Vector3(index.x * size + x, 0, index.y * size + z);
		Generate();
	}

	public void PrepairAsync(float x, float z)
	{
		prePosition = new Vector3(index.x * size + x, 0, index.y * size + z);
		task = Task.Run(Generate);
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
	void Generate()
	{
		if (detail <= 0)
		{
			return;
		}

		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.AddSmoothGroup(true);//(addCollision);

		foreach (Quad quad in quads)
		{
			for (int v = 0; v < quad.vertices.Length; v++)
			{
				AddNoise(ref quad.vertices[v]);
				surfaceTool.AddVertex(quad.vertices[v]);
			}
		}
		
		surfaceTool.SetMaterial(material);
		surfaceTool.GenerateNormals();

		// Generate a mesh instance data:
		GetTheOtherMeshInstance().Mesh = surfaceTool.Commit();
		if (addCollision)
		{
			GetTheOtherShape().Data = GetTheOtherMeshInstance().Mesh.GetFaces();
		}

		surfaceTool.Clear();
		seamQuads = null;
	}

	MeshInstance GetTheOtherMeshInstance()
	{
		if (mesh_instanceCurrent == mesh_instance1)
			return mesh_instance2;
		else
			return mesh_instance1;
	}

	ConcavePolygonShape GetTheOtherShape()
	{
		if (mesh_instanceCurrent == mesh_instance1)
			return shape2;
		else
			return shape1;
	}

	public void Apply()
	{
		collision.Shape = GetTheOtherShape();
		mesh_instanceCurrent.Visible = false;
		mesh_instanceCurrent = GetTheOtherMeshInstance();
		mesh_instanceCurrent.Visible = true;
		
		Translation = prePosition;
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

	void AddNoise(ref Vector3 vertex)
	{
		if (noise == null)
			return;
		vertex.y = noise.GetNoise2d(vertex.x + prePosition.x, vertex.z + prePosition.z);
		vertex.y *= vertex.y < 0.5f ? Mathf.Pow(vertex.y * 2, 2) / 2f : 1 - (Mathf.Pow((1 - vertex.y) * 2, 2) / 2f);
		vertex.y += 0.01f;
		vertex.y *= 1000f;
	}
}