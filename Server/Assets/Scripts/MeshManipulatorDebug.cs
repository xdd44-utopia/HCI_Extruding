using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MeshManipulatorDebug : MonoBehaviour
{
	private float camWidth;
	private float camHeight;
	public Text debugText;

	//hit
	private GameObject hitObj;
	public Mesh defaultMesh;
	private RaycastHit hit;
	private Vector3[] hitVertices;
	private int[] hitTriangles;
	private int hitVerticesNum;
	private int hitTrianglesNum;

	//selection
	[HideInInspector]
	public List<int> selectEdgeVertices;
	private List<int> prevEdgeVertices;
	[HideInInspector]
	public List<int> selectTriangles;
	private int selectTriangleIndex;
	[HideInInspector]
	public int focusTriangleIndex;
	[HideInInspector]
	public Vector3 focusNormal;
	
	//focus
	private Vector3 axisToFocus;
	private float angleToFocus;
	private Vector3 posToFocus;
	private const float focusSpeed = 25;

	//slice
	private List<Vector3> leftVerticesList;
	private List<Vector3> rightVerticesList;
	private List<Vector3> edgeVerticesList;
	private List<Vector3> cuttingPlaneVerticesList;
	private Vector3 planeNormalWorld;
	private bool isThisScreenCuttingPlane = false;

	private float angle;

	private Status state = Status.select;
	private enum Status {
		select,
		focus,
		extrude,
		taper,
		drill
	}

	/* #region Main */
	void Start()
	{

		hitObj = GameObject.Find("OBJECT");

		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
	
	}

	void Update() {

		angle = - Mathf.PI / 3;
		if (state == Status.select) {
			hitVertices = hitObj.GetComponent<MeshFilter>().mesh.vertices;
			hitTriangles = hitObj.GetComponent<MeshFilter>().mesh.triangles;
			hitVerticesNum = hitVertices.Length;
			hitTrianglesNum = hitTriangles.Length;
		}
	}
	/* #endregion */

	/* #region Split */
	public void enableCuttingPlaneOtherScreen() {
		// isOtherScreenPenetrate = true;
		// isEdgeAligned = false;
	}
	public void executeCuttingPlaneOtherScreen() {
		startSlice(true, false);
		executeSlice();
		// isOtherScreenPenetrate = false;
	}
	private void enableCuttingPlaneThisScreen() {
		isThisScreenCuttingPlane = true;
		// isEdgeAligned = false;
	}
	private void executeCuttingPlaneThisScreen() {
		startSlice(true, true);
		executeSlice();
		isThisScreenCuttingPlane = false;
	}
	public void cuttingPlaneSwitcher() {
		if (!isThisScreenCuttingPlane) {
			enableCuttingPlaneThisScreen();
		}
		else {
			executeCuttingPlaneThisScreen();
		}
	}
	public void startSlice(bool isScreenCut, bool isMainScreen) {
		GameObject slicePlane = GameObject.Find("SlicePlane");
		if (state == Status.select) {
			// if (isScreenCut && isMainScreen) {
			// 	prepareSlice(new Vector3(0, 0, 0.025f), new Vector3(0, 0, -1), isScreenCut);
			// }
			// else {
				Vector3 normalTemp = slicePlane.transform.rotation * new Vector3(0, 1, 0);
				prepareSlice(slicePlane.transform.position, normalTemp, isScreenCut);
			// }
		}
	}
	private void prepareSlice(Vector3 planePos, Vector3 planeNormal, bool isScreenCut) {

		// prepareUndo();

		// x ⋅ planePos - dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planeNormalWorld = new Vector3(planeNormal.x, planeNormal.y, planeNormal.z);
		planePos = hitObj.transform.InverseTransformPoint(planePos);
		planeNormal = hitObj.transform.InverseTransformPoint(planeNormal + hitObj.transform.position);
		Vector3 avoidZeroVector = new Vector3(UnityEngine.Random.Range(0.01f, 0.02f), UnityEngine.Random.Range(0.01f, 0.02f), UnityEngine.Random.Range(0.01f, 0.02f));
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

		leftVerticesList = new List<Vector3>();
		rightVerticesList = new List<Vector3>();
		edgeVerticesList = new List<Vector3>();

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

		if (edgeVerticesList.Count == 0) {
			return;
		}

		int whileLoopLimit = 10000;
		int whileLoopCount = 0;

		//Extract edge as successive vertices
		List<Vector3> sortedEdgeVerticesList = new List<Vector3>();
		int curEdge = 0;
		Vector3 curVect = edgeVerticesList[1];
		bool done = false;
		sortedEdgeVerticesList.Clear();

		for (int i=0;i<edgeVerticesList.Count / 2;i++) {
			for (int j=0;j<edgeVerticesList.Count / 2;j++) {
				if (curEdge != j) {
					if (edgeVerticesList[j * 2] == curVect) {
						sortedEdgeVerticesList.Add(edgeVerticesList[j * 2]);
						curVect = edgeVerticesList[j * 2 + 1];
						curEdge = j;
						break;
					}
					else if (edgeVerticesList[j * 2 + 1] == curVect) {
						sortedEdgeVerticesList.Add(edgeVerticesList[j * 2 + 1]);
						curVect = edgeVerticesList[j * 2];
						curEdge = j;
						break;
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
			if (whileLoopCount > whileLoopLimit) {
				debugText.text = "2nd while loop stucked\n";
				whileLoopCount = 0;
				break;
			}
		}

		Vector3[] sortedEdgeVertices = new Vector3[sortedEdgeVerticesList.Count];
		for (int i=0;i<sortedEdgeVerticesList.Count;i++){
			sortedEdgeVertices[i] = hitObj.transform.TransformPoint(sortedEdgeVerticesList[i]);
		}
		
		//Construct cutting plane
		cuttingPlaneVerticesList = new List<Vector3>();
		while (sortedEdgeVerticesList.Count > 3) {
			int prevCount = sortedEdgeVerticesList.Count;
			int loopCount = 0;
			int i = UnityEngine.Random.Range(0, sortedEdgeVerticesList.Count);
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
			if (whileLoopCount > whileLoopLimit) {
				debugText.text = "3rd while loop stucked\n";
				whileLoopCount = 0;
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
	}
	
	public void executeSlice(){

		if (edgeVerticesList.Count == 0) {
			return;
		}

		bool isTrim = true;

		GameObject leftObj = hitObj;
		Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;
		leftMesh.Clear();
		leftMesh.vertices = new Vector3[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		leftMesh.triangles = new int[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		Vector3[] leftVertices = leftMesh.vertices;
		int[] leftTriangles = leftMesh.triangles;
		for (int i=0;i<leftVerticesList.Count;i++) {
			leftVertices[i] = leftVerticesList[i];
			leftTriangles[i] = i;
		}

		for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
			for (int j=0;j<3;j++) {
				leftVertices[i * 3 + j + leftVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
				leftTriangles[i * 3 + j + leftVerticesList.Count] = i * 3 + j + leftVerticesList.Count;
			}
		}

		//relocate mesh center
		Vector3 meshCenter = new Vector3(0, 0, 0);
		for (int i=0;i<leftVertices.Length;i++) {
			meshCenter += leftVertices[i];
		}
		meshCenter /= leftVertices.Length;
		for (int i=0;i<leftVertices.Length;i++) {
			leftVertices[i] -= meshCenter;
		}
		meshCenter = leftObj.transform.TransformPoint(meshCenter);
		leftObj.transform.position = meshCenter;

		//update mesh

		leftMesh.vertices = leftVertices;
		leftMesh.triangles = leftTriangles;
		leftMesh.MarkModified();
		leftMesh.RecalculateNormals();
		leftObj.GetComponent<MeshFilter>().mesh = leftMesh;
		leftObj.GetComponent<MeshCollider>().sharedMesh = leftMesh;

		leftObj.transform.position = leftObj.transform.position - (isTrim ? Vector3.zero : planeNormalWorld.normalized * 0.25f);
		leftObj.GetComponent<ObjectController>().isTransformUpdated = true;
		leftObj.GetComponent<ObjectController>().isMeshUpdated = true;

		if (!isTrim) {
			GameObject rightObj = Instantiate(leftObj, leftObj.transform);
			rightObj.transform.parent = null;
			rightObj.transform.position = leftObj.transform.position;
			rightObj.transform.rotation = leftObj.transform.rotation;
			rightObj.transform.localScale = leftObj.transform.localScale;
			Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
			rightMesh.Clear();
			rightMesh.vertices = new Vector3[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
			rightMesh.triangles = new int[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
			Vector3[] rightVertices = rightMesh.vertices;
			int[] rightTriangles = rightMesh.triangles;
			for (int i=0;i<rightVerticesList.Count;i++) {
				rightVertices[i] = rightVerticesList[i];
				rightTriangles[i] = i;
			}
			for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
				for (int j=0;j<3;j++) {
					rightVertices[i * 3 + j + rightVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
					rightTriangles[i * 3 + j + rightVerticesList.Count] = i * 3 + (2 - j) + rightVerticesList.Count;
				}
			}
			rightMesh.vertices = rightVertices;
			rightMesh.triangles = rightTriangles;
			rightMesh.MarkModified();
			rightMesh.RecalculateNormals();
			rightObj.GetComponent<MeshFilter>().mesh = rightMesh;
			rightObj.GetComponent<MeshCollider>().sharedMesh = rightMesh;
			rightObj.transform.position = rightObj.transform.position + planeNormalWorld.normalized * 0.25f;
			rightObj.GetComponent<ObjectController>().isTransformUpdated = true;
			rightObj.GetComponent<ObjectController>().isMeshUpdated = true;
		}

	}
	/* #endregion */
	
	/* #region Calculator */
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
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

	public void restart() {
		cancel();

		hitObj.SetActive(true);

		hitObj.transform.position = new Vector3(0, 0, 2f);
		hitObj.transform.localScale = new Vector3(2, 2, 2);
		hitObj.transform.rotation = Quaternion.Euler(30, 60, 45);

		hitObj.GetComponent<MeshFilter>().mesh = defaultMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = defaultMesh;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
		hitObj.GetComponent<ObjectController>().isMeshUpdated = true;
	}

	public void cancel() {
		state = Status.select;
		hitObj.GetComponent<ObjectController>().cleanHighlight();
	}
	/* #endregion */
}