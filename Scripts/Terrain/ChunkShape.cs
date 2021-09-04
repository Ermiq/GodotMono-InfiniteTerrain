using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ChunkShape : MeshInstance
{
	Vector3[] vertices;
	int[] indices;
	Vector3[] normals;
	int detail;

	Godot.Collections.Array mesh_arrays;
	ArrayMesh arrayMesh;
	CollisionShape collision;
	ConcavePolygonShape shape;

	Vector3 center;
	float size;

	/// <summary>
	/// This is a visual representation of a chunk. Basically a MeshInstance with terrain vertices generation functions.
	/// </summary>
	/// <param name="center"></param>
	/// <param name="size"></param>
	public ChunkShape(Vector3 center, float size)
	{
		this.center = center;
		this.size = size;

		this.detail = World.detail;

		vertices = new Vector3[0];
		indices = new int[0];
		normals = new Vector3[0];

		arrayMesh = new ArrayMesh();
		Mesh = arrayMesh;

		StaticBody staticBody = new StaticBody();
		AddChild(staticBody);
		collision = new CollisionShape();
		shape = new ConcavePolygonShape();
		collision.Shape = shape;
		staticBody.AddChild(collision);
	}

	public void Create()
	{
		if (vertices.Length == 0)
		{
			GenerateQuads();
			CreateSurface();
			Mesh = arrayMesh;
			if (size <= World.chunkSize)
				shape.SetDeferred("data", arrayMesh.GetFaces());
		}
	}

	public void Remove()
	{
		vertices = new Vector3[0];
		indices = new int[0];
		normals = new Vector3[0];
		// Remove previous surface from the previous arrayMesh:
		if (arrayMesh.GetSurfaceCount() != 0)
			arrayMesh.SurfaceRemove(0);
		Mesh = arrayMesh;
		shape.SetDeferred("data", null);
	}

	// Chunks will be made of 2 triangle quads
	void GenerateQuads()
	{
		// Each quad has 4 corner vertices:
		int verticesAmount = (detail + 1) * (detail + 1);
		// Each quad consists of 2 triangles
		int indicesAmount = detail * detail * 2 * 3;
		/* Edge quads will have 2 additional vertices and 2 additional triangles,
		so, we need to increase the arrays sizes. */
		verticesAmount += (detail * detail) * 2;
		indicesAmount += (detail * detail) * 2 * 3;

		vertices = new Vector3[verticesAmount];
		indices = new int[indicesAmount];
		normals = new Vector3[verticesAmount];

		float sizeS = size * 2f + size * 2f / detail * 2f;
		int detailS = detail + 3;

		int vInd = 0, iInd = 0;
		for (int z = 0; z < detailS; z++)
		{
			for (int x = 0; x < detailS; x++)
			{
				Vector2 percent = new Vector2(x, z) / (detailS - 1);
				vertices[vInd] = center
					+ (percent.x - 0.5f) * Vector3.Right * sizeS
					- (percent.y - 0.5f) * Vector3.Forward * sizeS;

				if (x < detailS - 1 && z < detailS - 1)
				{
					indices[iInd] = vInd;
					indices[iInd + 1] = vInd + detailS + 1;
					indices[iInd + 2] = vInd + detailS;
					indices[iInd + 3] = vInd;
					indices[iInd + 4] = vInd + 1;
					indices[iInd + 5] = vInd + detailS + 1;
					iInd += 6;
				}
				vInd++;
			}
		}
	}

	void CreateSurface()
	{
		// Apply noise:
		for (int v = 0; v < vertices.Length; v++)
		{
			Vector3 vertex = vertices[v];
			vertex = World.EvaluatePosition(vertex);
			vertices[v] = vertex;
		}

		// Calculate normals:
		for (int i = 0; i < indices.Length; i += 3)
		{
			int i1 = indices[i];
			int i2 = indices[i + 1];
			int i3 = indices[i + 2];
			Vector3 a = vertices[i1];
			Vector3 b = vertices[i2];
			Vector3 c = vertices[i3];
			// Smooth out normals for all vertices by averaging them with other vertices of the same face triangle:
			Vector3 norm = -(b - a).Cross(c - a);
			normals[i1] += norm;
			normals[i2] += norm;
			normals[i3] += norm;
		}
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = normals[i].Normalized();
		}

		// Lower skirt vertices to disguise the seam stiches.
		// Note that we use it after the normals calculations. This way the chunks borders will be almost invisible
		// due to the skirt normals being calculated as if they were aligned with the rest of the terrain.
		LowerSkirts();

		// Prepare mesh arrays:
		mesh_arrays = new Godot.Collections.Array();
		mesh_arrays.Resize(9);
		mesh_arrays[0] = vertices;
		mesh_arrays[1] = normals;
		mesh_arrays[8] = indices;

		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh_arrays);
		arrayMesh.SurfaceSetMaterial(0, World.material);
		mesh_arrays.Clear();
	}

	void LowerSkirts()
	{
		for (int i = 0; i < (detail + 3) * (detail + 3); i += detail + 3)
		{
			vertices[i].y -= 30f;
			vertices[i + detail + 2].y -= 30f;
		}
		for (int i = 1; i < detail + 2; i++)
		{
			vertices[i].y -= 30f;
			vertices[i + (detail + 3) * (detail + 2)].y -= 30f;
		}
	}
}