using Godot;
using System;
using System.Collections.Generic;

public class Ring : Spatial
{
	public Vector3 prePosition { get; private set; }

	int index;
	MeshInstance mesh_instance1;
	MeshInstance mesh_instance2;
	MeshInstance mesh_instanceCurrent;

	CollisionShape collision;
	ConcavePolygonShape shape1;
	ConcavePolygonShape shape2;

	SurfaceTool surfaceTool;
	OpenSimplexNoise noise;
	Material material;
	List<Chunk> chunks = new List<Chunk>();

	public Ring(int index, OpenSimplexNoise noise, Material material, float size, int detail, bool addCollision)
	{
		this.index = index;
		this.noise = noise;
		this.material = material;

		if (index == 1)
		{
			Chunk chunk = new Chunk(Vector2.Zero, size * 3, detail * 3, false);
			chunks.Add(chunk);
		}
		else
		{
			for (int j = -1; j < 2; j++)
			{
				for (int k = -1; k < 2; k++)
				{
					if (j == 0 && k == 0)
						continue;
					Chunk chunk = new Chunk(new Vector2(j, k), size, detail, true);
					chunks.Add(chunk);
				}
			}
		}

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
	}

	public void ShiftProcess(float offsetX, float offsetY)
	{
		prePosition = new Vector3(offsetX, 0, offsetY);

		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		surfaceTool.AddSmoothGroup(true);

		foreach (Chunk chunk in chunks)
		{
			foreach (Vector3 vertex in chunk.vertices)
			{
				Vector3 v = vertex;
				AddNoise(ref v);
				surfaceTool.AddVertex(v);
			}
		}

		surfaceTool.SetMaterial(material);
		surfaceTool.GenerateNormals();

		// Generate a mesh instance data:
		GetTheOtherMeshInstance().Mesh = surfaceTool.Commit();
		if (index == 1)
		{
			GetTheOtherShape().Data = GetTheOtherMeshInstance().Mesh.GetFaces();
		}

		surfaceTool.Clear();
	}

	public void ShiftApply()
	{
		collision.Shape = GetTheOtherShape();
		mesh_instanceCurrent.Visible = false;
		mesh_instanceCurrent = GetTheOtherMeshInstance();
		mesh_instanceCurrent.Visible = true;

		Translation = prePosition;
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

	void AddNoise(ref Vector3 vertex)
	{
		vertex.y = noise.GetNoise2d(vertex.x + prePosition.x, vertex.z + prePosition.z) * 80f;
	}
}