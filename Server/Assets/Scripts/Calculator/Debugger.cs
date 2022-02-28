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
        vertices[5] = new Vector3(1.235f, -8.877, 4.275f);
        vertices[6] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[7] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[8] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[9] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[10] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[11] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[12] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[13] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[14] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[15] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[16] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[17] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[18] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[19] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[20] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[21] = new Vector3(-7.041f, -3.658f, 5.970f);
        vertices[22] = new Vector3(-7.041f, -3.658f, 5.970f);
    }

    private void displayBoundaries(ref Vector3[] vertices, ref List<List<int>> boundaries, Vector3 offset) {
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
				lr.SetPosition(j, transform.TransformPoint(vertices[boundaries[i][j % boundaries[i].Count]]) + offset + new Vector3(0.1f, 0, 0) * i);
			}
		}

		for (int i=boundaries.Count;i<lineObj.Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = 0;
		}
    }
}
