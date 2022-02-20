using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MeshCalculator {

	private static float eps = 0.0000001f;

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

		vertices = new Vector3[verticesList.Count];
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] = verticesList[i];
		}

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
		triangles = new int[trianglesList.Count];
		for (int i=0;i<triangles.Length;i++) {
			triangles[i] = trianglesList[i];
		}

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
		edges = new int[edgesList.Count];
		for (int i=0;i<edges.Length;i++) {
			edges[i] = edgesList[i];
		}

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

		Vector3 localNormal = VectorCalculator.crossProduct(vertices[triangles[0]] - vertices[triangles[1]], vertices[triangles[0]] - vertices[triangles[2]]).normalized;

		//Offset towards normal direction
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] += localNormal * 0.00001f;
		}

		//Offset towards central
		Vector3[] offsets = new Vector3[vertices.Length];
		for (int i=0;i<boundary.Count;i++) {
			boundary[i] = clockwiseBoundary(ref vertices, boundary[i], localNormal);
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
				offsets[boundary[i][j]] = dir[j] * thickness * (isConvex[j] ^ isOuter ? -1 : 1) / Mathf.Cos(angle[j] / 2);
			}
		}

		for (int i=0;i<vertices.Length;i++) {
			vertices[i] += offsets[i];
		}

		simplifyMesh(ref vertices, ref triangles);

	}

	public static int[] triangulation(ref Vector3[] vertices, ref List<List<int>> boundaries, bool simplify) {

		List<Vector3> la = new List<Vector3>();
		la.Add(new Vector3(0, 0, 0));
		la.Add(new Vector3(1, 0, 0));
		la.Add(new Vector3(2, 0, 0));
		List<Vector3> lb = la;
		lb[1] = new Vector3(1, 2, 3);
		Debug.Log(la[1] + " " + lb[1]);

		List<List<int>> noHolePolygons = new List<List<int>>();

		if (boundaries.Count > 1) {
			noHolePolygons = splitHolePolygon(ref vertices, ref boundaries);
		}
		else {
			noHolePolygons.Add(boundaries[0]);
		}

		List<List<int>> monotonePolygons = new List<List<int>>();

		for (int i=0;i<noHolePolygons.Count;i++) {
			List<int> noHolePolygon = noHolePolygons[i];
			monotonePolygons.AddRange(splitMonotonePolygon(ref vertices, ref noHolePolygon));
		}

		List<int> trianglesList = new List<int>();

		for (int i=0;i<monotonePolygons.Count;i++) {
			List<int> monotonePolygon = monotonePolygons[i];
			trianglesList.AddRange(triangulizeMonotonePolygon(ref vertices, ref monotonePolygon));
		}

		int[] triangles = new int[trianglesList.Count];
		for (int i=0;i<triangles.Length;i++) {
			triangles[i] = trianglesList[i];
		}

		if (simplify) {
			simplifyMesh(ref vertices, ref triangles);
		}

		return triangles;

	}

	private static List<List<int>> splitHolePolygon(ref Vector3[] vertices, ref List<List<int>> boundaries) {

		return null;

	}
	private static List<List<int>> splitMonotonePolygon(ref Vector3[] vertices, ref List<int> boundary) {

		return null;

	}
	private static List<int> triangulizeMonotonePolygon(ref Vector3[] vertices, ref List<int> boundary) {

		return null;

	}

	private static List<int> clockwiseBoundary(ref Vector3[] vertices, List<int> boundary, Vector3 localNormal) {

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

}