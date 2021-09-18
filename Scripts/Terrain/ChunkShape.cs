using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ChunkShape : MeshInstance
{
	public bool isUpToDate { get { return isCreated && detail == World.detail; } }
	public bool isCreated { get; private set; }
	public bool isInProcess { get; private set; }

	Vector3[] vertices;
	int[] indices;
	Vector3[] normals;
	int detail;

	Godot.Collections.Array mesh_arrays;
	ArrayMesh arrayMesh;
	ConcavePolygonShape shape;

	Basis faceBasis;
	Vector3 centerC;
	float size;

	bool[] neighbours;

	public ChunkShape(Basis faceBasis, Vector3 centerC, float size)
	{
		this.faceBasis = faceBasis;
		this.centerC = centerC;
		this.size = size;

		vertices = new Vector3[0];
		indices = new int[0];
		normals = new Vector3[0];

		arrayMesh = new ArrayMesh();
		Mesh = arrayMesh;
		MaterialOverride = World.material;

		bool addCollision = size <= World.chunkSize;
		if (addCollision)
		{
			shape = new ConcavePolygonShape();
			StaticBody staticBody = new StaticBody();
			AddChild(staticBody);
			CollisionShape collision = new CollisionShape();
			staticBody.AddChild(collision);
			collision.Shape = shape;
		}
	}

	public void Create(bool[] neighbours, Action onReady)
	{
		this.neighbours = neighbours;
		GenerateAsync(onReady);
	}

	async void GenerateAsync(Action onReady)
	{
		isInProcess = true;
		await Task.Run(() => Generate());
		isInProcess = false;
		onReady();
	}

	public void Remove()
	{
		if (arrayMesh.GetSurfaceCount() != 0)
			arrayMesh.SurfaceRemove(0);
		isCreated = false;
	}

	void Generate()
	{
		detail = World.detail;

		CreateQuads();
		CreateSurface();

		// Make sure the node is still alive, and call finalization in a thread-safe manner:
		if (IsInstanceValid(this))
			CallDeferred("ApplyToMesh");
	}

	// When called as deferred causes hickups due to the expensive
	// collision shape operation, but it's the only way for now.
	// Physics related stuff is not thread-safe in Godot.
	void ApplyToMesh()
	{
		while (arrayMesh.GetSurfaceCount() > 0)
			arrayMesh.SurfaceRemove(0);
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, mesh_arrays);
		if (shape != null)
			shape.SetDeferred("data", arrayMesh.GetFaces());
		mesh_arrays.Clear();

		isCreated = true;
	}

	// Chunks will be made of 4 triangle quads
	void CreateQuads()
	{
		// Each quad has 4 corner vertices + center vertex:
		int verticesAmount = (detail + 1) * (detail + 1) + detail * detail;
		// Each quad consists of 4 triangles
		int indicesAmount = detail * detail * 4 * 3;

		/* Edge quads will have 2 additional vertices and 2 additional triangles,
		so, we need to increase the arrays sizes. */
		int sidesToStitch = 0;
		for (int i = 0; i < 4; i++)
			if (neighbours[i])
				sidesToStitch++;
		verticesAmount += detail * 2 * sidesToStitch;
		indicesAmount += detail * 2 * 3 * sidesToStitch;

		vertices = new Vector3[verticesAmount];
		indices = new int[indicesAmount];
		normals = new Vector3[verticesAmount];

		float quadHalfSize = size / (float)detail * 0.5f;

		// For additional 'skirts' along the seams between 2 chunks of different detail levels (sizes).
		// Each quad along the edge will get 2 additional vertices, and their data will be stored
		// at the end of vertices and indices arrays (after all the 'normal' vertices) starting from the offsets:
		int vOffset = (detail + 1) * (detail + 1) + detail * detail;
		int iOffset = detail * detail * 4 * 3;

		int vInd = 0, iInd = 0;
		for (int z = 0; z < detail + 1; z++)
		{
			for (int x = 0; x < detail + 1; x++)
			{
				Vector2 percent = new Vector2(x, z) / (float)detail;
				// Top left vertex:
				vertices[vInd] = centerC
					+ (percent.x - 0.5f) * faceBasis.x * size
					+ (percent.y - 0.5f) * faceBasis.z * size;

				if (x < detail && z < detail)
				{
					// Center vertex:
					vertices[vInd + detail + 1] = vertices[vInd] + faceBasis.x * quadHalfSize + faceBasis.z * quadHalfSize;
				}

				if (x > 0 && z > 0)
				{
					// At this stage all the vertices to the left and to up from current XZ position are created.
					// So, lets index them:
					indices[iInd] = vInd - (detail + 1) * 2;        // 0	0     1     2
					indices[iInd + 1] = vInd - (detail * 2 + 1);    // 1       3     4
					indices[iInd + 2] = vInd - (detail + 1);        // 3    5     6     7

					indices[iInd + 3] = vInd - (detail * 2 + 1);    // 1
					indices[iInd + 4] = vInd;                       // 6
					indices[iInd + 5] = vInd - (detail + 1);        // 3

					indices[iInd + 6] = vInd;                       // 6
					indices[iInd + 7] = vInd - 1;                   // 5
					indices[iInd + 8] = vInd - (detail + 1);        // 3

					indices[iInd + 9] = vInd - 1;                   // 5
					indices[iInd + 10] = vInd - (detail + 1) * 2;   // 0
					indices[iInd + 11] = vInd - (detail + 1);       // 3
					iInd += 12;

					if (x == 1 && neighbours[Chunk.W]) // from 0 and 5
						AddSeamQuad(vInd - 1, vInd - (detail + 1) * 2, vInd - (detail + 1), ref vOffset, ref iOffset, -faceBasis.x);
					if (x == detail && neighbours[Chunk.E]) // from 2 and 7
						AddSeamQuad(vInd - (detail * 2 + 1), vInd, vInd - (detail + 1), ref vOffset, ref iOffset, faceBasis.x);
					if (z == 1 && neighbours[Chunk.S]) // from 0-1, 1-2
						AddSeamQuad(vInd - (detail + 1) * 2, vInd - (detail * 2 + 1), vInd - (detail + 1), ref vOffset, ref iOffset, faceBasis.z);
					if (z == detail && neighbours[Chunk.N]) // from 5-6, 6-7
						AddSeamQuad(vInd, vInd - 1, vInd - (detail + 1), ref vOffset, ref iOffset, -faceBasis.z);
				}
				if (x == detail)
					// Skip central vertex indexes
					vInd += detail + 1;
				else vInd++;
			}
		}
	}

	void AddSeamQuad(int corner1, int corner2, int center, ref int vOffset, ref int iOffset, Vector3 toSide)
	{
		Vector3 seam = vertices[corner1] + (vertices[corner2] - vertices[corner1]) * 0.5f;

		vertices[vOffset] = seam;

		indices[iOffset] = center;
		indices[iOffset + 1] = corner1;
		indices[iOffset + 2] = vOffset;
		indices[iOffset + 3] = center;
		indices[iOffset + 4] = vOffset;
		indices[iOffset + 5] = corner2;

		vOffset += 1;
		iOffset += 6;
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

		// Prepare mesh arrays:
		mesh_arrays = new Godot.Collections.Array();
		mesh_arrays.Resize(9);
		mesh_arrays[0] = vertices;
		mesh_arrays[1] = normals;
		mesh_arrays[8] = indices;
	}
}