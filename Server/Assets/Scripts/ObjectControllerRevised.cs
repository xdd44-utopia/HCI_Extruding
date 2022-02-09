﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectControllerRevised : MonoBehaviour
{

	private GameObject inside;
	private ServerController sender;
	private MeshManipulator meshManipulator;
	[HideInInspector]
	public bool isRealMeasure;
	[HideInInspector]
	public Vector3 realMeasure = new Vector3(1, 1, 1);

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

	private const float eps = 0.0000001f;


	private float timer = 0;
	private int testInt = 0;
	private bool testBool = true;

	void Start()
	{
		GameObject findObject;
		inside = GameObject.Find("Inside");
		findObject = GameObject.Find("MeshManipulator");
		if (findObject != null) {
			meshManipulator = findObject.GetComponent<MeshManipulator>();
		}
		findObject = GameObject.Find("Server");
		if (findObject != null) {
			sender = findObject.GetComponent<ServerController>();
		}

		isRealMeasure = false;

		updateMesh(true);
		updateTransform();
	}

	void Update() {

		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out hit)) {
			updateSelect(hit.triangleIndex);
		}
		else {
			updateSelect(-1);
		}

		timer += Time.deltaTime;
		if (timer > 0.4f) {
			updateHighlight((int)Random.Range(0, triangles.Length / 3), (int)Random.Range(0, triangles.Length / 3));
			timer = 0;
		}

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

	private void sendMesh() {

		if (sender == null) {
			return;
		}

		string msg = "Mesh\n";
		msg += vertices.Length + "\n";
		for (int j=0;j<vertices.Length;j++) {
			msg += vertices[j].x + "," + vertices[j].y + "," + vertices[j].z + ",";
		}
		msg += "\n";
		msg += triangles.Length + "\n";
		for (int j=0;j<triangles.Length;j++) {
			msg += triangles[j] + ",";
		}
		msg += "\n";
		sender.sendMessage(msg);

	}

	public void updateSelect(int selectTriangle) { // -1 Cancel, i Update

		int selectFace = findFaceIndex(selectTriangle);

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
		}

		for (int i=0;i<boundaries[selectFace].Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = boundaries[selectFace][i].Count + 1;
			lr.startWidth = transform.localScale.magnitude * 0.0125f;
			lr.endWidth = transform.localScale.magnitude * 0.0125f;
			for (int j=0;j<=boundaries[selectFace][i].Count;j++) {
				lr.SetPosition(j, transform.TransformPoint(vertices[boundaries[selectFace][i][j % boundaries[selectFace][i].Count]]));
			}
		}

		for (int i=boundaries[selectFace].Count;i<lineObj.Count;i++) {
			LineRenderer lr = lineObj[i].GetComponent<LineRenderer>();
			lr.positionCount = 0;
		}

	}

	public void updateHighlight(int snapTriangle, int alignTriangle) { // -2 Preserve, -1 Cancel, i Update

		int snapFace = findFaceIndex(snapTriangle);
		int alignFace = findFaceIndex(alignTriangle);

		for (int i=0;i<faceObj.Count;i++) {
			faceObj[i].GetComponent<Renderer>().material.SetColor("_Color", generalColor);
		}

		if (snapFace >= 0 && snapFace < faceObj.Count) {
			faceObj[snapFace].GetComponent<Renderer>().material.SetColor("_Color", snapColor);
			prevSnap = snapFace;
		}
		else if (snapFace == -2 && prevSnap >= 0) {
			faceObj[prevSnap].GetComponent<Renderer>().material.SetColor("_Color", snapColor);
		}
		else {
			prevSnap = -1;
		}
		
		if (alignFace >= 0 && alignFace < faceObj.Count) {
			faceObj[alignFace].GetComponent<Renderer>().material.SetColor("_Color", alignColor);
			prevAlign = alignFace;
		}
		else if (alignFace == -2 && prevAlign >= 0) {
			faceObj[prevAlign].GetComponent<Renderer>().material.SetColor("_Color", alignColor);
		}
		else {
			prevAlign = -1;
		}

	}

	public void updateMesh(bool isGeometryUpdated) {

		mesh = GetComponent<MeshFilter>().mesh;
		vertices = mesh.vertices;
		triangles = mesh.triangles;
		if (isGeometryUpdated) {
			MeshCalculator.simplifyMesh(ref vertices, ref triangles);
			MeshCalculator.extractEdges(ref triangles, out edges, out triangleEdges);
			categorizeFaces();
			extractBoundaries();
		}
		generateCover();
		synchronize();
		sendMesh();

	}

	public void updateTransform() {

		if (sender == null) {
			return;
		}

		string msg =
			"Transform\n" + 
			this.transform.position.x + "," +
			this.transform.position.y + "," +
			this.transform.position.z + "\n" +
			this.transform.rotation.eulerAngles.x + "," +
			this.transform.rotation.eulerAngles.y + "," +
			this.transform.rotation.eulerAngles.z + "\n" +
			this.transform.localScale.x + "," +
			this.transform.localScale.y + "," +
			this.transform.localScale.z + "\n";
		sender.GetComponent<ServerController>().sendMessage(msg);

	}

	private bool isTrianglesSameFace(int a, int b) {
		for (int i=0;i<3;i++) {
			for (int j=0;j<3;j++) {
				if (triangleEdges[a * 3 + i] == triangleEdges[b * 3 + j]) { //sharing edge
					int edgeShared = triangleEdges[a * 3 + i];
					int vertexA = triangles[a * 3 + (i + 2) % 3];
					int vertexB = triangles[b * 3 + (j + 2) % 3];
					Vector3 vectorA = vertices[vertexA] - vertices[edges[edgeShared * 2]];
					Vector3 vectorB = vertices[vertexB] - vertices[edges[edgeShared * 2]];
					Vector3 vectorShared = vertices[edges[edgeShared * 2]] - vertices[edges[edgeShared * 2 + 1]];
					Vector3 normalA = VectorCalculator.crossProduct(vectorA, vectorShared).normalized;
					Vector3 normalB = VectorCalculator.crossProduct(vectorB, vectorShared).normalized;
					if ((normalA - normalB).magnitude < eps || (normalA + normalB).magnitude < eps) {
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
