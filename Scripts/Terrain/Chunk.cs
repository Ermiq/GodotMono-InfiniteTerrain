using Godot;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public enum SeamSide
{
	NONE, TOP, RIGHT, BOTTOM, LEFT
}

public class Chunk
{
	public Vector3[] vertices;
	public int[] indices;

	Vector2 index;
	float size;
	int detail;

	SeamSide seamSide = SeamSide.NONE;
	int[] seamQuads;

	public Chunk(Vector2 index, float size, int detail, bool hasSeamSide = false)
	{
		this.index = index;
		this.size = size;
		this.detail = detail;

		if (hasSeamSide)
			this.seamSide = GetSeamSide();
		else
			this.seamSide = SeamSide.NONE;

		// Each quad has 4 corner vertex + center vertex:
		int verticesAmount = (detail + 1) * (detail + 1) + detail * detail;
		// Each quad consists of 4 triangles
		int indicesAmount = detail * detail * 4 * 3;
		if (this.seamSide != SeamSide.NONE)
		{
			// A seam side quad will have 2 additional vertices and 2 additional triangles,
			// so, we need to increase the arrays sizes in this case.
			verticesAmount += detail * 2;
			indicesAmount += detail * 2 * 3;
		}
		vertices = new Vector3[verticesAmount];
		indices = new int[indicesAmount];
	}

	public void GenerateQuads(Vector3 position)
	{
		Vector3 chunkCenter = position
			+ World.Right * (index.x * (size * (float)World.heightCoef))
			+ World.Forward * (index.y * (size * (float)World.heightCoef));

		seamQuads = GetEdgeQuads(detail);

		Vector3 Center, FrontLeft, FrontRight, BackLeft, BackRight;
		// Calculate half size of the quad's edge. We'll use it to get the quad center position.
		float quadSize = size * (float)World.heightCoef / (float)detail;

		int v0 = 0, v1 = 0, t = 0;

		// For additional 'skirts' along the seams between 2 chunks of different detail
		// each quad will get 2 additional vertices, and their data will be stored
		// at the end of vertices and indices arrays starting from the offsets:
		int vOffset = vertices.Length - detail * 2;
		int iOffset = indices.Length - detail * 2 * 3;

		for (int z = 0; z < detail; z++)
		{
			for (int x = 0; x < detail; x++)
			{
				/*
				The vertices in the vertices array are placed in the following index order on the XZ matrix:

					X*	   0     1
				Z
				*		
						0     1     2
				0		   3     4
						5     6     7
				1		   8     9
						10    11    12

				On the first loop, the quad (0,1,6,5,3) is created.
				Next, on the loop Z=0,X=1, the quad (1,2,7,6,4) is created. Only 2,7,4 vertices are calculated.
				Next, on the loop Z=1,X=0, the quad (5,6,11,10,8). Only 11,10,8 vertices are calculated...

				Each full Z line (quads edges) starts from Z*(quadsInRow*2+1), i.e. when quadsInRow=2, they start from
				0, 5, 10 in the example matrix.
				So, int v0 = Z*(quadsInRow*2+1)+X  <- the index of each XZ element in the 'main' lines (quads edges),
				i.e. the top left corner of each quad (and also a bottom left of a quad that is positioned above).
				Central index of a quad: v0+(quadsInRow+1).
				The next Z line index (bottom left of a quad) is: (Z+1)*(quadsInRow*2+1)+X.

				So, here are the formulas for each vertex of a quad:
				
					Front left		= Z*(quadsInRow*2+1)+X					<- v0
					Front right		= Z*(quadsInRow*2+1)+X+1				<- v0+1
					Back left		= (Z+1)*(quadsInRow * 2 + 1)+X			<- v1, i.e. next v0 (at next Z line)
					Back right		= (Z+1)*(quadsInRow * 2 + 1)+X+1		<- v1+1, i.e. next v0+1 (at next Z line)
					Center			= Z*(quadsInRow*2+1)+X+(quadsInRow+1)	<- v0+(quadsInRow+1)

				*/

				v0 = z * (detail * 2 + 1) + x;
				v1 = (z + 1) * (detail * 2 + 1) + x;

				BackRight = chunkCenter +
					World.Right * (-size * World.heightCoef * 0.5f + (x + 1) * quadSize) +
					World.Forward * (size * World.heightCoef * 0.5f - (z + 1) * quadSize);
				vertices[v1 + 1] = BackRight;

				Center = BackRight - World.Right * (quadSize * 0.5f) + World.Forward * (quadSize * 0.5f);
				vertices[v0 + (detail + 1)] = Center;

				if (z == 0)
				{
					FrontRight = BackRight + World.Forward * quadSize;
					vertices[v0 + 1] = FrontRight;

					if (x == 0)
					{
						FrontLeft = BackRight - World.Right * quadSize + World.Forward * quadSize;
						vertices[v0] = FrontLeft;
					}
				}
				if (x == 0)
				{
					BackLeft = BackRight - World.Right * quadSize;
					vertices[v1] = BackLeft;
				}

				// Indexing:
				SeamSide s = GetQuadSeamSide(detail * z + x);

				// Top tri (3,0,1 or 8,5,6)
				indices[t] = v0 + detail + 1;
				indices[t + 1] = v0;
				if (s != SeamSide.TOP)
					indices[t + 2] = v0 + 1;
				else
					CreateSeamTris(v0, v0 + 1, t + 2, vOffset, iOffset, v0, v1);

				// Right tri (3,1,6 0r 8,6,11)
				indices[t + 3] = v0 + detail + 1;
				indices[t + 4] = v0 + 1;
				if (s != SeamSide.RIGHT)
					indices[t + 5] = v1 + 1;
				else
					CreateSeamTris(v0 + 1, v1 + 1, t + 5, vOffset, iOffset, v0, v1);

				// Bottom tri (3,6,5 or 8,11,10)
				indices[t + 6] = v0 + detail + 1;
				indices[t + 7] = v1 + 1;
				if (s != SeamSide.BOTTOM)
					indices[t + 8] = v1;
				else
					CreateSeamTris(v1 + 1, v1, t + 8, vOffset, iOffset, v0, v1);

				// Left tri (3,5,0 or 8,10,5)
				indices[t + 9] = v0 + detail + 1;
				indices[t + 10] = v1;
				if (s != SeamSide.LEFT)
					indices[t + 11] = v0;
				else
					CreateSeamTris(v1, v0, t + 11, vOffset, iOffset, v0, v1);

				t += 12;
				if (s != SeamSide.NONE)
				{
					vOffset += 2;
					iOffset += 6;
				}
			}
		}
	}

	// For 'skirt', to fix the seams between 2 chunks with ddifferent detail.
	// Instead of 1 triangle, we create 2 aditional vertices along the edge, and create 3 triangles.
	// The vertices and indices of the additional vertexes is stored in the arrays with some offset,
	// after all the usual triangles data.
	void CreateSeamTris(int corner1, int corner2, int t, int vOffset, int iOffset, int v0, int v1)
	{
		Vector3 seam1 = vertices[corner1] + (vertices[corner2] - vertices[corner1]) * 0.33333f;
		Vector3 seam2 = vertices[corner1] + (vertices[corner2] - vertices[corner1]) * 0.66666f;

		vertices[vOffset] = seam1;
		vertices[vOffset + 1] = seam2;

		indices[t] = vOffset;
		indices[iOffset] = v0 + detail + 1;
		indices[iOffset + 1] = vOffset;
		indices[iOffset + 2] = vOffset + 1;
		indices[iOffset + 3] = v0 + detail + 1;
		indices[iOffset + 4] = vOffset + 1;
		indices[iOffset + 5] = corner2;
	}

	int[] GetEdgeQuads(int detail)
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
			return SeamSide.TOP;
		else if (index == Vector2.Right)
			return SeamSide.LEFT;
		else if (index == Vector2.Down)
			return SeamSide.BOTTOM;
		else if (index == Vector2.Left)
			return SeamSide.RIGHT;
		else return SeamSide.NONE;
	}

	SeamSide GetQuadSeamSide(int quadIndex)
	{
		if (seamSide == SeamSide.NONE)
			return SeamSide.NONE;

		foreach (int i in seamQuads)
		{
			if (i == quadIndex)
				return seamSide;
		}
		return SeamSide.NONE;
	}
}