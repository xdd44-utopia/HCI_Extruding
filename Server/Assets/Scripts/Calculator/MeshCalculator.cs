using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class MeshCalculator {

	public static bool debugging = false;
	public static int debugInt = 0;
	private static int loopCount;

	public static List<int> clockwiseBoundary(Vector3[] vertices, List<int> boundary, Vector3 localNormal) {

		float angleAcc = 0;
		for (int i=0;i<boundary.Count;i++) {
			Vector3 va = (vertices[boundary[i]] - vertices[boundary[(i + boundary.Count - 1) % boundary.Count]]).normalized;
			Vector3 vb = (vertices[boundary[(i + 1) % boundary.Count]] - vertices[boundary[i]]).normalized;
			bool isConvex = Vector3.Angle(Vector3.Cross(va, vb).normalized, localNormal) < 45f;
			angleAcc += (180 - Vector3.Angle(va, vb)) * (isConvex ? 1 : -1);
		}
		if (angleAcc < 0) {
			List<int> newBoundary = new List<int>();
			for (int i=boundary.Count-1;i>=0;i--) {
				newBoundary.Add(boundary[i]);
			}
			return newBoundary;
		}
		else {
			return boundary;
		}

	}

	private static int[] checkTriangleNormal(Vector3[] vertices, int[] triangles, Vector3 localNormal) {
		int[] newTriangles = new int[triangles.Length];
		for (int i=0;i<triangles.Length/3;i++) {
			if (Vector3.Angle(Vector3.Cross(vertices[triangles[i * 3 + 1]] - vertices[triangles[i * 3]], vertices[triangles[i * 3 + 2]] - vertices[triangles[i * 3]]).normalized, localNormal) < 45f) {
				for (int j=0;j<3;j++) {
					newTriangles[i * 3 + j] = triangles[i * 3 + j];
				}
			}
			else {
				for (int j=0;j<3;j++) {
					newTriangles[i * 3 + j] = triangles[i * 3 + (2 - j)];
				}
			}
		}
		return newTriangles;
	}

	public static void simplifyMesh(ref Vector3[] vertices, ref int[] triangles) {

		//Remove duplicated or unused vertices
		//firstAppear[i] == -1: unused
		//firstAppear[i] != i: duplicated
		int[] firstAppear = new int[vertices.Length];
		for (int i=0;i<vertices.Length;i++) {
			firstAppear[i] = -1;
		}
		for (int i=0;i<triangles.Length;i++) {
			firstAppear[triangles[i]] = triangles[i];
		}
		for (int i=0;i<vertices.Length;i++) {
			for (int j=i+1;j<vertices.Length;j++) {
				if (Mathf.Approximately(0, Vector3.Angle(vertices[j], vertices[i]))) {
					firstAppear[j] = firstAppear[i];
				}
			}
		}
		for (int i=0;i<triangles.Length;i++) {
			triangles[i] = firstAppear[triangles[i]];
		}

		// Remap triangles
		int cnt = 0;
		List<Vector3> verticesList = new List<Vector3>(); 
		for (int i=0;i<vertices.Length;i++) {
			if (firstAppear[i] == i) {
				verticesList.Add(vertices[i]);
				for (int j=0;j<triangles.Length;j++) {
					if (triangles[j] == i) {
						triangles[j] = cnt;
					}
				}
				cnt++;
			}
		}

		vertices = verticesList.ToArray();

		//Remove triangles with area = 0
		List<int> trianglesList = new List<int>();
		for (int i=0;i<triangles.Length/3;i++) {
			if (
				triangles[i * 3] != triangles[i * 3 + 1] &&
				triangles[i * 3] != triangles[i * 3 + 2] &&
				triangles[i * 3 + 1] != triangles[i * 3 + 2]
			) {
				for (int j=0;j<3;j++) {
					trianglesList.Add(triangles[i * 3 + j]);
				}
			}
		}
		triangles = trianglesList.ToArray();

	}

	public static List<List<int>> extractBoundaries(ref Vector3[] vertices, ref int[] triangleEdges, ref int[] edges)  {

		//Boundaries consist of edges that appear only once
		List<List<int>> boundaries = new List<List<int>>();
		int[] appear = new int[edges.Length / 2];
		for (int i=0;i<triangleEdges.Length;i++) {
			appear[triangleEdges[i]]++;
		}

		for (int i=0;i<appear.Length;i++) {
			if (appear[i] == 1) {
				List<int> newBoundary = new List<int>();
				newBoundary.Add(edges[i * 2]);
				appear[i] = 0;
				int cur = i;
				int side = 1;
				bool found = false;
				do {
					found = false;
					int foundWhat = 0;
					for (int j=0;j<appear.Length;j++) {
						for (int k=0;k<2;k++) {
							if (appear[j] == 1 && edges[j * 2 + k] == edges[cur * 2 + side]) {
								appear[j] = 0;
								newBoundary.Add(edges[j * 2 + k]);
								cur = j;
								side = 1 - k;
								found = true;
								foundWhat = j;
								break;
							}
						}
					}
				} while (found);
				boundaries.Add(newBoundary);
			}
		}

		//If having holes, found the outer boundary and move it to the first
		//Boundary with largest variance?
		if (boundaries.Count > 1) {
			float maxVar = 0;
			int maxi = 0;
			for (int i=0;i<boundaries.Count;i++) {
				List<Vector3> boundaryVertices = new List<Vector3>();
				for (int j=0;j<boundaries[i].Count;j++) {
					boundaryVertices.Add(vertices[boundaries[i][j]]);
				}
				float variance = VectorCalculator.vectorVariance(boundaryVertices);
				if (variance > maxVar) {
					maxVar = variance;
					maxi = i;
				}
			}
			if (maxi != 0) {
				List<int> t = boundaries[0];
				boundaries[0] = boundaries[maxi];
				boundaries[maxi] = t;
			}
		}

		return boundaries;
	}

	public static void generateFaceCover(ref Vector3[] vertices, ref int[] triangles, ref List<List<int>> boundary, float thickness) {

		Vector3 localNormal = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]).normalized;

		//Offset towards normal direction
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] += localNormal * 0.002f;
		}
		
		offsetBoundary(ref vertices, boundary, localNormal, thickness);

		simplifyMesh(ref vertices, ref triangles);

	}

	private static void offsetBoundary(ref Vector3[] vertices, List<List<int>> boundary, Vector3 localNormal, float thickness) {

		//Offset towards central
		List<List<Vector3>> offsets = new List<List<Vector3>>();
		for (int i=0;i<boundary.Count;i++) {
			offsets.Add(new List<Vector3>());
			for (int j=0;j<boundary[i].Count;j++) {
				offsets[i].Add(new Vector3(0, 0, 0));
			}
		}

		for (int i=0;i<boundary.Count;i++) {
			boundary[i] = clockwiseBoundary(vertices, boundary[i], localNormal);
			Vector3[] dir = new Vector3[boundary[i].Count];
			float[] angle = new float[boundary[i].Count];
			bool[] isConvex = new bool[boundary[i].Count];
			for (int j=0;j<boundary[i].Count;j++) {
				Vector3 va = (vertices[boundary[i][j]] - vertices[boundary[i][(j + boundary[i].Count - 1) % boundary[i].Count]]).normalized;
				Vector3 vb = (vertices[boundary[i][(j + 1) % boundary[i].Count]] - vertices[boundary[i][j]]).normalized;
				isConvex[j] = Mathf.Approximately(0, Vector3.Angle(Vector3.Cross(va, vb).normalized, localNormal));
				angle[j] = VectorCalculator.vectorAngle(va, vb);
				dir[j] = (vb - va).normalized;
			}
			bool isOuter = (i == 0);
			for (int j=0;j<boundary[i].Count;j++) {
				offsets[i][j] = dir[j] * thickness * (isConvex[j] ^ isOuter ? -1 : 1) / Mathf.Cos(angle[j] / 2);
			}
		}

		for (int i=0;i<boundary.Count;i++) {
			for (int j=0;j<boundary[i].Count;j++) {
				vertices[boundary[i][j]] += offsets[i][j];
			}
		}

	}

	public static int[] triangulationUnorderedBoundaries(Vector3[] vertices, List<List<int>> boundaries, Vector3 localNormal) {

		for (int i=0;i<boundaries.Count;i++) {
			boundaries[i] = clockwiseBoundary(vertices, boundaries[i], localNormal);
		}

		Vector2[] vertices2D = VectorCalculator.facePlaneFront(vertices, localNormal);

		//Categorize boundaries into faces
		//If a boundary is contained by even number of larger boundaries, it can be seen as an outline of a face
		//Otherwise, it can be seen as a hole inside the smallest boundary that contains it.
		List<int> containCount = new List<int>();
		for (int i=0;i<boundaries.Count;i++) {
			containCount.Add(0);
			for (int j=0;j<boundaries.Count;j++) {
				if (i != j && VectorCalculator.isPointInsidePolygon(vertices2D, boundaries[j], vertices2D[boundaries[i][0]])) {
					containCount[i]++;
				}
			}
		}

		List<List<List<int>>> categorizedBoundaries = new List<List<List<int>>>();
		List<int> levelRecord = new List<int>();

		for (int i=0;i<boundaries.Count;i++) {
			if (containCount[i] % 2 == 0) {
				categorizedBoundaries.Add(new List<List<int>>());
				categorizedBoundaries[categorizedBoundaries.Count - 1].Add(boundaries[i]);
				levelRecord.Add(containCount[i]);
			}
		}
		for (int i=0;i<boundaries.Count;i++) {
			if (containCount[i] % 2 == 1) {
				for (int j=0;j<categorizedBoundaries.Count;j++) {
					if (levelRecord[j] == containCount[i] - 1 && VectorCalculator.isPointInsidePolygon(vertices2D, categorizedBoundaries[j][0], vertices2D[boundaries[i][0]])) {
						categorizedBoundaries[j].Add(boundaries[i]);
						break;
					}
				}
			}
		}

		List<int> trianglesList = new List<int>();
		for (int i=0;i<categorizedBoundaries.Count;i++) {
			boundaries = categorizedBoundaries[i];
			debugging = (i == 0);
			int[] triangles = triangulation(vertices, boundaries, localNormal);
			trianglesList.AddRange(triangles);
		}
		return trianglesList.ToArray();

	}

	public static int[] triangulation(Vector3[] vertices, List<List<int>> boundaries, Vector3 localNormal) {

		for (int i=0;i<boundaries.Count;i++) {
			boundaries[i] = clockwiseBoundary(vertices, boundaries[i], localNormal);
			boundaries[i] = VectorCalculator.simplifyBoundary(vertices, boundaries[i]);
		}

		List<int> noHolePolygon = boundaries.Count > 1 ? splitHolePolygon(vertices, boundaries, localNormal) : boundaries[0];

		if (debugging) {
			for (int i=0;i<noHolePolygon.Count;i++) {
				Debug.DrawLine(
					vertices[noHolePolygon[i]] + new Vector3(-5, 0, 1),
					vertices[noHolePolygon[(i + 1) % noHolePolygon.Count]] + new Vector3(-5, 0, 1),
					Color.white,
					5000
				);
			}
		}

		//Force duplicate reused vertices and offset
		//Rotate the vertices to x-y plane
		bool[] used = new bool[vertices.Length];
		List<Vector3> newVerticesList = new List<Vector3>();
		List<int> pointerToOriginal = new List<int>();
		for (int i=0;i<vertices.Length;i++) {
			newVerticesList.Add(vertices[i]);
			pointerToOriginal.Add(i);
		}
		List<int> boundary = new List<int>();
		for (int i=0;i<noHolePolygon.Count;i++) {
			if (!used[noHolePolygon[i]]) {
				boundary.Add(noHolePolygon[i]);
				used[noHolePolygon[i]] = true;
			}
			else {
				newVerticesList.Add(vertices[noHolePolygon[i]]);
				pointerToOriginal.Add(noHolePolygon[i]);
				boundary.Add(newVerticesList.Count - 1);
			}
		}
		Vector3[] newVertices = VectorCalculator.facePlaneFront(newVerticesList.ToArray(), localNormal).Select(v => new Vector3(v.x, v.y, 0)).ToArray();
		offsetBoundary(ref newVertices, new List<List<int>> {boundary}, new Vector3(0, 0, 1), 0.01f);
		boundary = clockwiseBoundary(newVertices, boundary, new Vector3(0, 0, 1));
		//Avoid two vertices locating on same x
		Vector2[] vertices2D = newVertices.Select(v => new Vector2(v.x + (float)Math.Tanh(v.y) * 0.1f, v.y + (float)Math.Tanh(v.x) * 0.1f)).ToArray();
		List<List<int>> monotonePolygons = splitMonotonePolygon(vertices2D, boundary);

		if (debugging) {
			for (int i=0;i<monotonePolygons.Count;i++) {
				for (int j=0;j<monotonePolygons[i].Count;j++) {
					Vector3 a = new Vector3(vertices2D[monotonePolygons[i][j]].x, vertices2D[monotonePolygons[i][j]].y, 0);
					Vector3 b = new Vector3(vertices2D[monotonePolygons[i][(j + 1) % monotonePolygons[i].Count]].x, vertices2D[monotonePolygons[i][(j + 1) % monotonePolygons[i].Count]].y, 0);
					Debug.DrawLine(
						a + new Vector3(-20, 0, 0.05f * i + debugInt),
						b + new Vector3(-20, 0, 0.05f * i + debugInt),
						new Color(20 * i, 20 * i, 20 * i + debugInt),
						5000
					);
				}
			}
		}

		List<int> trianglesList = new List<int>();

		for (int i=0;i<monotonePolygons.Count;i++) {
			trianglesList.AddRange(triangulizeMonotonePolygon(vertices2D, monotonePolygons[i]));
		}

		int[] triangles = trianglesList.Select(x => pointerToOriginal[x]).ToArray();

		if (debugging) {
			for (int i=0;i<triangles.Length/3;i++) {
				for (int j=0;j<3;j++) {
					Debug.DrawLine(
						vertices[triangles[i * 3 + j]] + new Vector3(-25, 0, debugInt),
						vertices[triangles[i * 3 + (j + 1) % 3]] + new Vector3(-25, 0, debugInt),
						Color.white,
						5000
					);
				}
			}
		}

		int[] trianglesNormalized = checkTriangleNormal(vertices, triangles, localNormal);

		return checkTriangleNormal(vertices, triangles, localNormal);

	}

	private static List<int> splitHolePolygon(Vector3[] vertices, List<List<int>> boundaries, Vector3 localNormal) {

		if (debugging) {
			for (int i=0;i<boundaries.Count;i++) {
				for (int j=0;j<boundaries[i].Count;j++) {
					Debug.DrawLine(
						vertices[boundaries[i][j]],
						vertices[boundaries[i][(j + 1) % boundaries[i].Count]],
						Color.white,
						5000
					);
				}
			}
		}

		int bn = boundaries.Count;

		//Define a disjoint set
		int[] father = new int[bn];
		for (int i=0;i<bn;i++) {
			father[i] = i;
		}
		
		int find(int x) {
			int p = father[x];
			while (p != father[p]) {
				p = father[p];
			}
			father[x] = p;
			return p;
		}

		void union(int x, int y) {
			int px = find(x);
			int py = find(y);
			if (px != py) {
				father[px] = py;
				father[x] = py;
			}
		}

		//Add edges to break holes
		List<int> edges = new List<int>();
		for (int cNum=0;cNum < bn - 1;cNum++) {
			//there should be #holes additional edges
			//Find the closest distance between two unconnected boundaries
			(float dist, int ub, int ue, int vb, int ve) minEdge = (2147483647, -1, -1, -1, -1); //Min dist, boundary a, vertex a, boundary b, vertex b
			for (int ib=0;ib<bn;ib++) {
				for (int jb=0;jb<bn;jb++) {
					if (find(ib) != find(jb)) {
						for (int ie=0;ie<boundaries[ib].Count;ie++) {
							for (int je=0;je<boundaries[jb].Count;je++) {
								Vector3 diff = vertices[boundaries[ib][ie]] - vertices[boundaries[jb][je]];
								if (diff.magnitude < minEdge.dist) {
									minEdge = (diff.magnitude, ib, ie, jb, je);
								}
							}
						}
					}
				}
			}
			edges.Add(boundaries[minEdge.ub][minEdge.ue]);
			edges.Add(boundaries[minEdge.vb][minEdge.ve]);
			union(minEdge.ub, minEdge.vb);
		}

		//Polygon with holes should be cut into a whole boundary
		return splitBoundariesByEdges(vertices, boundaries, edges, localNormal)[0];

	}
	private static List<List<int>> splitMonotonePolygon(Vector2[] vertices, List<int> boundary) {

		//Sort the boundary vertices by x
		List<int> verticePointers = new List<int>();
		for (int i=0;i<boundary.Count;i++) {
			verticePointers.Add(i);
		}
		int[] boundaryArray = boundary.ToArray();
		verticePointers.Sort((a, b) => {
			if (vertices[boundaryArray[a]].x - vertices[boundaryArray[b]].x == 0) {
				return 0;
			}
			else if (vertices[boundaryArray[a]].x - vertices[boundaryArray[b]].x > 0) {
				return 1;
			}
			else {
				return -1;
			}
		});

		//Add splitting edges
		List<int> edges = new List<int>();
		for (int i=0;i<verticePointers.Count;i++) {
			int cur = verticePointers[i];
			int pre = (cur + boundary.Count - 1) % boundary.Count;
			int nex = (cur + 1) % boundary.Count;
			if ((vertices[boundary[pre]] - vertices[boundary[cur]]).normalized.y > (vertices[boundary[nex]] - vertices[boundary[cur]]).normalized.y) {
				int t = pre; pre = nex; nex = t;
			}
			//If both edges are on same side, add an edge towards the other side of the trapezoid
			if (vertices[boundary[pre]].x > vertices[boundary[cur]].x && vertices[boundary[nex]].x > vertices[boundary[cur]].x) {
				for (int j=i-1;j>=0;j--) {
					if (VectorCalculator.isLineSegmentInsidePolygon(vertices, boundary, vertices[boundary[verticePointers[i]]], vertices[boundary[verticePointers[j]]])) {
						edges.Add(Math.Min(boundary[verticePointers[i]], boundary[verticePointers[j]]));
						edges.Add(Math.Max(boundary[verticePointers[i]], boundary[verticePointers[j]]));
						break;
					}
				}
			}
			if (vertices[boundary[pre]].x <= vertices[boundary[cur]].x && vertices[boundary[nex]].x <= vertices[boundary[cur]].x) {
				for (int j=i+1;j<verticePointers.Count;j++) {
					if (VectorCalculator.isLineSegmentInsidePolygon(vertices, boundary, vertices[boundary[verticePointers[i]]], vertices[boundary[verticePointers[j]]])) {
						edges.Add(Math.Min(boundary[verticePointers[i]], boundary[verticePointers[j]]));
						edges.Add(Math.Max(boundary[verticePointers[i]], boundary[verticePointers[j]]));
						break;
					}
				}
			}
		}

		VectorCalculator.debugInt++;

		List<List<int>> result = splitBoundariesByEdges(vertices.Select(v => new Vector3(v.x, v.y, 0)).ToArray(), new List<List<int>>{ boundary }, edges, new Vector3(0, 0, 1));

		return result;

	}
	private static List<int> triangulizeMonotonePolygon(Vector2[] vertices, List<int> boundary) {

		if (boundary.Count == 3) {
			return boundary;
		}

		//Algorithm:
		//Sort vertices by x
		//From left to right, connect each vertex with all vertices at the left side of it, and trim the formed triangles off the polygon

		//Use prev to keep track of its left adjacent vertex
		//Note that the first vertex has both adjacent vertices on its right, and the last vertex has both on its left
		List<(int index, int prev, int next, bool removed)> verticesPointers = new List<(int index, int prev, int next, bool removed)>();
		List<bool> removed = new List<bool>();
		for (int i=0;i<boundary.Count;i++) {
			int prev = vertices[boundary[(i + boundary.Count - 1) % boundary.Count]].x < vertices[boundary[(i + 1) % boundary.Count]].x ? boundary[(i + boundary.Count - 1) % boundary.Count] : boundary[(i + 1) % boundary.Count];
			int next = vertices[boundary[(i + boundary.Count - 1) % boundary.Count]].x > vertices[boundary[(i + 1) % boundary.Count]].x ? boundary[(i + boundary.Count - 1) % boundary.Count] : boundary[(i + 1) % boundary.Count];
			verticesPointers.Add((boundary[i], prev, next, false));
		}
		verticesPointers.Sort((a, b) => {
			if (vertices[a.index].x - vertices[b.index].x == 0) {
				return 0;
			}
			else if (vertices[a.index].x - vertices[b.index].x > 0) {
				return 1;
			}
			else {
				return -1;
			}
		});

		for (int i=0;i<verticesPointers.Count;i++) {
			Debug.DrawLine(
				new Vector3(vertices[verticesPointers[i].index].x - 20, vertices[verticesPointers[i].index].y, 0),
				new Vector3(vertices[verticesPointers[i].index].x - 20, vertices[verticesPointers[i].index].y, 0.05f * i),
				Color.yellow,
				5000,
				false
			);
		}
		
		List<int> triangles = new List<int>();

		//A polygon with n vertices will be splitted n - 3 times
		for (int s=0;s<boundary.Count - 3;s++) {
			int count = 0;
			int[] leftMost = new int[3];
			for (int i=0;i<verticesPointers.Count;i++) {
				if (!verticesPointers[i].removed) {
					leftMost[count++] = i;
				}
				if (count == 3) {
					break;
				}
			}
			bool found = false;
			for (int i=leftMost[2];i<verticesPointers.Count;i++) {
				if (!verticesPointers[i].removed) {
					int prev = verticesPointers.FindIndex(a => a.index == verticesPointers[i].prev);
					int prevprev = prev == leftMost[0] ? leftMost[1] : verticesPointers.FindIndex(a => a.index == verticesPointers[prev].prev);
					if (VectorCalculator.isLineSegmentInsidePolygon(vertices, boundary, vertices[verticesPointers[i].index], vertices[verticesPointers[prevprev].index])) {
						triangles.Add(verticesPointers[prev].index);
						triangles.Add(verticesPointers[prevprev].index);
						triangles.Add(verticesPointers[i].index);
						verticesPointers[prev] = (0, 0, 0, true);
						verticesPointers[i] = (verticesPointers[i].index, verticesPointers[prevprev].index, verticesPointers[i].next, false);
						found = true;
						break;
					}
					if (i == verticesPointers.Count - 1) {
						prev = verticesPointers.FindIndex(a => a.index == verticesPointers[i].next);
						prevprev = prev == leftMost[0] ? leftMost[1] : verticesPointers.FindIndex(a => a.index == verticesPointers[prev].prev);
						if (VectorCalculator.isLineSegmentInsidePolygon(vertices, boundary, vertices[verticesPointers[i].index], vertices[verticesPointers[prevprev].index])) {
							triangles.Add(verticesPointers[prev].index);
							triangles.Add(verticesPointers[prevprev].index);
							triangles.Add(verticesPointers[i].index);
							verticesPointers[prev] = (0, 0, 0, true);
							verticesPointers[i] = (verticesPointers[i].index, verticesPointers[i].prev, verticesPointers[prevprev].index, false);
							found = true;
							break;
						}
					}
				}
			}
			if (!found) {
				Debug.Log("Failed on " + string.Join(" ", leftMost) + " " + verticesPointers.Count);
			}
		}
		//The remaining vertices form the last triangle
		List<int> remainingVertices = new List<int>();
		for (int i=0;i<verticesPointers.Count;i++) {
			if (!verticesPointers[i].removed) {
				remainingVertices.Add(verticesPointers[i].index);
			}
		}
		for (int i=0;i<3;i++) {
			triangles.Add(remainingVertices[i]);
		}

		return triangles;

	}

	private static List<List<int>> splitBoundariesByEdges(Vector3[] vertices, List<List<int>> boundaries, List<int> edges, Vector3 localNormal) {

		if (debugging) {
			string msg = localNormal + "\n" + vertices.Length + "\n";
			for (int i=0;i<boundaries.Count;i++) {
				for (int j=0;j<boundaries[i].Count;j++) {
					msg += boundaries[i][j] + " " + vertices[boundaries[i][j]] + " ";
				}
				msg += "\n";
			}
			for (int i=0;i<edges.Count / 2;i++) {
				msg += edges[i * 2] + " " + edges[i * 2 + 1] + "\n";
			}
			Debug.Log(msg);
		}

		debugInt++;

		if (debugging) {
			for (int i=0;i<edges.Count / 2;i++) {
				Vector3 a = vertices[edges[i * 2]];
				Vector3 b = vertices[edges[i * 2 + 1]];
				Debug.DrawLine(
					a + new Vector3(-10, 0, debugInt),
					b + new Vector3(-10, 0, debugInt),
					Color.yellow,
					5000,
					false
				);
			}
			for (int i=0;i<boundaries.Count;i++) {
				for (int j=0;j<boundaries[i].Count;j++) {
					Vector3 a = vertices[boundaries[i][j]];
					Vector3 b = vertices[boundaries[i][(j + 1) % boundaries[i].Count]];
					Debug.DrawLine(
						a + new Vector3(-10, 0, debugInt),
						b + new Vector3(-10, 0, debugInt),
						Color.white,
						5000,
						false
					);
				}
			}
		}

		//Create an adjacency list
		//clockwise = 1: clockwise, = 0: edge, = -1: counterclockwise in result boundaries
		//The direction will be reversed for edges on holes
		List<(int edge, int visitCount, int clockwise)>[] adjList = new List<(int edge, int visitCount, int clockwise)>[vertices.Length];
		for (int i=0;i<vertices.Length;i++) {
			adjList[i] = new List<(int edge, int visitCount, int clockwise)>();
		}
		for (int b=0;b<boundaries.Count;b++) {
			for (int e=0;e<boundaries[b].Count;e++) {
				int u = boundaries[b][e];
				int v = boundaries[b][(e + 1) % boundaries[b].Count];
				//edges on boundaries can be used once
				adjList[u].Add((v, 1, (b == 0 ? 1 : -1)));
				adjList[v].Add((u, 1, (b == 0 ? -1 : 1)));
				// adjList[u].Add((v, 1, ((b == 0 ^ e == boundaries[b].Count - 1) ? 1 : -1)));
				// adjList[v].Add((u, 1, ((b == 0 ^ e == boundaries[b].Count - 1) ? -1 : 1)));
			}
		}
		for (int i=0;i<edges.Count/2;i++) {
			//edges that connected boundaries can be used twice
			adjList[edges[i * 2]].Add((edges[i * 2 + 1], 2, 0));
			adjList[edges[i * 2 + 1]].Add((edges[i * 2], 2, 0));
		}

		List<List<int>> newBoundaries = new List<List<int>>();
		while (true) {
			//Find a starting edge
			(int u, int v) cur = (-1, -1);
			for (int i=0;i<vertices.Length;i++) {
				if (adjList[i].Count > 0) {
					for (int j=0;j<adjList[i].Count;j++) {
						if (adjList[i][j].clockwise == 1 && adjList[i].Count == 2) {
							cur = (i, adjList[i][j].edge);
						}
						else if (adjList[i][j].clockwise == -1 && adjList[adjList[i][j].edge].Count == 2) {
							cur = (adjList[i][j].edge, i);
						}
						if (cur.u != -1) {
							break;
						}
					}
				}
				if (cur.u != -1) {
					break;
				}
			}
			if (cur.u == -1) {
				break;
			}

			List<int> newBoundary = new List<int>();
			int start = cur.u;
			newBoundary.Add(cur.u);
			while (true) {
				//Remove used edge
				for (int i=0;i<adjList[cur.u].Count;i++) {
					if (adjList[cur.u][i].edge == cur.v) {
						adjList[cur.u][i] = (adjList[cur.u][i].edge, adjList[cur.u][i].visitCount - 1, adjList[cur.u][i].clockwise);
						if (adjList[cur.u][i].visitCount == 0) {
							adjList[cur.u].RemoveAt(i);
						}
					}
				}
				for (int i=0;i<adjList[cur.v].Count;i++) {
					if (adjList[cur.v][i].edge == cur.u) {
						adjList[cur.v][i] = (adjList[cur.v][i].edge, adjList[cur.v][i].visitCount - 1, adjList[cur.v][i].clockwise);
						if (adjList[cur.v][i].visitCount == 0) {
							adjList[cur.v].RemoveAt(i);
						}
					}
				}
				if (cur.v == start) {
					break;
				}
				else {
					newBoundary.Add(cur.v);
				}
				//Find the most clockwise edge
				(float maxAngle, int v) maxEdge = (-2147483647, -1);
				Vector3 v1 = vertices[cur.v] - vertices[cur.u];
				for (int i=0;i<adjList[cur.v].Count;i++) {
					int nex = adjList[cur.v][i].edge;
					if (nex != cur.u) {
						Vector3 v2 = vertices[nex] - vertices[cur.v];
						float angle = VectorCalculator.vectorAngle(v1, v2) * (Vector3.Angle(Vector3.Cross(v1, v2).normalized, localNormal) < 45f ? 1 : -1);
						if (angle > maxEdge.maxAngle) {
							maxEdge = (angle, nex);
						}
					}
				}
				cur.u = cur.v;
				cur.v = maxEdge.v;
			}
			newBoundaries.Add(newBoundary);
		}

		return newBoundaries;
	}

	public static void cutByPlane(ref Vector3[] vertices, ref int[] triangles, Vector3 planePos, Vector3 planeNormal) {

		List<Vector3> remainVerticesList = new List<Vector3>();
		List<Vector3> edgeVerticesList = new List<Vector3>();

		//Calculate edge of cutting plane & reconstruct faces along the edge
		int[] triangleSide = new int[triangles.Length / 3]; // -1 left, 0 cross, 1 right
		float pn = Vector3.Dot(planePos, planeNormal);
		for (int i=0;i<triangles.Length / 3;i++) {
			float[] verticesPos = new float[3]{
				Vector3.Dot(vertices[triangles[i * 3 + 0]], planeNormal) - pn,
				Vector3.Dot(vertices[triangles[i * 3 + 1]], planeNormal) - pn,
				Vector3.Dot(vertices[triangles[i * 3 + 2]], planeNormal) - pn
			};
			if (verticesPos[0] <= 0 && verticesPos[1] <= 0 && verticesPos[2] <= 0) {
				triangleSide[i] = -1;
				for (int j=0;j<3;j++) {
					remainVerticesList.Add(vertices[triangles[i * 3 + j]]);
				}
			}
			else if (verticesPos[0] <= 0 || verticesPos[1] <= 0 || verticesPos[2] <= 0) {
				triangleSide[i] = 0;
				for (int j=0;j<3;j++) {
					Vector3[] curVec = new Vector3[3]{
						vertices[triangles[i * 3 + j]],
						vertices[triangles[i * 3 + ((j + 1) % 3)]],
						vertices[triangles[i * 3 + ((j + 2) % 3)]]
					};
					Vector3 newVec1 = VectorCalculator.getLinePlaneIntersection(curVec[0], curVec[2], planePos, planeNormal);
					Vector3 newVec2 = VectorCalculator.getLinePlaneIntersection(curVec[1], curVec[2], planePos, planeNormal);
					if (verticesPos[j] <= 0 && verticesPos[(j + 1) % 3] <= 0) {
						remainVerticesList.Add(curVec[0]);
						remainVerticesList.Add(curVec[1]);
						remainVerticesList.Add(newVec1);
						remainVerticesList.Add(curVec[1]);
						remainVerticesList.Add(newVec2);
						remainVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
					else if (verticesPos[j] > 0 && verticesPos[(j+1)%3] > 0) {
						remainVerticesList.Add(curVec[2]);
						remainVerticesList.Add(newVec1);
						remainVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
				}
			}
		}

		if (edgeVerticesList.Count == 0) {
			return;
		}

		//Extract edge as successive vertices
		List<List<Vector3>> sortedEdgeVerticesList = new List<List<Vector3>>();
		bool[] used = new bool[edgeVerticesList.Count / 2];
		int boundaryCount = 0;

		while (true) {

			int curEdge = -1;
			Vector3 curStart = new Vector3(0, 0, 0);
			Vector3 curVect = new Vector3(0, 0, 0);
			for (int i=0;i<edgeVerticesList.Count / 2;i++) {
				if (!used[i]) {
					curEdge = i;
					curStart = edgeVerticesList[i * 2];
					curVect = edgeVerticesList[i * 2 + 1];
					used[i] = true;
					break;
				}
			}
			if (curEdge == -1) {
				break;
			}

			boundaryCount++;
			sortedEdgeVerticesList.Add(new List<Vector3>());
			sortedEdgeVerticesList[boundaryCount - 1].Add(curStart);

			bool done = false;
			loopCount = 0;
			do {
				for (int j=0;j<edgeVerticesList.Count / 2;j++) {
					if (curEdge != j) {
						for (int k=0;k<2;k++) {
							if (edgeVerticesList[j * 2 + k] == curVect) {
								sortedEdgeVerticesList[boundaryCount - 1].Add(edgeVerticesList[j * 2 + k]);
								curVect = edgeVerticesList[j * 2 + (1 - k)];
								if (curVect == curStart) {
									done = true;
								}
								curEdge = j;
								used[j] = true;
								break;
							}
						}
					}
					if (done) {
						break;
					}
				}
				loopCount++;
				if (loopCount > 1000) {
					Debug.Log("Infinite Loop");
					break;
				}
			} while (!done);

		}

		List<Vector3> allEdgeVerticesList = new List<Vector3>();
		List<List<int>> boundaries = new List<List<int>>();
		int acc = 0;
		for (int i=0;i<sortedEdgeVerticesList.Count;i++) {
			allEdgeVerticesList.AddRange(sortedEdgeVerticesList[i]);
			boundaries.Add(new List<int>());
			for (int j=0;j<sortedEdgeVerticesList[i].Count;j++) {
				boundaries[i].Add(acc);
				acc++;
			}
		}

		int[] cuttingPlaneTriangles = MeshCalculator.triangulationUnorderedBoundaries(allEdgeVerticesList.ToArray(), boundaries, planeNormal);

		Vector3[] newVertices = new Vector3[remainVerticesList.Count + allEdgeVerticesList.Count];
		int[] newTriangles = new int[remainVerticesList.Count + cuttingPlaneTriangles.Length];
		for (int i=0;i<remainVerticesList.Count;i++) {
			newVertices[i] = remainVerticesList[i];
			newTriangles[i] = i;
		}
		for (int i=0;i<allEdgeVerticesList.Count;i++) {
			newVertices[i + remainVerticesList.Count] = allEdgeVerticesList[i];
		}
		for (int i=0;i<cuttingPlaneTriangles.Length;i++) {
			newTriangles[i + remainVerticesList.Count] = cuttingPlaneTriangles[i] + remainVerticesList.Count;
		}

		vertices = newVertices;
		triangles = newTriangles;

	}

}