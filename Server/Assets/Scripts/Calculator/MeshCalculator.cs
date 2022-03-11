using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MeshCalculator {

	private static float eps = 0.01f;
	public static bool debugging = false;

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
				if ((vertices[j] - vertices[i]).magnitude < eps) {
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

	public static void extractEdges(ref int[] triangles, out int[] edges, out int[] triangleEdges) {

		//Calculate edges
		List<int> edgesList = new List<int>();
		triangleEdges = new int[triangles.Length];
		for (int i=0;i<triangles.Length / 3;i++) {
			for (int j=0;j<3;j++) {
				int u = triangles[i * 3 + j];
				int v = triangles[i * 3 + (j + 1) % 3];
				if (u > v) {
					int t = u; u = v; v = t;
				}
				triangleEdges[i * 3 + j] = -1;
				for (int k=0;k<edgesList.Count / 2;k++) {
					if (edgesList[k * 2] == u && edgesList[k * 2 + 1] == v) {
						triangleEdges[i * 3 + j] = k;
						break;
					}
				}
				if (triangleEdges[i * 3 + j] == -1) {
					triangleEdges[i * 3 + j] = edgesList.Count / 2;
					edgesList.Add(u);
					edgesList.Add(v);
				}
			}
		}
		edges = edgesList.ToArray();

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

		Vector3 localNormal = VectorCalculator.crossProduct(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]).normalized;

		//Offset towards normal direction
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] += localNormal * 0.001f;
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
				isConvex[j] = (Mathf.Abs((VectorCalculator.crossProduct(va, vb).normalized - localNormal).magnitude) < eps);
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

		Vector2[] vertices2D = VectorCalculator.facePlaneFront(vertices);

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
			int[] triangles = triangulation(vertices, boundaries, localNormal);
			trianglesList.AddRange(triangles);
		}
		return trianglesList.ToArray();

	}

	public static int[] triangulation(Vector3[] vertices, List<List<int>> boundaries, Vector3 localNormal) {

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

		List<int> noHolePolygon = boundaries.Count > 1 ? splitHolePolygon(vertices, boundaries) : boundaries[0];
		if (localNormal.magnitude < eps) {
			localNormal = VectorCalculator.crossProduct(vertices[noHolePolygon[1]] - vertices[noHolePolygon[0]], vertices[noHolePolygon[2]] - vertices[noHolePolygon[1]]).normalized;
		}
		List<List<int>> monotonePolygons = splitMonotonePolygon(vertices, noHolePolygon);

		if (debugging) {
			for (int i=0;i<monotonePolygons.Count;i++) {
				for (int j=0;j<monotonePolygons[i].Count;j++) {
					Debug.DrawLine(
						vertices[monotonePolygons[i][j]] + new Vector3(-40, 0, 0),
						vertices[monotonePolygons[i][(j + 1) % monotonePolygons[i].Count]] + new Vector3(-40, 0, 0),
						Color.white,
						5000
					);
				}
			}
		}

		List<int> trianglesList = new List<int>();

		for (int i=0;i<monotonePolygons.Count;i++) {
			List<int> monotonePolygon = clockwiseBoundary(vertices, monotonePolygons[i], localNormal);
			trianglesList.AddRange(triangulizeMonotonePolygon(vertices, monotonePolygon));
		}

		int[] triangles = trianglesList.ToArray();

		if (debugging) {
			for (int i=0;i<triangles.Length/3;i++) {
				for (int j=0;j<3;j++) {
					Debug.DrawLine(
						vertices[triangles[i * 3 + j]] + new Vector3(-60, 0, 0),
						vertices[triangles[i * 3 + (j + 1) % 3]] + new Vector3(-60, 0, 0),
						Color.white,
						5000
					);
				}
			}
		}

		int[] trianglesNormalized = checkTriangleNormal(vertices, triangles, localNormal);

		return checkTriangleNormal(vertices, triangles, localNormal);

	}

	private static List<int> splitHolePolygon(Vector3[] vertices, List<List<int>> boundaries) {

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
		return splitBoundariesByEdges(vertices, boundaries, edges)[0];

	}
	private static List<List<int>> splitMonotonePolygon(Vector3[] originalVertices, List<int> originalBoundary) {

		//Force duplicate reused vertices and offset
		Vector3 localNormal = VectorCalculator.crossProduct(originalVertices[originalBoundary[1]] - originalVertices[originalBoundary[0]], originalVertices[originalBoundary[2]] - originalVertices[originalBoundary[1]]).normalized;
		bool[] used = new bool[originalVertices.Length];
		List<Vector3> newVerticesList = new List<Vector3>();
		List<int> pointerToOriginal = new List<int>();
		for (int i=0;i<originalVertices.Length;i++) {
			newVerticesList.Add(originalVertices[i]);
			pointerToOriginal.Add(i);
		}
		List<int> boundary = new List<int>();
		for (int i=0;i<originalBoundary.Count;i++) {
			if (!used[originalBoundary[i]]) {
				boundary.Add(originalBoundary[i]);
				used[originalBoundary[i]] = true;
			}
			else {
				newVerticesList.Add(originalVertices[originalBoundary[i]]);
				pointerToOriginal.Add(originalBoundary[i]);
				boundary.Add(newVerticesList.Count - 1);
			}
		}
		
		Vector3[] offsetVertices = newVerticesList.ToArray();
		offsetBoundary(ref offsetVertices, new List<List<int>> {boundary}, localNormal, 0.01f);
		//Rotate the vertices to x-y plane
		Vector2[] vertices = VectorCalculator.facePlaneFront(offsetVertices);

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
		//Edge pointers sorted by y
		List<int> intersectingEdges = new List<int>();
		for (int i=0;i<verticePointers.Count;i++) {
			int cur = verticePointers[i];
			List<float> intersectingY = new List<float>();
			for (int j=0;j<intersectingEdges.Count;j++) {
				intersectingY.Add(VectorCalculator.getLineIntersection(
					vertices[boundary[intersectingEdges[j]]],
					vertices[boundary[(intersectingEdges[j] + 1) % boundary.Count]],
					vertices[boundary[cur]],
					vertices[boundary[cur]] + new Vector2(0, 1)
				).y);
			}
			int pre = (cur + boundary.Count - 1) % boundary.Count;
			int nex = (cur + 1) % boundary.Count;
			if ((vertices[boundary[pre]] - vertices[boundary[cur]]).normalized.y > (vertices[boundary[nex]] - vertices[boundary[cur]]).normalized.y) {
				int t = pre; pre = nex; nex = t;
			}
			if (vertices[boundary[pre]].x >= vertices[boundary[cur]].x) {
				int pointer = 0;
				while (pointer < intersectingEdges.Count && vertices[boundary[cur]].y >= intersectingY[pointer]) {
					pointer++;
				}
				int edge = pre == 0 || cur == 0 ? boundary.Count - 1 : Math.Min(pre, cur);
				intersectingEdges.Insert(pointer, edge);
				intersectingY.Insert(pointer, vertices[boundary[cur]].y);
			}
			else {
				int edge = pre == 0 || cur == 0 ? boundary.Count - 1 : Math.Min(pre, cur);
				int j = intersectingEdges.FindIndex(a => a == edge);
				intersectingEdges.RemoveAt(j);
				intersectingY.RemoveAt(j);
			}
			if (vertices[boundary[nex]].x >= vertices[boundary[cur]].x) {
				int pointer = 0;
				while (pointer < intersectingEdges.Count && vertices[boundary[cur]].y >= intersectingY[pointer]) {
					pointer++;
				}
				int edge = nex == 0 || cur == 0 ? boundary.Count - 1 : Math.Min(nex, cur);
				intersectingEdges.Insert(pointer, edge);
				intersectingY.Insert(pointer, vertices[boundary[cur]].y);
			}
			else {
				int edge = nex == 0 || cur == 0 ? boundary.Count - 1 : Math.Min(nex, cur);
				int j = intersectingEdges.FindIndex(a => a == edge);
				intersectingEdges.RemoveAt(j);
				intersectingY.RemoveAt(j);
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

		if (debugging) {
			for (int i=0;i<boundary.Count;i++) {
				Debug.DrawLine(
					offsetVertices[boundary[i]] + new Vector3(-20, 0, 0),
					offsetVertices[boundary[(i + 1) % boundary.Count]] + new Vector3(-20, 0, 0),
					Color.white,
					5000
				);
			}
		}

		List<List<int>> boundaries = new List<List<int>>{ boundary };
		List<List<int>> offsetBoundaries = splitBoundariesByEdges(offsetVertices, boundaries, edges);
		List<List<int>> newBoundaries = new List<List<int>>();
		for (int i=0;i<offsetBoundaries.Count;i++) {
			newBoundaries.Add(new List<int>());
			for (int j=0;j<offsetBoundaries[i].Count;j++) {
				newBoundaries[i].Add(pointerToOriginal[offsetBoundaries[i][j]]);
			}
		}
		return newBoundaries;

	}
	private static List<int> triangulizeMonotonePolygon(Vector3[] originalVertices, List<int> boundary) {

		Vector3 localNormal = VectorCalculator.crossProduct(originalVertices[boundary[1]] - originalVertices[boundary[0]], originalVertices[boundary[2]] - originalVertices[boundary[1]]).normalized;
		Vector2[] vertices = VectorCalculator.facePlaneFront(originalVertices);

		//Algorithm:
		//Sort vertices by x
		//From left to right, connect each vertex with all vertices at the left side of it, and trim the formed triangles off the polygon

		//Use prev to keep track of its left adjacent vertex
		//Note that the first vertex has both adjacent vertices on its right, and the last vertex has both on its left
		List<(int index, int prev, bool removed)> verticesPointers = new List<(int index, int prev, bool removed)>();
		List<bool> removed = new List<bool>();
		for (int i=0;i<boundary.Count;i++) {
			int prev = vertices[boundary[(i + boundary.Count - 1) % boundary.Count]].x < vertices[boundary[(i + 1) % boundary.Count]].x ? boundary[(i + boundary.Count - 1) % boundary.Count] : boundary[(i + 1) % boundary.Count];
			verticesPointers.Add((boundary[i], prev, false));
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
			for (int i=leftMost[2];i<verticesPointers.Count;i++) {
				if (!verticesPointers[i].removed) {
					int prev = verticesPointers.FindIndex(a => a.index == verticesPointers[i].prev);
					int prevprev = prev == leftMost[0] ? leftMost[1] : verticesPointers.FindIndex(a => a.index == verticesPointers[prev].prev);
					if (VectorCalculator.isLineSegmentInsidePolygon(vertices, boundary, vertices[verticesPointers[i].index], vertices[verticesPointers[prevprev].index])) {
						triangles.Add(verticesPointers[prev].index);
						triangles.Add(verticesPointers[prevprev].index);
						triangles.Add(verticesPointers[i].index);
						verticesPointers[prev] = (0, 0, true);
						verticesPointers[i] = (verticesPointers[i].index, verticesPointers[prevprev].index, false);
						break;
					}
				}
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

	private static List<List<int>> splitBoundariesByEdges(Vector3[] vertices, List<List<int>> boundaries, List<int> edges) {

		Vector3 localNormal = VectorCalculator.crossProduct(vertices[boundaries[0][1]] - vertices[boundaries[0][0]], vertices[boundaries[0][2]] - vertices[boundaries[0][1]]).normalized;

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
						if (adjList[i][j].clockwise == 1) {
							cur = (i, adjList[i][0].edge);
						}
						else if (adjList[i][j].clockwise == -1) {
							cur = (adjList[i][0].edge, i);
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
					Vector3 v2 = vertices[nex] - vertices[cur.v];
					float angle = VectorCalculator.vectorAngle(v1, v2) * (Mathf.Abs((VectorCalculator.crossProduct(v1, v2).normalized - localNormal).magnitude) < eps ? 1 : -1);
					if (angle > maxEdge.maxAngle) {
						maxEdge = (angle, nex);
					}
				}
				cur.u = cur.v;
				cur.v = maxEdge.v;
			}
			newBoundaries.Add(newBoundary);
		}

		return newBoundaries;
	}

	private static List<int> clockwiseBoundary(Vector3[] vertices, List<int> boundary, Vector3 localNormal) {

		float angleAcc = 0;
		for (int i=0;i<boundary.Count;i++) {
			Vector3 va = (vertices[boundary[i]] - vertices[boundary[(i + boundary.Count - 1) % boundary.Count]]).normalized;
			Vector3 vb = (vertices[boundary[(i + 1) % boundary.Count]] - vertices[boundary[i]]).normalized;
			bool isConvex = (Mathf.Abs((VectorCalculator.crossProduct(va, vb).normalized - localNormal).magnitude) < eps);
			angleAcc += VectorCalculator.vectorAngle(va, vb) * (isConvex ? 1 : -1);
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
			if ((VectorCalculator.crossProduct(vertices[triangles[i * 3 + 1]] - vertices[triangles[i * 3]], vertices[triangles[i * 3 + 2]] - vertices[triangles[i * 3]]).normalized - localNormal).magnitude < eps) {
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

}