using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class MeshManipulator : MonoBehaviour
{
	Camera cam;

	private GameObject hitObj;
	private Mesh hitMesh;
	private Vector3[] hitVertices;
	private Vector2[] hituv;
	private int[] hitTriangles;
	private Transform hitTransform;
	private int hitVerticesNum;
	private int hitTrianglesNum;
	private Renderer hitRenderer;

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

	private Status state = Status.freemove;
	private enum Status {
		freemove,
		select,
		focus,
		extrude,
		taper
	}

	private SelectMode smode = SelectMode.selectObject;
	private enum SelectMode {
		selectFace,
		selectObject
	}

	void Start()
	{
		cam = Camera.main;

		highlight = new Mesh();
		GetComponent<MeshFilter>().mesh = highlight;
		mr = GetComponent<MeshRenderer>();

		highlight.vertices = new Vector3[3];
		highlight.uv = new Vector2[3];
		highlight.triangles = new int[]{0, 1, 2};
	}

	void Update() {

		if (state == Status.freemove && smode == SelectMode.selectObject) {
			GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Object");
			foreach (GameObject obj in allObjects) {
				Renderer tempRenderer = obj.GetComponent<Renderer>();
				tempRenderer.material.SetColor("_Color", Color.white);
			}
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
				taperScale = (Input.mousePosition.y - 80) / 500 + 0.5f;
				taper();
				break;
		}
		mr.enabled = isHit;

		if (Input.GetMouseButtonDown(0)) {
			if (isHit && state == Status.freemove) {
				findEdge();
				state = Status.select;
			}
			else if (Input.mousePosition.y > 300 && state == Status.select) {
				state = Status.freemove;
			}
			else if (state == Status.extrude) {
				hitObj.GetComponent<MeshFilter>().mesh = extrudedMesh;
				hitObj.GetComponent<MeshCollider>().sharedMesh = extrudedMesh;
				state = Status.freemove;
			}
			else if (state == Status.taper) {
				hitObj.GetComponent<MeshFilter>().mesh = taperedMesh;
				hitObj.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
				state = Status.freemove;
			}
		}
	}

	private void castRay() {
		RaycastHit hit;
		if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit)) {
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
				findCoplanar();
				constructHighlight();
			}
			isHit = true;
		}
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
					vertexTemps[i * 3 + j] = hitTransform.TransformPoint(vertexTemps[i * 3 + j]) + 0.001f * hitTransform.InverseTransformPoint(selectedNormal.normalized);
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

	private void extrude() {

		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;

		Vector3 localNormal = hitTransform.InverseTransformPoint(selectedNormal);
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
	}

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

		Debug.Log(planePos + " " + planeNormal);

		List<Vector3> leftVerticesList = new List<Vector3>();
		List<Vector3> rightVerticesList = new List<Vector3>();
		List<int> leftTrianglesList = new List<int>();
		List<int> rightTrianglesList = new List<int>();
		List<int> edgeVerticesList = new List<int>();

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
					leftTrianglesList.Add(leftTrianglesList.Count);
				}
			}
			else if (verticesPos[0] > 0 && verticesPos[1] > 0 && verticesPos[2] > 0) {
				triangleSide[i] = 1;
				for (int j=0;j<3;j++) {
					rightVerticesList.Add(hitVertices[hitTriangles[i * 3 + j]]);
					rightTrianglesList.Add(rightTrianglesList.Count);
				}
			}
			else {
				triangleSide[i] = 0;
				for (int j=0;j<3;j++) {
					if (verticesPos[j] <= 0 && verticesPos[(j+1)%3] <= 0) {
						Vector3 newVec1 = intersectLinePlane(hitVertices[hitTriangles[i * 3 + j]], hitVertices[hitTriangles[i * 3 + ((j+2)%3)]], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(hitVertices[hitTriangles[i * 3 + ((j+1)%3)]], hitVertices[hitTriangles[i * 3 + ((j+2)%3)]], planePos, planeNormal);
						Debug.DrawLine(
							hitTransform.TransformPoint(newVec1),
							hitTransform.TransformPoint(newVec2),
							Color.white,
							1000,
							false
						);
					}
					else if (verticesPos[j] > 0 && verticesPos[(j+1)%3] > 0) {
						Vector3 newVec1 = intersectLinePlane(hitVertices[hitTriangles[i * 3 + j]], hitVertices[hitTriangles[i * 3 + ((j+2)%3)]], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(hitVertices[hitTriangles[i * 3 + ((j+1)%3)]], hitVertices[hitTriangles[i * 3 + ((j+2)%3)]], planePos, planeNormal);
						Debug.DrawLine(
							hitTransform.TransformPoint(newVec1),
							hitTransform.TransformPoint(newVec2),
							Color.white,
							1000,
							false
						);
					}
				}
			}
		}
		Debug.Log("Left: " + leftVerticesList.Count + " Right: " + rightVerticesList.Count);

		// GameObject leftObj = hitObj;
		// GameObject rightObj = Instantiate(leftObj, leftObj.transform);
		// Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;
		// Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;




		state = Status.freemove;
	}

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
			state = Status.taper;
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
		if (smode == SelectMode.selectFace) {
			smode = SelectMode.selectObject;
		}
		else {
			smode = SelectMode.selectFace;
		}
	}

	public void cancel() {
		state = Status.freemove;
	}
}