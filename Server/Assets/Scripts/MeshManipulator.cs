﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MeshManipulator : MonoBehaviour
{
	public Camera cam;
	public Text debugText;
	public GameObject panVisualizer;
	public GameObject objectManager;

	[HideInInspector]
	public Vector3 touchPosition;
	private Vector3 prevTouchPosition;
	private float touchTimer = 0;
	private float touchDelayTolerance = 0.1f;
	
	[HideInInspector]
	public GameObject hitObj;
	private Mesh hitMesh;
	private Vector3[] hitVertices;
	private Vector2[] hituv;
	private int[] hitTriangles;
	private Transform hitTransform;
	private int hitVerticesNum;
	private int hitTrianglesNum;
	private Renderer hitRenderer;
	private Vector3 centerToHitFace;

	private Mesh highlight;
	private MeshRenderer mr;

	private bool isHit = false;

	//selection
	private List<int> selected = new List<int>();
	private int selectedFaceIndex;
	private int faceNum;
	private Vector3 selectedNormal;
	private Vector3[] selectedVertices = new Vector3[3];

	private List<int> edgeVertices;
	private int edgeLength;
	
	//focus
	private Vector3 axisToFocus;
	private float angleToFocus;
	private Vector3 posToFocus;
	private const float focusSpeed = 25;

	//extrude
	private Mesh extrudedMesh;
	private float extrudeDist = 0.2f;
	private Vector3[] extrudedVertices;
	private int[] extrudedTriangles;

	//taper
	private Mesh taperedMesh;
	private Vector3[] taperedVertices;
	private int[] taperedTriangles;
	private Vector3 taperCenter;
	private float taperScale;
	private bool taperStarted = false;
	private float taperTimer = 0;

	private Status state = Status.freemove;
	private enum Status {
		freemove,
		select,
		focus,
		extrude,
		taper
	}

	private SelectMode smode = SelectMode.selectFace;
	private enum SelectMode {
		selectFace,
		selectObject
	}

	/* #region Main */
	void Start()
	{
		highlight = new Mesh();
		GetComponent<MeshFilter>().mesh = highlight;
		mr = GetComponent<MeshRenderer>();

		highlight.vertices = new Vector3[3];
		highlight.uv = new Vector2[3];
		highlight.triangles = new int[]{0, 1, 2};

		touchPosition = new Vector3(10000, 10000, 10000);
	}

	void Update() {

		if (state == Status.freemove) {
			GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Object");
			foreach (GameObject obj in allObjects) {
				Renderer tempRenderer = obj.GetComponent<Renderer>();
				tempRenderer.material.SetColor("_Color", Color.white);
			}
		}

		bool usePreviousTouch = false;
		if (touchPosition.magnitude > 10000 && touchTimer >= 0) {
			touchPosition = prevTouchPosition;
			usePreviousTouch = true;
		}

		switch(state) {
			case Status.freemove:
				castRay();
				break;
			case Status.focus:
				focus();
				break;
			case Status.extrude:
				extrudeDist = (Input.mousePosition.y - 80) / 100;
				extrude();
				break;
			case Status.taper:
				taper();
				break;
		}
		mr.enabled = isHit;

		if (Input.GetMouseButtonDown(0)) {
			if (state == Status.extrude) {
				hitObj.GetComponent<MeshFilter>().mesh = extrudedMesh;
				hitObj.GetComponent<MeshCollider>().sharedMesh = extrudedMesh;
				state = Status.freemove;
				isHit = false;
				hitObj.GetComponent<ObjectController>().isMeshUpdated = true;
			}
		}

		if (touchPosition.magnitude < 10000 && !usePreviousTouch) {
			prevTouchPosition = touchPosition;
			touchTimer = touchDelayTolerance;
		}
		else {
			touchTimer -= Time.deltaTime;
		}
		if (touchTimer < 0 && isHit) {
			findEdge();
			select();
		}

	}
	/* #endregion */

	/* #region Construct highlight */
	private void castRay() {

		Vector3 mousePos = touchPosition;
		mousePos.x -= 360;
		mousePos.y -= 772;
		mousePos.z = 0;
		mousePos *= Camera.main.orthographicSize / 772;
		Debug.DrawLine(
			cam.gameObject.transform.position,
			cam.gameObject.transform.position + 10 * (mousePos - cam.gameObject.transform.position),
			Color.blue,
			0.01f,
			true
		);

		RaycastHit hit;
		if (!Physics.Raycast(cam.gameObject.transform.position, mousePos - cam.gameObject.transform.position, out hit)) {
			isHit = false;
		}
		else {
			MeshCollider meshCollider = hit.collider as MeshCollider;
			if (meshCollider != null && meshCollider.sharedMesh != null) {
				hitObj = meshCollider.gameObject;
				hitMesh = meshCollider.sharedMesh;
				selectedFaceIndex = hit.triangleIndex;
				selectedNormal = hit.normal;
				hitVertices = hitMesh.vertices;
				hitVerticesNum = hitVertices.Length;
				hituv = hitMesh.uv;
				hitTriangles = hitMesh.triangles;
				hitTrianglesNum = hitTriangles.Length;
				hitTransform = hit.collider.transform;
				hitRenderer = hitObj.GetComponent<Renderer>();
				selectedVertices[0] = hitVertices[hitTriangles[selectedFaceIndex * 3 + 0]];
				selectedVertices[1] = hitVertices[hitTriangles[selectedFaceIndex * 3 + 1]];
				selectedVertices[2] = hitVertices[hitTriangles[selectedFaceIndex * 3 + 2]];

				findPosition();
				findCoplanar();
				constructHighlight();
			}
			isHit = true;
		}
	}

	private void findPosition() {
		centerToHitFace =
			hitTransform.InverseTransformPoint(
					selectedNormal + hitObj.transform.position
				).normalized *
			dotProduct(
				hitTransform.InverseTransformPoint(
					selectedNormal + hitObj.transform.position
				).normalized,
				(state == Status.freemove ? hitVertices[hitTriangles[selectedFaceIndex * 3 + 0]] : extrudedVertices[hitTriangles[selectedFaceIndex * 3 + 0]])
			);
		posToFocus = new Vector3(0, 0, (hitTransform.TransformPoint(centerToHitFace) - hitObj.transform.position).magnitude);
	}

	private void findCoplanar() {
		int n = hitTrianglesNum / 3;
		Vector3 localNormal = crossProduct(selectedVertices[0] - selectedVertices[1], selectedVertices[0] - selectedVertices[2]);

		selected.Clear();
		bool[] isVisited = new bool[n];
		for (int i=0;i<n;i++) {
			isVisited[i] = false;
		}
		Queue<int> bs = new Queue<int>();
		bs.Enqueue(selectedFaceIndex);
		while (bs.Count > 0) {
			int idx = bs.Peek();
			isVisited[idx] = true;
			bs.Dequeue();
			Vector3 currentNormal = crossProduct(
				hitVertices[hitTriangles[idx * 3 + 0]] - hitVertices[hitTriangles[idx * 3 + 1]],
				hitVertices[hitTriangles[idx * 3 + 0]] - hitVertices[hitTriangles[idx * 3 + 2]]
			);
			if (localNormal.normalized == currentNormal.normalized) {
				selected.Add(idx);
				for (int i=0;i<n;i++) {
					if (!isVisited[i]) {
						int matchCount = 0;
						for (int v1=0;v1<3;v1++) {
							for (int v2=0;v2<3;v2++) {
								if (hitVertices[hitTriangles[idx * 3 + v1]] == hitVertices[hitTriangles[i * 3 + v2]]) {
									matchCount++;
								}
							}
						}
						if (matchCount >= 2) {
							bs.Enqueue(i);
						}
					}
				}
			}
		}
		faceNum = selected.Count;
		axisToFocus = crossProduct(selectedNormal, new Vector3(0, 0, -1));
		angleToFocus = Vector3.Angle(selectedNormal, new Vector3(0, 0, -1));
	}

	private void findEdge() {

		edgeVertices = new List<int>();

		int[] edgeLeft = new int[faceNum * 3];
		int[] edgeRight = new int[faceNum * 3];
		bool[] isDup = new bool[faceNum * 3];
		for (int i=0;i<faceNum * 3;i++) {
			isDup[i] = false;
		}

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				edgeLeft[i * 3 + j] = hitTriangles[selected[i] * 3 + j];
				edgeRight[i * 3 + j] = hitTriangles[selected[i] * 3 + ((j + 1) % 3)];
			}
		}

		for (int i=0;i<faceNum * 3;i++) {
			for (int j=i+1;j<faceNum * 3;j++) {
				if ( !isDup[i] && !isDup[j] &&
					((hitVertices[edgeLeft[i]] == hitVertices[edgeLeft[j]] && hitVertices[edgeRight[i]] == hitVertices[edgeRight[j]]) ||
					(hitVertices[edgeLeft[i]] == hitVertices[edgeRight[j]] && hitVertices[edgeRight[i]] == hitVertices[edgeLeft[j]]))
				) {
					isDup[i] = true;
					isDup[j] = true;
				}
			}
		}

		int startVertex = 0;
		for (int i=0;i<faceNum * 3;i++) {
			if (!isDup[i]) {
				startVertex = i;
				break;
			}
		}

		edgeVertices.Add(edgeLeft[startVertex]);
		edgeVertices.Add(edgeRight[startVertex]);
		isDup[startVertex] = true;
		bool done = false;
		while (!done) {
			done = true;
			for (int i=0;i<faceNum * 3;i++) {
				if (!isDup[i]) {
					if (hitVertices[edgeLeft[i]] == hitVertices[edgeVertices[edgeVertices.Count - 1]]) {
						edgeVertices.Add(edgeRight[i]);
						isDup[i] = true;
						done = false;
						break;
					}
					else if (hitVertices[edgeRight[i]] == hitVertices[edgeVertices[edgeVertices.Count - 1]]) {
						edgeVertices.Add(edgeLeft[i]);
						isDup[i] = true;
						done = false;
						break;
					}
				}
			}
		}
		edgeVertices.RemoveAt(edgeVertices.Count - 1);
		edgeLength = edgeVertices.Count;

	}

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		hitObj.transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * hitObj.transform.rotation;
		constructHighlight();
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			state = Status.select;
		}
		hitObj.transform.position = Vector3.Lerp(hitObj.transform.position, posToFocus, focusSpeed * Time.deltaTime);
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
	}

	private void constructHighlight() {
		highlight.Clear();

		if (smode == SelectMode.selectFace) {
			highlight.vertices = new Vector3[faceNum * 3];
			highlight.triangles = new int[faceNum * 3];
			Vector3[] vertexTemps = highlight.vertices;
			int[] triangleTemps = highlight.triangles;
			for (int i=0;i<faceNum * 3;i++) {
				triangleTemps[i] = i;
			}
			for (int i=0;i<faceNum;i++) {
				for (int j=0;j<3;j++) {
					switch (state) {
						case Status.extrude:
							vertexTemps[i * 3 + j] = extrudedVertices[extrudedTriangles[selected[i] * 3 + j]];
							break;
						case Status.taper:
							vertexTemps[i * 3 + j] = taperedVertices[taperedTriangles[selected[i] * 3 + j]];
							break;
						default:
							vertexTemps[i * 3 + j] = hitVertices[hitTriangles[selected[i] * 3 + j]];
							break;
					}
					vertexTemps[i * 3 + j] = hitTransform.TransformPoint(vertexTemps[i * 3 + j]) + 0.01f * selectedNormal.normalized;
				}
			}
			highlight.vertices = vertexTemps;
			highlight.triangles = triangleTemps;
			highlight.MarkModified();
			highlight.RecalculateNormals();
		}
		else {
			hitRenderer.material.SetColor("_Color", Color.yellow);
		}
	}
	/* #endregion */

	/* #region Extrude */
	private void extrude() {

		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;

		Vector3 localNormal = hitTransform.InverseTransformPoint(selectedNormal + hitObj.transform.position);
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i * 6 + 2] = hitVertices[edgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 4] = hitVertices[edgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 5] = hitVertices[edgeVertices[i]] + localNormal * extrudeDist;
		}

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				extrudedVertices[hitTriangles[selected[i] * 3 + j]] = hitVertices[hitTriangles[selected[i] * 3 + j]] + localNormal * extrudeDist;
			}
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;
		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		constructHighlight();
		findPosition();
		hitObj.transform.position = posToFocus;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
	}
	
	private void prepareExtrude() {
		
		extrudedMesh = hitObj.GetComponent<MeshFilter>().mesh;
		extrudedMesh.vertices = new Vector3[hitVerticesNum + edgeLength * 6];
		extrudedMesh.triangles = new int[hitTrianglesNum + edgeLength * 6];
		
		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		for (int i=0;i<hitVerticesNum;i++) {
			extrudedVertices[i] = hitVertices[i];
		}
		for (int i=0;i<hitTrianglesNum;i++) {
			extrudedTriangles[i] = hitTriangles[i];
		}
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i * 6 + 0] = hitVertices[edgeVertices[i]];
			extrudedVertices[hitVerticesNum + i * 6 + 1] = hitVertices[edgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 2] = hitVertices[edgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 3] = hitVertices[edgeVertices[i]];
			extrudedVertices[hitVerticesNum + i * 6 + 4] = hitVertices[edgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 5] = hitVertices[edgeVertices[i]];
		}
		for (int i=0;i<edgeLength;i++) {
			for (int j=0;j<6;j++) {
				extrudedTriangles[hitTrianglesNum + i * 6 + j] = hitVerticesNum + i * 6 + j;
			}
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;

		state = Status.extrude;
	}
	/* #endregion */

	/* #region Taper */
	private void taper() {
		
		taperedVertices = taperedMesh.vertices;
		taperedTriangles = taperedMesh.triangles;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperedVertices[hitTriangles[selected[i] * 3 + j]] = taperScale * (hitVertices[hitTriangles[selected[i] * 3 + j]] - taperCenter) + taperCenter;
			}
		}

		for (int i=0;i<taperedVertices.Length;i++) {
			for (int j=0;j<edgeLength;j++) {
				if (hitVertices[edgeVertices[j]] == hitVertices[i]) {
					taperedVertices[i] = taperScale * (hitVertices[edgeVertices[j]] - taperCenter) + taperCenter;
				}
			}
		}

		taperedMesh.vertices = taperedVertices;
		taperedMesh.triangles = taperedTriangles;
		taperedMesh.MarkModified();
		taperedMesh.RecalculateNormals();

		constructHighlight();

		if(taperStarted) {
			taperTimer -= Time.deltaTime;
		}
		if (taperTimer < 0) {
			hitObj.GetComponent<MeshFilter>().mesh = taperedMesh;
			hitObj.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
			state = Status.freemove;
			isHit = false;
			hitObj.GetComponent<ObjectController>().isMeshUpdated = true;
		}

	}

	private void prepareTaper() {

		taperScale = 1;
		taperCenter = new Vector3(0, 0, 0);

		taperedMesh = hitObj.GetComponent<MeshFilter>().mesh;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperCenter += hitVertices[hitTriangles[selected[i] * 3 + j]];
			}
		}
		taperCenter /= faceNum * 3;

		state = Status.taper;

		taperStarted = false;
		taperTimer = touchDelayTolerance;
	}
	/* #endregion */

	/* #region Split */
	private void slice(Vector3 planePos, Vector3 planeNormal) {
		// x ⋅ planePos - dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planePos = hitTransform.InverseTransformPoint(planePos);
		planeNormal = hitTransform.InverseTransformPoint(planeNormal);
		Vector3 avoidZeroVector = new Vector3(Random.Range(1f, 5f), Random.Range(1f, 5f), Random.Range(1f, 5f));
		if (planeNormal.x != 0) {
			avoidZeroVector.x = - (avoidZeroVector.y * planeNormal.y + avoidZeroVector.z * planeNormal.z) / planeNormal.x;
		}
		else if (planeNormal.y != 0) {
			avoidZeroVector.y = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.z * planeNormal.z) / planeNormal.y;
		}
		else if (planeNormal.z != 0) {
			avoidZeroVector.z = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.y * planeNormal.y) / planeNormal.z;
		}
		planePos += avoidZeroVector;

		List<Vector3> leftVerticesList = new List<Vector3>();
		List<Vector3> rightVerticesList = new List<Vector3>();
		List<Vector3> edgeVerticesList = new List<Vector3>();

		//Calculate edge of cutting plane & reconstruct faces along the edge
		int[] triangleSide = new int[hitTrianglesNum / 3]; // -1 left, 0 cross, 1 right
		float pn = dotProduct(planePos, planeNormal);
		for (int i=0;i<hitTrianglesNum / 3;i++) {
			float[] verticesPos = new float[3]{
				dotProduct(hitVertices[hitTriangles[i * 3 + 0]], planeNormal) - pn,
				dotProduct(hitVertices[hitTriangles[i * 3 + 1]], planeNormal) - pn,
				dotProduct(hitVertices[hitTriangles[i * 3 + 2]], planeNormal) - pn
			};
			if (verticesPos[0] <= 0 && verticesPos[1] <= 0 && verticesPos[2] <= 0) {
				triangleSide[i] = -1;
				for (int j=0;j<3;j++) {
					leftVerticesList.Add(hitVertices[hitTriangles[i * 3 + j]]);
				}
			}
			else if (verticesPos[0] > 0 && verticesPos[1] > 0 && verticesPos[2] > 0) {
				triangleSide[i] = 1;
				for (int j=0;j<3;j++) {
					rightVerticesList.Add(hitVertices[hitTriangles[i * 3 + j]]);
				}
			}
			else {
				triangleSide[i] = 0;
				for (int j=0;j<3;j++) {
					Vector3[] curVec = new Vector3[3]{
						hitVertices[hitTriangles[i * 3 + j]],
						hitVertices[hitTriangles[i * 3 + ((j+1)%3)]],
						hitVertices[hitTriangles[i * 3 + ((j+2)%3)]]
					};
					if (verticesPos[j] <= 0 && verticesPos[(j+1)%3] <= 0) {
						Vector3 newVec1 = intersectLinePlane(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(curVec[1], curVec[2], planePos, planeNormal);
						leftVerticesList.Add(curVec[0]);
						leftVerticesList.Add(curVec[1]);
						leftVerticesList.Add(newVec1);
						leftVerticesList.Add(curVec[1]);
						leftVerticesList.Add(newVec2);
						leftVerticesList.Add(newVec1);
						rightVerticesList.Add(curVec[2]);
						rightVerticesList.Add(newVec1);
						rightVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
					else if (verticesPos[j] > 0 && verticesPos[(j+1)%3] > 0) {
						Vector3 newVec1 = intersectLinePlane(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(curVec[1], curVec[2], planePos, planeNormal);
						rightVerticesList.Add(curVec[0]);
						rightVerticesList.Add(curVec[1]);
						rightVerticesList.Add(newVec1);
						rightVerticesList.Add(curVec[1]);
						rightVerticesList.Add(newVec2);
						rightVerticesList.Add(newVec1);
						leftVerticesList.Add(curVec[2]);
						leftVerticesList.Add(newVec1);
						leftVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
				}
			}
		}

		//Extract edge as successive vertices
		List<Vector3> sortedEdgeVerticesList = new List<Vector3>();
		int curEdge = 0;
		bool done = false;
		sortedEdgeVerticesList.Add(edgeVerticesList[0]);
		sortedEdgeVerticesList.Add(edgeVerticesList[1]);
		while (!done) {
			for (int i=0;i<edgeVerticesList.Count / 2;i++) {
				for (int j=0;j<2;j++) {
					if (i != curEdge && edgeVerticesList[i * 2 + j] == sortedEdgeVerticesList[sortedEdgeVerticesList.Count - 1]) {
						if (edgeVerticesList[i * 2 + (1 - j)] == sortedEdgeVerticesList[0]) {
							done = true;
						}
						else {
							sortedEdgeVerticesList.Add(edgeVerticesList[i * 2 + (1 - j)]);
							curEdge = i;
						}
					}
				}
			}
		}

		//Simplify edge
		while (true) {
			int prevCount = sortedEdgeVerticesList.Count;
			for (int i=0;i<sortedEdgeVerticesList.Count;i++) {
				int prev = (i + sortedEdgeVerticesList.Count - 1) % sortedEdgeVerticesList.Count;
				int next = (i + 1) % sortedEdgeVerticesList.Count;
				if (crossProduct(sortedEdgeVerticesList[next] - sortedEdgeVerticesList[i], sortedEdgeVerticesList[i] - sortedEdgeVerticesList[prev]).magnitude < 0.001f) {
					sortedEdgeVerticesList.RemoveAt(i);
					break;
				}
			}
			if (sortedEdgeVerticesList.Count == prevCount) {
				break;
			}
		}

		//Construct cutting plane
		List<Vector3> cuttingPlaneVerticesList = new List<Vector3>();
		while (sortedEdgeVerticesList.Count > 3) {
			int prevCount = sortedEdgeVerticesList.Count;
			int loopCount = 0;
			int i = Random.Range(0, sortedEdgeVerticesList.Count);
			while (loopCount < prevCount) {
				int prev1 = (i + sortedEdgeVerticesList.Count - 1) % sortedEdgeVerticesList.Count;
				int prev2 = (i + sortedEdgeVerticesList.Count - 2) % sortedEdgeVerticesList.Count;
				int next1 = (i + 1) % sortedEdgeVerticesList.Count;
				int next2 = (i + 2) % sortedEdgeVerticesList.Count;
				bool isIntersect = false;
				if (Vector3.Angle(
						sortedEdgeVerticesList[next1] - sortedEdgeVerticesList[i],
						sortedEdgeVerticesList[next2] - sortedEdgeVerticesList[next1]
					) <
					Vector3.Angle(
						sortedEdgeVerticesList[next1] - sortedEdgeVerticesList[i],
						sortedEdgeVerticesList[prev1] - sortedEdgeVerticesList[next1]
					)
				) {
					for (int j=0;j<sortedEdgeVerticesList.Count;j++) {
						if (j != i && j != prev1 && j != prev2 && j != next1 &&
							areLinesIntersect(
								sortedEdgeVerticesList[prev1],
								sortedEdgeVerticesList[next1],
								sortedEdgeVerticesList[j],
								sortedEdgeVerticesList[(j+1)%sortedEdgeVerticesList.Count]
							)
						) {
							isIntersect = true;
							break;
						}
					}
					if (!isIntersect) {
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[prev1]);
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[i]);
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[next1]);
						sortedEdgeVerticesList.RemoveAt(i);
					}
				}
				if (!isIntersect) {
					break;
				}
				i++;
				loopCount++;
				if (i == sortedEdgeVerticesList.Count) {
					i = 0;
				}
			}
			if (prevCount == sortedEdgeVerticesList.Count) {
				Debug.Log(prevCount);
				break;
			}
		}
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[0]);
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[1]);
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[2]);

		//Verify direction
		for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
			Vector3 normalFace = crossProduct(
				cuttingPlaneVerticesList[i * 3 + 1] - cuttingPlaneVerticesList[i * 3 + 0],
				cuttingPlaneVerticesList[i * 3 + 2] - cuttingPlaneVerticesList[i * 3 + 1]
			);
			if (
				(normalFace.x != 0 && planeNormal.x != 0 && planeNormal.x / normalFace.x < 0) ||
				(normalFace.y != 0 && planeNormal.y != 0 && planeNormal.y / normalFace.y < 0) ||
				(normalFace.z != 0 && planeNormal.z != 0 && planeNormal.z / normalFace.z < 0)
			) {
				Vector3 tempVertex = cuttingPlaneVerticesList[i * 3 + 1];
				cuttingPlaneVerticesList[i * 3 + 1] = new Vector3(cuttingPlaneVerticesList[i * 3 + 0].x, cuttingPlaneVerticesList[i * 3 + 0].y, cuttingPlaneVerticesList[i * 3 + 0].z);
				cuttingPlaneVerticesList[i * 3 + 0] = tempVertex;
			}
		}

		GameObject leftObj = hitObj;
		GameObject rightObj = Instantiate(leftObj, leftObj.transform);
		rightObj.transform.parent = null;
		rightObj.transform.position = leftObj.transform.position;
		rightObj.transform.rotation = leftObj.transform.rotation;
		rightObj.transform.localScale = leftObj.transform.localScale;
		Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;
		Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
		leftMesh.Clear();
		rightMesh.Clear();

		leftMesh.vertices = new Vector3[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		leftMesh.triangles = new int[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		Vector3[] leftVertices = leftMesh.vertices;
		int[] leftTriangles = leftMesh.triangles;
		rightMesh.vertices = new Vector3[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
		rightMesh.triangles = new int[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
		Vector3[] rightVertices = rightMesh.vertices;
		int[] rightTriangles = rightMesh.triangles;

		for (int i=0;i<leftVerticesList.Count;i++) {
			leftVertices[i] = leftVerticesList[i];
			leftTriangles[i] = i;
		}
		for (int i=0;i<rightVerticesList.Count;i++) {
			rightVertices[i] = rightVerticesList[i];
			rightTriangles[i] = i;
		}
		for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
			for (int j=0;j<3;j++) {
				leftVertices[i * 3 + j + leftVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
				leftTriangles[i * 3 + j + leftVerticesList.Count] = i * 3 + j + leftVerticesList.Count;
				rightVertices[i * 3 + j + rightVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
				rightTriangles[i * 3 + j + rightVerticesList.Count] = i * 3 + (2 - j) + rightVerticesList.Count;
			}
		}

		leftMesh.vertices = leftVertices;
		leftMesh.triangles = leftTriangles;
		leftMesh.MarkModified();
		leftMesh.RecalculateNormals();
		leftObj.GetComponent<MeshFilter>().mesh = leftMesh;
		leftObj.GetComponent<MeshCollider>().sharedMesh = leftMesh;
		rightMesh.vertices = rightVertices;
		rightMesh.triangles = rightTriangles;
		rightMesh.MarkModified();
		rightMesh.RecalculateNormals();
		rightObj.GetComponent<MeshFilter>().mesh = rightMesh;
		rightObj.GetComponent<MeshCollider>().sharedMesh = rightMesh;

		leftObj.transform.position = leftObj.transform.position + selectedNormal.normalized * 0.25f;
		rightObj.transform.position = rightObj.transform.position - selectedNormal.normalized * 0.25f;

		leftObj.GetComponent<ObjectController>().isTransformUpdated = true;
		leftObj.GetComponent<ObjectController>().isMeshUpdated = true;
		rightObj.GetComponent<ObjectController>().isTransformUpdated = true;
		rightObj.GetComponent<ObjectController>().isMeshUpdated = true;
		rightObj.GetComponent<ObjectController>().index = objectManager.GetComponent<ObjectManager>().getNum();

		state = Status.freemove;
		isHit = false;
	}
	/* #endregion */

	/* #region Calculator */
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private Vector3 intersectLinePlane(Vector3 a, Vector3 b, Vector3 p, Vector3 n) { //line passes a and b, plane passes p with normal n
		float t = (dotProduct(p, n) - dotProduct(a, n)) / dotProduct(n, a - b);
		return a + t * (a - b);
	}

	private bool areLinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2) {
		bool result = true;
		if (crossProduct(a2 - a1, b2 - a2).magnitude > 0 &&
			crossProduct(a2 - a1, b1 - a2).magnitude > 0 &&
			Vector3.Angle(crossProduct(a2 - a1, b2 - a2), crossProduct(a2 - a1, b1 - a2)) < 0.001f
		) {
			result = false;
		}
		if (crossProduct(b2 - b1, a1 - b2).magnitude > 0 &&
			crossProduct(b2 - b1, a2 - b2).magnitude > 0 &&
			Vector3.Angle(crossProduct(b2 - b1, a1 - b2), crossProduct(b2 - b1, a2 - b2)) < 0.001f
		) {
			result = false;
		}
		return result;
	}
	/* #endregion */

	/* #region Public */
	private void select() {
		if (isHit && state == Status.freemove) {
			state = Status.select;
		}
	}
	public void startFocus() {
		if (state == Status.select && smode == SelectMode.selectFace) {
			state = Status.focus;
		}
	}

	public void startExtrude() {
		if (state == Status.select && smode == SelectMode.selectFace) {
			state = Status.extrude;
			prepareExtrude();
		}
	}

	public void startTaper() {
		if (state == Status.select && smode == SelectMode.selectFace) {
			prepareTaper();
		}
	}

	public void startSlice() {
		GameObject slicePlane = GameObject.Find("SlicePlane");
		if (state == Status.select && smode == SelectMode.selectObject) {
			slice(slicePlane.transform.position, slicePlane.transform.rotation * new Vector3(0, 1, 0));
		}
	}

	public void switchSelectMode() {
		state = Status.freemove;
		if (smode == SelectMode.selectFace) {
			smode = SelectMode.selectObject;
		}
		else {
			smode = SelectMode.selectFace;
		}
	}

	public void pan(Vector3 delta) {
		if (isHit && smode == SelectMode.selectObject) {
			hitObj.transform.position += delta;
			hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
			panVisualizer.GetComponent<PanVisualizer>().pan(hitObj.transform.position);
		}
	}

	public void pinch(float delta) {
		if (isHit) {
			if (smode == SelectMode.selectObject) {
				hitObj.transform.localScale += new Vector3(delta, delta, delta);
				hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
			}
			else if (state == Status.taper) {
				if (delta > 0){
					taperStarted = true;
				}
				taperScale += delta / 2;
				taperTimer = touchDelayTolerance;
			}
		}
	}

	public void turn(float angle, bool isMainScreen) {
		debugText.text = "" + angle;
		if (isHit && smode == SelectMode.selectObject) {
			Quaternion rot = Quaternion.AngleAxis(angle, (isMainScreen ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0)));
			hitObj.transform.rotation = rot * hitObj.transform.rotation;
			hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
		}
	}

	public void cancel() {
		state = Status.freemove;
		isHit = false;
	}
	/* #endregion */
}