using Godot;
using System;
using System.Collections.Generic;

[Tool]
public class Ring : Spatial
{
	Chunk[] chunks;
	Vector3 position;

	ArrayMesh arrayMesh;
	MeshInstance mesh_instance;
	CollisionShape collision;
	ConcavePolygonShape shape;
	SurfaceTool surfaceTool;

	OpenSimplexNoise noise;
	Material material;
	bool addCollision;

	public Ring(int index, OpenSimplexNoise noise, Material material, float size, int detail, bool addCollision)
	{
		this.noise = noise;
		this.material = material;
		this.addCollision = addCollision;

		surfaceTool = new SurfaceTool();
		arrayMesh = new ArrayMesh();

		mesh_instance = new MeshInstance();
		AddChild(mesh_instance);

		if (addCollision)
		{
			StaticBody staticBody = new StaticBody();
			AddChild(staticBody);
			collision = new CollisionShape();
			shape = new ConcavePolygonShape();
			collision.Shape = shape;
			staticBody.AddChild(collision);
		}

		if (index == 1)
		{
			chunks = new Chunk[1] {
				new Chunk(Vector2.Zero, size * 3, detail, false)
			};
		}
		else
		{
			chunks = new Chunk[8];
			int count = 0;
			for (int j = -1; j < 2; j++)
			{
				for (int k = -1; k < 2; k++)
				{
					if (j == 0 && k == 0)
						continue;

					Chunk chunk = new Chunk(new Vector2(j, k), size, detail, index > 2);
					chunks[count] = chunk;
					count++;
				}
			}
		}

		ShiftProcess(Vector3.Zero);
		ShiftApply();
	}

	// Surface generation using the SurfaceTool. Easy to use, but works slowly and eats a lot of memory.
	void GenerateSurface()
	{
		int oldSurfacesCount = arrayMesh.GetSurfaceCount();
		foreach (Chunk chunk in chunks)
		{
			surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
			surfaceTool.AddSmoothGroup(true);

			for (int v = 0; v < chunk.vertices.Length; v++)
			{
				Vector3 vertex = chunk.vertices[v];
				AddNoise(ref vertex);
				surfaceTool.AddVertex(vertex);
			}
			for (int i = 0; i < chunk.indices.Length; i++)
			{
				surfaceTool.AddIndex(chunk.indices[i]);
			}

			surfaceTool.SetMaterial(material);
			surfaceTool.GenerateNormals();

			surfaceTool.Commit(arrayMesh);
			surfaceTool.Clear();
		}
		if (oldSurfacesCount > 0)
		{
			for (int i = 0; i < oldSurfacesCount; i++)
				arrayMesh.SurfaceRemove(0);
		}

		// The deferred call causes hick ups, but when the Shape.Data is set from a thread it causes crashes... so...
		if (addCollision)
			shape.SetDeferred("data", arrayMesh.GetFaces());
	}

	// 'Manual' creation of the surface with mesh data arrays. Approximately 2x times faster than SurfaceTool and takes 2x times less memory.
	void GenerateSurfaceArrays()
	{
		List<Vector3> vertices = new List<Vector3>();
		List<int> indices = new List<int>();
		
		// Collect vertices and indexes from all chunks
		foreach (Chunk c in chunks)
		{
			for (int i = 0; i < c.indices.Length; i++)
			{
				// The chunk indices are sort of 'in local space', meaning that it's index=1 means 2nd vector in the chunk's local vertices array,
				// but here the ring has another vector at index 1 (because the ring vertices holds the vectors from several chunks).
				// So, we need to adjust the index to take into account the indexes of previously stored chunks by adding current vertices.Count
				// to the new indexes being added:
				indices.Add(c.indices[i] + vertices.Count);
			}
			for (int v = 0; v < c.vertices.Length; v++)
			{
				Vector3 vertex = c.vertices[v];
				AddNoise(ref vertex);
				vertices.Add(vertex);
			}
		}

		// Calculate normals:
		Vector3[] normals = new Vector3[vertices.Count];
		for (int i = 0; i < indices.Count; i += 3)
		{
			var vi_a = indices[i];
			var vi_b = indices[i + 1];
			var vi_c = indices[i + 2];
			var a = vertices[vi_a];
			var b = vertices[vi_b];
			var c = vertices[vi_c];
			// Smooth out normals for all vertices by averaging them with other vertices of the same face triangle:
			var norm = -(b - a).Cross(c - a);
			normals[vi_a] += norm;
			normals[vi_b] += norm;
			normals[vi_c] += norm;
		}
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = normals[i].Normalized();
		}

		// Prepare mesh arrays:
		var mesh_arrays = new Godot.Collections.Array();
		mesh_arrays.Resize(9);
		mesh_arrays[0] = vertices.ToArray();
		mesh_arrays[1] = normals;
		mesh_arrays[8] = indices.ToArray();

		// Remove previous surface
		if (arrayMesh.GetSurfaceCount() != 0)
			arrayMesh.SurfaceRemove(0);
		
		// Apply surface:
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh_arrays);
		arrayMesh.SurfaceSetMaterial(0, material);

		// The deferred call causes hick ups, but when the Shape.Data is set from a thread it causes crashes... so...
		if (addCollision)
		{
			shape.SetDeferred("data", arrayMesh.GetFaces());
		}
	}

	// Heavy stuff that should be called in a thread, task, etc.
	public void ShiftProcess(Vector3 position)
	{
		foreach (Chunk chunk in chunks)
		{
			chunk.GenerateQuads(position);
		}
		//GenerateSurface(); // <- using SurfaceTool
		GenerateSurfaceArrays(); // <- manually creating arrays of data
	}

	// Ring shift finalization that might be called in main thread after the heavy stuff is done in parrallel threads.
	public void ShiftApply()
	{
		mesh_instance.Mesh = arrayMesh;
	}

	void AddNoise(ref Vector3 vertex)
	{
		if (noise == null)
			return;

		float n = noise.GetNoise3dv(vertex);
		if (n > 0)
			n = Mathf.Pow(n, 2);
		vertex.y += n * 1000f;
	}
}