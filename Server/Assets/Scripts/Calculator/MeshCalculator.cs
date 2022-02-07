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

	public static List<List<int>> extractBoundaries(ref Vector3[] vertices, ref int[] triangleEdges, ref int[] edges, bool debug)  {

		//Boundaries consist of edges that appear only once
		List<List<int>> boundaries = new List<List<int>>();
		int[] appear = new int[edges.Length / 2];
		for (int i=0;i<triangleEdges.Length;i++) {
			appear[triangleEdges[i]]++;
		}

		if (debug) {
			string msg = "";
			for (int i=0;i<appear.Length;i++) {
				msg += appear[i] + " ";
			}
			Debug.Log(msg);
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
				List<int> at = new List<int>();
				List<int> bt = new List<int>();
				for (int i=0;i<boundaries[0].Count;i++) {
					bt.Add(boundaries[0][i]);
				}
				for (int i=0;i<boundaries[maxi].Count;i++) {
					at.Add(boundaries[maxi][i]);
				}
				boundaries[0].Clear();
				boundaries[maxi].Clear();
				for (int i=0;i<at.Count;i++) {
					boundaries[0].Add(at[i]);
				}
				for (int i=0;i<bt.Count;i++) {
					boundaries[maxi].Add(bt[i]);
				}
			}
		}

		return boundaries;
	}

}