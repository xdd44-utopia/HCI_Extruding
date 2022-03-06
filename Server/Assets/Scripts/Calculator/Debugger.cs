using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Debugger : MonoBehaviour
{
    private List<GameObject> lineObj = new List<GameObject>();
	public GameObject linePrefab;
    public bool update;
    void Start() {
        update = false;
    }

    void Update() {
        if (update) {
            update = false;
            splitBoundariesByEdgesDebug();
        }
    }

    private void splitBoundariesByEdgesDebug() {
        Vector3[] vertices = new Vector3[23];
        vertices[0] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[1] = new Vector3(-8.276f, 5.219f, 1.695f);
        vertices[2] = new Vector3(-1.235f, 8.877f, -4.275f);
        vertices[3] = new Vector3(7.041f, 3.658f, -5.970f);
        vertices[4] = new Vector3(8.276f, -5.219f, -1.695f);
        vertices[5] = new Vector3(1.235f, -8.877f, 4.275f);
        vertices[6] = new Vector3(-4.909f, 5.258f, -0.204f);
        vertices[7] = new Vector3(-3.546f, 6.489f, -1.651f);
        vertices[8] = new Vector3(-1.944f, -2.822f, 2.661f);
        vertices[9] = new Vector3(3.823f, -3.858f, 0.026f);
        vertices[10] = new Vector3(3.461f, 1.776f, -2.922f);
        vertices[11] = new Vector3(5.850f, 2.721f, -4.782f);
        vertices[12] = new Vector3(3.907f, -6.464f, 1.437f);
        vertices[13] = new Vector3(-4.323f, -3.931f, 4.608f);
        vertices[14] = new Vector3(-0.606f, -2.017f, 1.466f);
        vertices[15] = new Vector3(-0.822f, -0.533f, 0.756f);
        vertices[16] = new Vector3(2.096f, -1.868f, -0.124f);
        vertices[17] = new Vector3(1.315f, 0.322f, -0.913f);
        vertices[18] = new Vector3(-1.173f, 1.551f, -0.214f);
        vertices[19] = new Vector3(1.964f, 2.013f, -2.220f);
        vertices[20] = new Vector3(2.243f, 3.682f, -3.309f);
        vertices[21] = new Vector3(-1.770f, 3.326f, -0.873f);
        vertices[22] = new Vector3(-1.959f, 6.680f, -2.643f);
        List<List<int>> boundaries = new List<List<int>>();
        boundaries.Add(new List<int>{0, 1, 2, 3, 4, 5});
        boundaries.Add(new List<int>{6, 7, 8, 9, 10, 11, 12, 13});
        boundaries.Add(new List<int>{14, 15, 16});
        boundaries.Add(new List<int>{17, 18, 19});
        boundaries.Add(new List<int>{20, 21, 22});
        List<int> edges = new List<int>{1, 6, 7, 18, 15, 17, 2, 22};
        List<List<int>> newBoundaries = MeshCalculator.splitBoundariesByEdges(ref vertices, ref boundaries, ref edges);

        Vector2[] verticesXY = VectorCalculator.facePlaneFront(vertices);

		List<List<int>> monotonePolygons = new List<List<int>>();
        for (int i=0;i<newBoundaries.Count;i++) {
            List<int> boundary = newBoundaries[i];
            monotonePolygons.AddRange(MeshCalculator.splitMonotonePolygon(ref vertices, ref boundary));
        }

        displayBoundaries(ref vertices, ref monotonePolygons, new Vector3(0, 0, 0));
    }

    private void displayBoundaries(ref Vector3[] vertices, ref List<List<int>> boundaries, Vector3 offset) {
        Vector3 localNormal = VectorCalculator.crossProduct(vertices[boundaries[0][1]] - vertices[boundaries[0][0]], vertices[boundaries[0][2]] - vertices[boundaries[0][1]]).normalized;
        while (boundaries.Count > lineObj.Count) {
			lineObj.Add(Instantiate(linePrefab, new Vector3(0, 0, 0), Quaternion.identity));
			lineObj[lineObj.Count - 1].transform.parent = this.transform;
		}

		for (int i=0;i<boundaries.Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = boundaries[i].Count + 1;
			lr.startWidth = transform.localScale.magnitude * 0.0125f;
			lr.endWidth = transform.localScale.magnitude * 0.0125f;
			for (int j=0;j<=boundaries[i].Count;j++) {
				lr.SetPosition(j, transform.TransformPoint(vertices[boundaries[i][j % boundaries[i].Count]]) + offset + localNormal * 0.1f * j);
			}
		}

		for (int i=boundaries.Count;i<lineObj.Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = 0;
		}
    }
}
