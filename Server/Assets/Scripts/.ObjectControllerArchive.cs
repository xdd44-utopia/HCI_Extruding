using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectControllerArchive : MonoBehaviour
{

	private GameObject inside;
	private GameObject meshManipulator;
	private GameObject sender;
	private LineRenderer selectLine;
	public bool isTransformUpdated;
	[HideInInspector]
	public bool isMeshUpdated;
	[HideInInspector]
	public bool isRealMeasure;
	[HideInInspector]
	public Vector3 realMeasure = new Vector3(1, 1, 1);

	//Mesh
	private Vector3[] vertices;
	private int[] triangles;

	//Faces
	private int faceNum = 0;
	private List<List<int>> faceToMeshPointers = new List<List<int>>(); //Categorized triangle indices
	private List<List<int>> edges = new List<List<int>>(); //Categorized edge indices
	private int[] meshToFacePointers;
	private List<GameObject> faceObj = new List<GameObject>();
	public GameObject facePrefab;

	//Selected face
	private int selectTriangleIndex = -1; //-1: select object, >=0: select face
	private int selectFaceIndex = -1;
	private int prevSelectFaceIndex = -1;
	private int snappedTriangleIndex = -1;
	private int snappedFaceIndex = -1;
	private int prevSnappedFaceIndex = -1;
	private int alignFaceIndex = -1;
	private int prevAlignFaceIndex = -1;

	//Colors
	private Color generalColor = new Color(0.5f, 0.5f, 0.5f, 1f);
	// private Color selectColor = new Color(1f, 1f, 0f, 1f);
	private Color snappedColor = new Color(1f, 1f, 0f, 1f);
	private Color alignColor = new Color(0f, 1f, 0f, 1f);

	void Start()
	{
		inside = GameObject.Find("Inside");
		meshManipulator = GameObject.Find("MeshManipulator");
		sender = GameObject.Find("Server");
		selectLine = GameObject.Find("Select").GetComponent<LineRenderer>();

		isTransformUpdated = true;
		isMeshUpdated = true;
		isRealMeasure = false;
		selectLine.positionCount = 0;
		
	}

	// Update is called once per frame
	void Update() {
		isRealMeasure = (this.transform.localScale - realMeasure).magnitude < 0.01f;
		try {
			selectFaceIndex = (selectTriangleIndex == -1 ? -1 : meshToFacePointers[selectTriangleIndex]);
			snappedFaceIndex = (snappedTriangleIndex == -1 ? -1 : meshToFacePointers[snappedTriangleIndex]);
		}
		catch (Exception e) {
			cleanHighlight();
			meshManipulator.GetComponent<MeshManipulator>().cancel();
		}
		updateHighlight();
		if (isMeshUpdated) {
			simplifyMesh();
			updateVisualization();
			inside.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
		}
		if (isTransformUpdated) {
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
			if (sender != null) {
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			isTransformUpdated = false;
			selectLine.SetWidth(0.025f * this.transform.localScale.x, 0.025f * this.transform.localScale.x);
		}
	}

	private void simplifyMesh() {

		vertices = this.GetComponent<MeshFilter>().mesh.vertices;
		triangles = this.GetComponent<MeshFilter>().mesh.triangles;

		updateFaces();
		updateEdges();

		//Simplify edges
		for (int i=0;i<faceNum;i++) {
			while (true) {
				int prevCount = edges[i].Count;
				for (int j=0;j<edges[i].Count;j++) {
					int prev = (j + edges[i].Count - 1) % edges[i].Count;
					int next = (j + 1) % edges[i].Count;
					if (crossProduct(vertices[edges[i][next]] - vertices[edges[i][j]], vertices[edges[i][j]] - vertices[edges[i][prev]]).magnitude < 0.001f) {
						edges[i].RemoveAt(j);
						break;
					}
				}
				if (edges[i].Count == prevCount) {
					break;
				}
			}
		}

		//Reconstruct faces
		List<int> newTrianglesList = new List<int>();
		for (int i=0;i<faceNum;i++) {
			int edgeCount = edges[i].Count;
			if (edgeCount != 0) {
			int triangleCount = newTrianglesList.Count;
			while (edges[i].Count > 3) {
				int prevCount = edges[i].Count;
				int loopCount = 0;
				int j = UnityEngine.Random.Range(0, edges[i].Count);
				while (loopCount < prevCount) {
					int prev1 = (j + edges[i].Count - 1) % edges[i].Count;
					int prev2 = (j + edges[i].Count - 2) % edges[i].Count;
					int next1 = (j + 1) % edges[i].Count;
					int next2 = (j + 2) % edges[i].Count;
					bool isIntersect = false;
					if (Vector3.Angle(
							vertices[edges[i][next1]] - vertices[edges[i][j]],
							vertices[edges[i][next2]] - vertices[edges[i][next1]]
						) <
						Vector3.Angle(
							vertices[edges[i][next1]] - vertices[edges[i][j]],
							vertices[edges[i][prev1]] - vertices[edges[i][next1]]
						)
					) {
						for (int k=0;k<edges[i].Count;k++) {
							if (k != j && k != prev1 && k != prev2 && k != next1 &&
								areLinesIntersect(
									vertices[edges[i][prev1]],
									vertices[edges[i][next1]],
									vertices[edges[i][k]],
									vertices[edges[i][(k+1)%edges[i].Count]]
								)
							) {
								isIntersect = true;
								break;
							}
						}
						if (!isIntersect) {
							newTrianglesList.Add(edges[i][prev1]);
							newTrianglesList.Add(edges[i][j]);
							newTrianglesList.Add(edges[i][next1]);
							edges[i].RemoveAt(j);
						}
					}
					if (!isIntersect) {
						break;
					}
					j++;
					loopCount++;
					if (j == edges[i].Count) {
						j = 0;
					}
				}
			}
			newTrianglesList.Add(edges[i][0]);
			newTrianglesList.Add(edges[i][1]);
			newTrianglesList.Add(edges[i][2]);
			}
		}
		triangles = new int[newTrianglesList.Count];
		for (int i=0;i<newTrianglesList.Count;i++) {
			triangles[i] = newTrianglesList[i];
		}

		Mesh mesh = GetComponent<MeshFilter>().mesh;

		//Remove repeated and not used vertices
		List<Vector3> tempVertices = new List<Vector3>();
		bool[] isUsed = new bool[vertices.Length];
		for (int i=0;i<vertices.Length;i++) {
			isUsed[i] = false;
		}
		for (int i=0;i<vertices.Length;i++) {
			if(!isUsed[i]) {
				bool isUsedByTriangle = false;
				tempVertices.Add(vertices[i]);
				for (int j=i;j<vertices.Length;j++) {
					if (Vector3.Distance(vertices[i], vertices[j]) < 0.001f) {
						isUsed[j] = true;
					}
				}
				for (int j=0;j<triangles.Length;j++) {
					if (Vector3.Distance(vertices[i], vertices[triangles[j]]) < 0.001f) {
						isUsedByTriangle = true;
						triangles[j] = tempVertices.Count - 1;
					}
				}
				if (!isUsedByTriangle) {
					tempVertices.RemoveAt(tempVertices.Count - 1);
				}
			}
		}
		vertices = new Vector3[tempVertices.Count];
		for (int i=0;i<tempVertices.Count;i++) {
			vertices[i] = tempVertices[i];
		}
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = new Vector2[vertices.Length];
		mesh.MarkModified();
		mesh.RecalculateNormals();
		GetComponent<MeshFilter>().mesh = mesh;
		GetComponent<MeshCollider>().sharedMesh = mesh;

	}

	private void updateVisualization() {

		vertices = this.GetComponent<MeshFilter>().mesh.vertices;
		triangles = this.GetComponent<MeshFilter>().mesh.triangles;

		updateFaces();
		updateEdges();
		updateCovers();
		sendMesh();
		
	}

	private void updateFaces() {

		vertices = this.GetComponent<MeshFilter>().mesh.vertices;
		triangles = this.GetComponent<MeshFilter>().mesh.triangles;

		meshToFacePointers = new int[triangles.Length / 3];
		int n = triangles.Length / 3;
		int cnt = n;
		bool[] isVisited = new bool[n];
		for (int i=0;i<n;i++) {
			isVisited[i] = false;
		}
		faceToMeshPointers.Clear();

		for (int i=0;i<n;i++) {
			if ((vertices[triangles[i * 3 + 0]] - vertices[triangles[i * 3 + 1]]).magnitude < 0.001f ||
			(vertices[triangles[i * 3 + 0]] - vertices[triangles[i * 3 + 2]]).magnitude < 0.001f ||
			(vertices[triangles[i * 3 + 1]] - vertices[triangles[i * 3 + 2]]).magnitude < 0.001f
			) {
				isVisited[i] = true;
			}
		}

		for (int i=0;(i < n && cnt > 0);i++) {
			if (!isVisited[i]) {
				faceToMeshPointers.Add(new List<int>());
				Vector3 localNormal = crossProduct(vertices[triangles[i * 3 + 0]] - vertices[triangles[i * 3 + 1]], vertices[triangles[i * 3 + 0]] - vertices[triangles[i * 3 + 2]]);

				Queue<int> bs = new Queue<int>();
				bs.Enqueue(i);
				while (bs.Count > 0) {
					int idx = bs.Peek();
					bs.Dequeue();
					Vector3 currentNormal = crossProduct(
						vertices[triangles[idx * 3 + 0]] - vertices[triangles[idx * 3 + 1]],
						vertices[triangles[idx * 3 + 0]] - vertices[triangles[idx * 3 + 2]]
					);
					if (localNormal.normalized == currentNormal.normalized) {
						isVisited[idx] = true;
						faceToMeshPointers[faceToMeshPointers.Count - 1].Add(idx);
						meshToFacePointers[idx] = faceToMeshPointers.Count - 1;
						cnt--;
						for (int j=0;j<n;j++) {
							if (!isVisited[j]) {
								int matchCount = 0;
								for (int v1=0;v1<3;v1++) {
									for (int v2=0;v2<3;v2++) {
										if (vertices[triangles[idx * 3 + v1]] == vertices[triangles[j * 3 + v2]]) {
											matchCount++;
										}
									}
								}
								if (matchCount >= 2) {
									bs.Enqueue(j);
								}
							}
						}
					}
				}
			}
		}

		faceNum = faceToMeshPointers.Count;

	}

	private void updateEdges() {
		
		vertices = this.GetComponent<MeshFilter>().mesh.vertices;
		triangles = this.GetComponent<MeshFilter>().mesh.triangles;

		edges.Clear();
		for (int i=0;i<faceNum;i++) {
			List<int> edgeVertices = new List<int>();
			int triangleNum = faceToMeshPointers[i].Count;

			int[] edgeLeft = new int[triangleNum * 3];
			int[] edgeRight = new int[triangleNum * 3];
			bool[] isDup = new bool[triangleNum * 3];
			for (int j=0;j<triangleNum * 3;j++) {
				isDup[j] = false;
			}

			for (int j=0;j<triangleNum;j++) {
				for (int k=0;k<3;k++) {
					edgeLeft[j * 3 + k] = triangles[faceToMeshPointers[i][j] * 3 + k];
					edgeRight[j * 3 + k] = triangles[faceToMeshPointers[i][j] * 3 + ((k + 1) % 3)];
				}
			}

			for (int j=0;j<triangleNum * 3;j++) {
				for (int k=j+1;k<triangleNum * 3;k++) {
					if ( !isDup[j] && !isDup[k] &&
						((vertices[edgeLeft[j]] == vertices[edgeLeft[k]] && vertices[edgeRight[j]] == vertices[edgeRight[k]]) ||
						(vertices[edgeLeft[j]] == vertices[edgeRight[k]] && vertices[edgeRight[j]] == vertices[edgeLeft[k]]))
					) {
						isDup[j] = true;
						isDup[k] = true;
					}
				}
			}

			int startVertex = 0;
			for (int j=0;j<triangleNum * 3;j++) {
				if (!isDup[j]) {
					startVertex = j;
					break;
				}
			}

			edgeVertices.Add(edgeLeft[startVertex]);
			edgeVertices.Add(edgeRight[startVertex]);
			isDup[startVertex] = true;
			bool done = false;
			while (!done) {
				done = true;
				for (int j=0;j<triangleNum * 3;j++) {
					if (!isDup[j]) {
						if (vertices[edgeLeft[j]] == vertices[edgeVertices[edgeVertices.Count - 1]]) {
							edgeVertices.Add(edgeRight[j]);
							isDup[j] = true;
							done = false;
							break;
						}
						else if (vertices[edgeRight[j]] == vertices[edgeVertices[edgeVertices.Count - 1]]) {
							edgeVertices.Add(edgeLeft[j]);
							isDup[j] = true;
							done = false;
							break;
						}
					}
				}
			}
			edgeVertices.RemoveAt(edgeVertices.Count - 1);
			
			edges.Add(edgeVertices);
		}

	}

	private void updateCovers() {

		while (faceNum > faceObj.Count) {
			faceObj.Add(Instantiate(facePrefab, new Vector3(0, 0, 0), Quaternion.identity));
			faceObj[faceObj.Count - 1].transform.parent = this.transform;
			faceObj[faceObj.Count - 1].transform.localScale = new Vector3(1.001f, 1.001f, 1.001f);
			faceObj[faceObj.Count - 1].transform.localPosition = new Vector3(0, 0, 0);
			faceObj[faceObj.Count - 1].transform.localRotation = Quaternion.identity;
			faceObj[faceObj.Count - 1].GetComponent<MeshFilter>().mesh = new Mesh();
		}
		while (faceNum < faceObj.Count) {
			GameObject temp = faceObj[faceObj.Count - 1];
			faceObj.RemoveAt(faceObj.Count - 1);
			Destroy(temp, 0);
		}

		for (int i=0;i<faceNum;i++) {
			int triangleNum = faceToMeshPointers[i].Count;
			Vector3[] faceVertices = new Vector3[triangleNum * 3];
			int[] faceTriangles = new int[triangleNum * 3];
			Vector3 faceCenter = new Vector3(0, 0, 0);
			for (int j=0;j<triangleNum;j++) {
				Vector3 localNormal = crossProduct(vertices[triangles[faceToMeshPointers[i][j] * 3 + 0]] - vertices[triangles[faceToMeshPointers[i][j] * 3 + 1]], vertices[triangles[faceToMeshPointers[i][j] * 3 + 0]] - vertices[triangles[faceToMeshPointers[i][j] * 3 + 2]]);
				localNormal = localNormal.normalized;
				for (int k=0;k<3;k++) {
					faceVertices[j * 3 + k] = vertices[triangles[faceToMeshPointers[i][j] * 3 + k]] + 0.00001f * localNormal;
					faceTriangles[j * 3 + k] = j * 3 + k;
					faceCenter += faceVertices[j * 3 + k];
				}
			}
			faceCenter /= triangleNum * 3;
			for (int j=0;j<triangleNum;j++) {
				for (int k=0;k<3;k++) {
					faceVertices[j * 3 + k] = (faceVertices[j * 3 + k] - faceCenter) * 0.975f + faceCenter;
				}
			}
			Mesh mesh = faceObj[i].GetComponent<MeshFilter>().mesh;
			mesh.Clear();
			mesh.vertices = faceVertices;
			mesh.triangles = faceTriangles;
			mesh.uv = new Vector2[faceToMeshPointers[i].Count * 3];
			mesh.MarkModified();
			mesh.RecalculateNormals();
			faceObj[i].GetComponent<MeshFilter>().mesh = mesh;
		}

	}

	private void sendMesh() {

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
		if (sender != null) {
			sender.GetComponent<ServerController>().sendMessage(msg);
		}
		isMeshUpdated = false;

	}

	public void cleanHighlight() {
		snappedTriangleIndex = -1;
		selectTriangleIndex = -1;
		snappedFaceIndex = -1;
		selectFaceIndex = -1;
		updateHighlight();
	}

	private void updateHighlight() {
		if (selectFaceIndex != prevSelectFaceIndex || snappedFaceIndex != prevSnappedFaceIndex || alignFaceIndex != prevAlignFaceIndex) {
			string msg = "Highlight\n" + selectFaceIndex + "\n" + snappedFaceIndex + "\n" + alignFaceIndex;
			if (sender != null) {
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			prevSelectFaceIndex = selectFaceIndex;
			prevAlignFaceIndex = alignFaceIndex;
			prevSnappedFaceIndex = snappedFaceIndex;
		}
		for (int i=0;i<faceNum;i++) {
			Renderer tempRenderer = faceObj[i].GetComponent<Renderer>();
			tempRenderer.material.SetColor("_Color", generalColor);
			if (i == snappedFaceIndex) {
				tempRenderer.material.SetColor("_Color", snappedColor);
			}
			if (i == alignFaceIndex) {
				tempRenderer.material.SetColor("_Color", alignColor);
			}
		}
		if (selectFaceIndex == -1) {
			selectLine.positionCount = 0;
		}
		else {
			selectLine.positionCount = edges[selectFaceIndex].Count + 1;
			for (int i=0;i<edges[selectFaceIndex].Count;i++) {
				selectLine.SetPosition(i, vertices[edges[selectFaceIndex][i]]);
			}
			selectLine.SetPosition(edges[selectFaceIndex].Count, vertices[edges[selectFaceIndex][0]]);
		}
	}

	public void updateAlignFace(int si) {
		if (si == -1) {
			alignFaceIndex = -1;
		}
		else {
			alignFaceIndex = meshToFacePointers[si];
			if (alignFaceIndex != prevAlignFaceIndex) {
				updateHighlight();
				prevAlignFaceIndex = alignFaceIndex;
			}
		}
	}

	public int selectFace(int si) {
		int tempFace = meshToFacePointers[si];
		if (selectFaceIndex == tempFace) {
			selectTriangleIndex = -1;
		}
		else {
			selectTriangleIndex = si;
		}
		return findFirstTriangle(selectTriangleIndex);
	}

	public void newFocus() {
		snappedTriangleIndex = selectTriangleIndex;
		int tempFace = meshToFacePointers[selectTriangleIndex];
		meshManipulator.GetComponent<MeshManipulator>().selectTriangles = faceToMeshPointers[tempFace];
		meshManipulator.GetComponent<MeshManipulator>().selectEdgeVertices = edges[tempFace];
		//meshManipulator.GetComponent<MeshManipulator>().focusTriangleIndex = findFirstTriangle(selectTriangleIndex);
	}

	public void removeFocus() {
		snappedTriangleIndex = -1;
	}

	private int findFirstTriangle(int idx) {
		if (idx == -1) {
			return -1;
		}
		for (int i=0;i<meshToFacePointers.Length;i++) {
			if (meshToFacePointers[i] == meshToFacePointers[idx]) {
				return i;
			}
		}
		return -1;
	}

	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
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
}
