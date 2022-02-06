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

}