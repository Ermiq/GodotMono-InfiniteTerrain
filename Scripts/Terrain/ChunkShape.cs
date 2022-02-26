using Godot;
using System;
using System.Threading.Tasks;

public class ChunkShape : MeshInstance
{
	public bool isCreated { get; private set; }
	public bool isInProcess { get; private set; }
	public Vector3 centerA { get; private set; }

	Vector3[] vertices;
	Vector3[] normals;
	int[] indices;
	int detail;

	// For additional 'skirts' along the seams between 2 chunks of different detail levels (sizes)
	// each quad along the edge will be expaneded with 2 additional triangles, and their data will be stored
	// in vertices/indices arrays after all the 'main' vertices/indices starting from the 'offset' indexes:
	int mainVerticesAmount;
	int vOffset;
	int iOffset;

	Godot.Collections.Array mesh_arrays;
	ArrayMesh arrayMesh;
	ConcavePolygonShape shape;

	Basis faceBasis;
	Vector3 centerC;
	float size;
	TerrainSettings settings;

	public ChunkShape(Basis faceBasis, Vector3 centerC, float size, TerrainSettings settings)
	{
		this.faceBasis = faceBasis;
		this.centerC = centerC;
		this.size = size;
		this.settings = settings;

		arrayMesh = new ArrayMesh();
		Mesh = arrayMesh;
		MaterialOverride = settings.material;

		bool addCollision = size <= settings.chunkSize;
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

	public void Create(Action onReady)
	{
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
		detail = settings.detail;

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

	void CreateQuads()
	{
		// Each quad has 4 corner vertices + center vertex:
		int verticesAmount = (detail + 1) * (detail + 1) + detail * detail;
		// Each quad consists of 4 triangles
		int indicesAmount = detail * detail * 4 * 3;

		// Store the main vertices amount for averaged center calculations later:
		mainVerticesAmount = verticesAmount;
		// and setup the index from which the skirt vertices/indices will be placed in the surface data arrays:
		vOffset = verticesAmount;
		iOffset = indicesAmount;
		// Edge quads will be expanded with 2 additional vertices and 2 additional triangles,
		// 'skirt' quads, so, we need to increase the arrays sizes:
		verticesAmount += (detail * detail) * 2;
		indicesAmount += (detail * detail) * 2 * 3;

		vertices = new Vector3[verticesAmount];
		normals = new Vector3[verticesAmount];
		indices = new int[indicesAmount];

		float quadHalfSize = size / detail * 0.5f;

		int vInd = 0, iInd = 0;
		for (int y = 0; y < detail + 1; y++)
		{
			for (int x = 0; x < detail + 1; x++)
			{
				Vector2 percent = new Vector2(x, y) / detail;
				// Top left vertex:
				vertices[vInd] = Vector3.Zero
					+ (percent.x - 0.5f) * faceBasis.x * size
					+ (percent.y - 0.5f) * faceBasis.z * size;

				if (x < detail && y < detail)
				{
					// Center vertex:
					vertices[vInd + detail + 1] = vertices[vInd] + faceBasis.x * quadHalfSize + faceBasis.z * quadHalfSize;

					// Index vertices (first indexation will be at vertex 0):
					indices[iInd] = vInd;                           // 0    0     1     2
					indices[iInd + 1] = vInd + 1;                   // 1       3     4
					indices[iInd + 2] = vInd + detail + 1;          // 3    5     6     7

					indices[iInd + 3] = vInd + 1;                   // 1
					indices[iInd + 4] = vInd + (detail + 1) * 2;    // 6
					indices[iInd + 5] = vInd + detail + 1;          // 3

					indices[iInd + 6] = vInd + (detail + 1) * 2;    // 6
					indices[iInd + 7] = vInd + (detail * 2) + 1;    // 5
					indices[iInd + 8] = vInd + detail + 1;          // 3

					indices[iInd + 9] = vInd + (detail * 2) + 1;    // 5
					indices[iInd + 10] = vInd;                      // 0
					indices[iInd + 11] = vInd + detail + 1;         // 3
					iInd += 12;
				}

				if (x > 0 && y > 0)
				{
					// This condition will begin to be met from vertex 6.
					// At this stage all the vertices to the left and to up from current XZ position are created.
					// So, let's calculate the skirts vertex positions if we're on the edge:
					if (x == 1) // to the right from 0 and 5
						AddSkirtQuad(vInd - 1, vInd - (detail + 1) * 2, -faceBasis.x);
					if (x == detail) // to he left from 2 and 7
						AddSkirtQuad(vInd - (detail * 2 + 1), vInd, faceBasis.x);
					if (y == 1) // up from 0-1, 1-2
						AddSkirtQuad(vInd - (detail + 1) * 2, vInd - (detail * 2 + 1), -faceBasis.z);
					if (y == detail) // down from 5-6, 6-7
						AddSkirtQuad(vInd, vInd - 1, faceBasis.z);
				}
				if (x == detail)
					// Skip central vertex line and jump to the next quad line vertex index:
					vInd += detail + 1;
				else vInd++;
			}
		}
	}

	void AddSkirtQuad(int corner1, int corner2, Vector3 toSide)
	{
		float quadSize = size / (float)detail;
		Vector3 seam1 = vertices[corner1] + toSide * quadSize;
		Vector3 seam2 = vertices[corner2] + toSide * quadSize;

		vertices[vOffset] = seam1;
		vertices[vOffset + 1] = seam2;

		indices[iOffset] = corner1;
		indices[iOffset + 1] = vOffset;
		indices[iOffset + 2] = vOffset + 1;
		indices[iOffset + 3] = vOffset + 1;
		indices[iOffset + 4] = corner2;
		indices[iOffset + 5] = corner1;

		vOffset += 2;
		iOffset += 6;
	}

	void CreateSurface()
	{
		// Apply noise to vertices and calculate the averaged center vertex:
		for (int v = 0; v < vertices.Length; v++)
		{
			Vector3 vertex = PrepareVertex(v);
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

		// Lower skirt vertices to disguise the seams
		LowerSkirts();

		FinalizeGeneration();
	}

	Vector3 PrepareVertex(int v)
	{
		Vector3 vertex = vertices[v] + faceBasis.y * settings.EvaluatePositionFlat(vertices[v] + centerC);

		// Add to the averaged center:
		if (v < mainVerticesAmount)
			centerA += vertex;

		return vertex;
	}

	void LowerSkirts()
	{
		float l = size / (float)detail;

		for (int v = mainVerticesAmount; v < vertices.Length; v++)
		{
			Vector3 vertex = vertices[v];
			vertex -= faceBasis.y * l;
			vertices[v] = vertex;
		}
	}

	void FinalizeGeneration()
	{
		// Prepare mesh arrays:
		mesh_arrays = new Godot.Collections.Array();
		mesh_arrays.Resize(9);
		mesh_arrays[0] = vertices;
		mesh_arrays[1] = normals;
		mesh_arrays[8] = indices;

		// Average the centerA:
		centerA /= mainVerticesAmount;
	}
}