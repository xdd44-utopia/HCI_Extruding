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

	private Mesh highlight;
	private MeshRenderer mr;

	private bool isHit = false;

	//selection
	private List<int> selected = new List<int>();
	private int selectedFaceIndex;
	private int faceNum;
	private Vector3 selectedNormal;
	private Vector3[] selectedVertices = new Vector3[3];
	
	//focus
	private Vector3 axisToFocus;
	private float angleToFocus;
	private const float focusSpeed = 25;

	//extrude
	private Mesh extrudedMesh;
	private List<int> edgeVertices;
	private int edgeLength;
	private List<int> left;
	private List<int> right;
	private float extrudeDist = 0.2f;
	private Vector3[] extrudedVertices;
	private Vector2[] extrudeduv;
	private int[] extrudedTriangles;

	private Status state = Status.freemove;
	private enum Status {
		freemove,
		freeze,
		focus,
		extrude
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
		switch(state) {
			case Status.freemove:
				castRay();
				break;
			case Status.focus:
				focus();
				break;
			case Status.extrude:
				extrudeDist = (Input.mousePosition.y - 100) / 50;
				extrude();
				break;
		}
		mr.enabled = isHit;

		if (Input.GetMouseButtonDown(0)) {
			if (isHit && state == Status.freemove) {
				state = Status.freeze;
			}
			else if (Input.mousePosition.y > 300 && state == Status.freeze) {
				state = Status.freemove;
			}
			else if (state == Status.extrude) {
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

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		hitObj.transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * hitObj.transform.rotation;
		constructHighlight();
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			state = Status.freeze;
		}
	}

	private void constructHighlight() {
		highlight.Clear();
		highlight.vertices = new Vector3[faceNum * 3];
		highlight.triangles = new int[faceNum * 3];
		highlight.uv = new Vector2[faceNum * 3];
		Vector3[] vertexTemps = highlight.vertices;
		int[] triangleTemps = highlight.triangles;
		for (int i=0;i<faceNum * 3;i++) {
			triangleTemps[i] = i;
		}
		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				vertexTemps[i * 3 + j] = (state == Status.extrude ? extrudedVertices[extrudedTriangles[selected[i] * 3 + j]] : hitVertices[hitTriangles[selected[i] * 3 + j]]);
				vertexTemps[i * 3 + j] = hitTransform.TransformPoint(vertexTemps[i * 3 + j]) * 1.0001f;
			}
		}
		highlight.vertices = vertexTemps;
		highlight.triangles = triangleTemps;
		
	}

	private void extrude() {

		extrudedVertices = extrudedMesh.vertices;
		extrudeduv = extrudedMesh.uv;
		extrudedTriangles = extrudedMesh.triangles;

		Vector3 localNormal = hitTransform.InverseTransformPoint(selectedNormal);
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i * 6 + 2] = hitVertices[edgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 4] = hitVertices[edgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 5] = hitVertices[edgeVertices[i]] + localNormal * extrudeDist;
			for (int j=0;j<3;j++) {
				Debug.DrawLine(
					hitTransform.TransformPoint(extrudedVertices[hitVerticesNum + i * 6 + j]),
					hitTransform.TransformPoint(extrudedVertices[hitVerticesNum + i * 6 + ((j + 1) % 3)]),
					Color.blue,
					0.01f,
					false
				);
				Debug.DrawLine(
					hitTransform.TransformPoint(extrudedVertices[hitVerticesNum + i * 6 + j + 3]),
					hitTransform.TransformPoint(extrudedVertices[hitVerticesNum + i * 6 + ((j + 1) % 3) + 3]),
					Color.blue,
					0.01f,
					false
				);
			}
		}

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				extrudedVertices[hitTriangles[selected[i] * 3 + j]] = hitVertices[hitTriangles[selected[i] * 3 + j]] + localNormal * extrudeDist;
			}
			for (int j=0;j<3;j++) {
				Debug.DrawLine(
					hitTransform.TransformPoint(extrudedVertices[hitTriangles[selected[i] * 3 + j]]),
					hitTransform.TransformPoint(extrudedVertices[hitTriangles[selected[i] * 3 + ((j + 1) % 3)]]),
					Color.yellow,
					0.01f,
					false
				);
			}
		}

		for (int i=0;i<extrudedVertices.Length;i++) {
			extrudeduv[i] = new Vector2(extrudedVertices[i].x, extrudedVertices[i].z);
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.uv = extrudeduv;
		extrudedMesh.uv2 = new Vector2[extrudeduv.Length];
		extrudedMesh.triangles = extrudedTriangles;
		//Unwrapping.GenerateSecondaryUVSet(extrudedMesh);
		hitObj.GetComponent<MeshFilter>().mesh = extrudedMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = hitObj.GetComponent<MeshFilter>().mesh;
		hitObj.GetComponent<MeshFilter>().mesh.MarkModified();
		//hitObj.GetComponent<MeshFilter>().mesh.UploadMeshData(false);

		constructHighlight();
	}
	
	private void prepareExtrude() {

		edgeVertices = new List<int>();
		left = new List<int>();
		right = new List<int>();

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
		
		extrudedMesh = hitObj.GetComponent<MeshFilter>().mesh;
		hitObj.GetComponent<MeshFilter>().mesh.MarkModified();
		extrudedMesh.vertices = new Vector3[hitVerticesNum + edgeLength * 6];
		extrudedMesh.uv = new Vector2[hitVerticesNum + edgeLength * 6];
		extrudedMesh.triangles = new int[hitTrianglesNum + edgeLength * 6];
		
		extrudedVertices = extrudedMesh.vertices;
		extrudeduv = extrudedMesh.uv;
		extrudedTriangles = extrudedMesh.triangles;
		for (int i=0;i<hitVerticesNum;i++) {
			extrudedVertices[i] = hitVertices[i];
		}
		for (int i=0;i<hitVerticesNum;i++) {
			extrudeduv[i] = hituv[i];
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
			extrudeduv[hitVerticesNum + i * 6 + 0] = hituv[edgeVertices[i]];
			extrudeduv[hitVerticesNum + i * 6 + 1] = hituv[edgeVertices[(i+1)%edgeLength]];
			extrudeduv[hitVerticesNum + i * 6 + 2] = hituv[edgeVertices[(i+1)%edgeLength]];
			extrudeduv[hitVerticesNum + i * 6 + 3] = hituv[edgeVertices[i]];
			extrudeduv[hitVerticesNum + i * 6 + 4] = hituv[edgeVertices[(i+1)%edgeLength]];
			extrudeduv[hitVerticesNum + i * 6 + 5] = hituv[edgeVertices[i]];
		}
		for (int i=0;i<edgeLength;i++) {
			for (int j=0;j<6;j++) {
				extrudedTriangles[hitTrianglesNum + i * 6 + j] = hitVerticesNum + i * 6 + j;
			}
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;
		extrudedMesh.uv = extrudeduv;

		state = Status.extrude;
	}

	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	public void startFocus() {
		if (state == Status.freeze) {
			state = Status.focus;
		}
	}

	public void startExtrude() {
		if (state == Status.freeze) {
			state = Status.extrude;
			prepareExtrude();
		}
	}

	public void cancel() {
		state = Status.freemove;
	}
}