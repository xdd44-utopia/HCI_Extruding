﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour
{

	public GameObject inside;
	public Text debugText;

	//Mesh
	private Mesh mesh;
	private Vector3[] vertices;
	private int[] triangles;
	private int[] edges;
	private int[] triangleEdges;
	private List<List<int>> faces;
	private List<List<List<int>>> boundaries;

	//Additional components
	private List<GameObject> faceObj = new List<GameObject>();
	public GameObject facePrefab;
	private List<GameObject> lineObj = new List<GameObject>();
	public GameObject linePrefab;

	//Record
	private int prevSnap = -1;
	private int prevAlign = -1;

	//Colors
	private Color generalColor = new Color(1f, 1f, 1f, 1f);
	// private Color selectColor = new Color(1f, 1f, 0f, 1f);
	private Color snapColor = new Color(1f, 1f, 0f, 1f);
	private Color alignColor = new Color(0f, 1f, 0f, 1f);

	private const float eps = 0.05f;


	private float timer = 0;
	private int testInt = 0;
	private bool testBool = true;

	void Start()
	{
		GameObject findObject;
		inside = GameObject.Find("Inside");

		debugText = GameObject.Find("DebugText").GetComponent<Text>();
	}

	void Update() {

		debugText.text = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + "";

	}

	private void categorizeFaces() {

		// Categorize triangles into faces
		int num = triangles.Length / 3;
		bool[] isCategorized = new bool[triangles.Length];
		faces = new List<List<int>>();
		for (int i=0;i<num;i++) {
			if (!isCategorized[i]) {
				List<int> newFace = new List<int>();
				Stack<int> bfs = new Stack<int>();
				bfs.Push(i);
				while (bfs.Count > 0) { //Sharing edge && coplanar
					int cur = bfs.Pop();
					if (isCategorized[cur]) {
						continue;
					}
					newFace.Add(cur);
					isCategorized[cur] = true;
					for (int j=0;j<num;j++) {
						if (!isCategorized[j] && isTrianglesSameFace(cur, j)) {
							bfs.Push(j);
						}
					}
				}
				faces.Add(newFace);
			}
		}

	}

	private void extractBoundaries() {

		if (boundaries == null) {
			boundaries = new List<List<List<int>>>();
		}
		else {
			boundaries.Clear();
		}
		for (int i=0;i<faces.Count;i++) {
			int[] faceTriangleEdges = new int[faces[i].Count * 3];
			for (int j=0;j<faces[i].Count;j++) {
				for (int k=0;k<3;k++) {
					faceTriangleEdges[j * 3 + k] = triangleEdges[faces[i][j] * 3 + k];
				}
			}
			boundaries.Add(MeshCalculator.extractBoundaries(ref vertices, ref faceTriangleEdges, ref edges));
		}

	}

	private void generateCover() {

		// Generate cover gameObjects
		while (faces.Count > faceObj.Count) {
			faceObj.Add(Instantiate(facePrefab, new Vector3(0, 0, 0), Quaternion.identity));
			faceObj[faceObj.Count - 1].transform.parent = this.transform;
			faceObj[faceObj.Count - 1].transform.localScale = new Vector3(1f, 1f, 1f);
			faceObj[faceObj.Count - 1].transform.localPosition = new Vector3(0, 0, 0);
			faceObj[faceObj.Count - 1].transform.localRotation = Quaternion.identity;
			faceObj[faceObj.Count - 1].GetComponent<MeshFilter>().mesh = new Mesh();
		}
		while (faces.Count < faceObj.Count) {
			GameObject temp = faceObj[faceObj.Count - 1];
			faceObj.RemoveAt(faceObj.Count - 1);
			Destroy(temp, 0);
		}

		for (int i=0;i<faces.Count;i++) {
			Vector3[] faceVertices = new Vector3[vertices.Length];
			for (int j=0;j<vertices.Length;j++) {
				faceVertices[j] = vertices[j];
			}
			int[] faceTriangles = new int[faces[i].Count * 3];
			for (int j=0;j<faces[i].Count;j++) {
				for (int k=0;k<3;k++) {
					faceTriangles[j * 3 + k] = triangles[faces[i][j] * 3 + k];
				}
			}
			List<List<int>> boundary = boundaries[i];
			MeshCalculator.generateFaceCover(ref faceVertices, ref faceTriangles, ref boundary, 0.005f);
			Mesh mesh = faceObj[i].GetComponent<MeshFilter>().mesh;
			mesh.Clear();
			mesh.vertices = faceVertices;
			mesh.triangles = faceTriangles;
			mesh.uv = new Vector2[faceVertices.Length];
			mesh.MarkModified();
			mesh.RecalculateNormals();
			faceObj[i].name = "Face " + i;
			faceObj[i].GetComponent<MeshFilter>().mesh = mesh;
		}

	}

	private void synchronize() {

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = new Vector2[vertices.Length];
		mesh.MarkModified();
		mesh.RecalculateNormals();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;
		inside.GetComponent<MeshFilter>().mesh = mesh;

	}

	public void updateSelect(int selectFace) { // -2 Preserve, -1 Cancel, i Update

		if (selectFace == -2) {
			return;
		}

		if (selectFace == -1 || selectFace >= faces.Count) {
			for (int i=0;i<lineObj.Count;i++) {
				LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
				lr.positionCount = 0;
			}
			return;
		}

		// Generate line gameObjects
		while (boundaries[selectFace].Count > lineObj.Count) {
			lineObj.Add(Instantiate(linePrefab, new Vector3(0, 0, 0), Quaternion.identity));
			lineObj[lineObj.Count - 1].transform.parent = this.transform;
			lineObj[lineObj.Count - 1].transform.localPosition = new Vector3(0, 0, 0);
			lineObj[lineObj.Count - 1].transform.localScale = new Vector3(1, 1, 1);
			lineObj[lineObj.Count - 1].transform.localRotation = Quaternion.identity;
		}

		for (int i=0;i<boundaries[selectFace].Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = boundaries[selectFace][i].Count + 1;
			lr.startWidth = transform.localScale.magnitude * 0.0125f;
			lr.endWidth = transform.localScale.magnitude * 0.0125f;
			for (int j=0;j<=boundaries[selectFace][i].Count;j++) {
				lr.SetPosition(j, vertices[boundaries[selectFace][i][j % boundaries[selectFace][i].Count]]);
			}
		}

		for (int i=boundaries[selectFace].Count;i<lineObj.Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = 0;
		}

	}

	public void updateHighlight(int snapFace, int alignFace) {

		for (int i=0;i<faceObj.Count;i++) {
			faceObj[i].GetComponent<Renderer>().material.SetColor("_Color", generalColor);
		}

		if (snapFace == -2) {
			snapFace = prevSnap;
		}
		prevSnap = snapFace;
		if (snapFace != -1) {
			if (snapFace >= 0 && snapFace < faceObj.Count) {
				faceObj[snapFace].GetComponent<Renderer>().material.SetColor("_Color", snapColor);
			}
		}

		if (alignFace == -2) {
			alignFace = prevSnap;
		}
		prevAlign = alignFace;
		if (alignFace != -1) {
			if (alignFace >= 0 && alignFace < faceObj.Count) {
				faceObj[alignFace].GetComponent<Renderer>().material.SetColor("_Color", alignColor);
			}
		}

	}

	public void updateMesh(string msg) {
		
		mesh = gameObject.GetComponent<MeshFilter>().mesh;
		vertices = mesh.vertices;
		triangles = mesh.triangles;

		string[] temp1 = msg.Split('\n');

		int verticesNum = System.Convert.ToInt32(temp1[1]);
		string[] verticesStr = temp1[2].Split(',');
		vertices = new Vector3[verticesNum];
		for (int i=0;i<verticesNum;i++) {
			vertices[i] = new Vector3(
				System.Convert.ToSingle(verticesStr[i * 3 + 0]),
				System.Convert.ToSingle(verticesStr[i * 3 + 1]),
				System.Convert.ToSingle(verticesStr[i * 3 + 2])
			);
		}

		int trianglesNum = System.Convert.ToInt32(temp1[3]);
		string[] trianglesStr = temp1[4].Split(',');
		triangles = new int[trianglesNum];
		for (int i=0;i<trianglesNum;i++) {
			triangles[i] = System.Convert.ToInt32(trianglesStr[i]);
		}
		
		MeshCalculator.simplifyMesh(ref vertices, ref triangles);
		MeshCalculator.extractEdges(ref triangles, out edges, out triangleEdges);
		categorizeFaces();
		extractBoundaries();
		generateCover();
		synchronize();

	}

	public void updateTransform(string msg) {

		string[] temp1 = msg.Split('\n');

		string[] positionStr = temp1[1].Split(',');
		string[] rotationStr = temp1[2].Split(',');
		string[] scaleStr = temp1[3].Split(',');

		this.transform.position = new Vector3(
			System.Convert.ToSingle(positionStr[0]),
			System.Convert.ToSingle(positionStr[1]),
			System.Convert.ToSingle(positionStr[2])
		);

		this.transform.position = VectorCalculator.convertFromServer(this.transform.position);
		Quaternion currentRot = this.transform.rotation;
		currentRot = Quaternion.Euler(
			System.Convert.ToSingle(rotationStr[0]),
			System.Convert.ToSingle(rotationStr[1]),
			System.Convert.ToSingle(rotationStr[2])
		);
		currentRot = Quaternion.AngleAxis(- VectorCalculator.angle * 180 / Mathf.PI, new Vector3(0, 1, 0)) * currentRot;
		this.transform.rotation = currentRot;
		this.transform.localScale = new Vector3(
			System.Convert.ToSingle(scaleStr[0]),
			System.Convert.ToSingle(scaleStr[1]),
			System.Convert.ToSingle(scaleStr[2])
		);

	}

	private bool isTrianglesSameFace(int a, int b) {
		for (int i=0;i<3;i++) {
			for (int j=0;j<3;j++) {
				if (triangleEdges[a * 3 + i] == triangleEdges[b * 3 + j]) { //sharing edge
					int edgeShared = triangleEdges[a * 3 + i];
					int vertexA = triangles[a * 3 + (i + 2) % 3];
					int vertexB = triangles[b * 3 + (j + 2) % 3];
					Vector3 v1 = vertices[vertexA] - vertices[edges[edgeShared * 2]];
					Vector3 v2 = vertices[vertexA] - vertices[edges[edgeShared * 2 + 1]];
					Vector3 v3 = vertices[vertexA] - vertices[vertexB];
					if (Mathf.Abs(Vector3.Dot(v1.normalized, Vector3.Cross(v2.normalized, v3.normalized).normalized)) < eps) {
						return true;
					}
				}
			}
		}
		return false;
	}

	private int findFaceIndex(int triangleIndex) {

		for (int i=0;i<faces.Count;i++) {
			for (int j=0;j<faces[i].Count;j++) {
				if (faces[i][j] == triangleIndex) {
					return i;
				}
			}
		}

		return triangleIndex;
	}

}
