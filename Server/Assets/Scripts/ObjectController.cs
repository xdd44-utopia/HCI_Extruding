using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour
{

	public GameObject inside;
	public GameObject meshManipulator;
	public GameObject sender;
	public Text debugText;
	[HideInInspector]
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
	private int snappedTriangleIndex = -1;
	private int snappedFaceIndex = -1;

	//Colors
	private Color generalColor = new Color(0.25f, 0.25f, 0.25f, 1f);
	private Color selectColor = new Color(1f, 1f, 0f, 1f);
	private Color snappedColor = new Color(1f, 1f, 1f, 1f);

	void Start()
	{
		isTransformUpdated = true;
		isMeshUpdated = true;
		isRealMeasure = false;
	}

	// Update is called once per frame
	void Update()
	{
		selectFaceIndex = (selectTriangleIndex == -1 ? -1 : meshToFacePointers[selectTriangleIndex]);
		snappedFaceIndex = (snappedTriangleIndex == -1 ? -1 : meshToFacePointers[snappedTriangleIndex]);
		updateHighlight();
		if (isMeshUpdated) {
			inside.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
			updateCover();
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
			sender.GetComponent<ServerController>().sendMessage(msg);
			isTransformUpdated = false;
		}
	}

	void updateCover() {

		//categorize coplanar triangles

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

		//construct faces
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
					faceVertices[j * 3 + k] = vertices[triangles[faceToMeshPointers[i][j] * 3 + k]] + 0.01f * localNormal;
					faceTriangles[j * 3 + k] = j * 3 + k;
					faceCenter += faceVertices[j * 3 + k];
				}
			}
			faceCenter /= triangleNum * 3;
			for (int j=0;j<triangleNum;j++) {
				for (int k=0;k<3;k++) {
					faceVertices[j * 3 + k] = (faceVertices[j * 3 + k] - faceCenter) * 0.99f + faceCenter;
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

		//find edges
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

		//Send out mesh
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
		sender.GetComponent<ServerController>().sendMessage(msg);
		isMeshUpdated = false;
		
	}

	private void updateHighlight() {
		string msg = "Highlight\n" + selectFaceIndex + "\n" + snappedFaceIndex;
		sender.GetComponent<ServerController>().sendMessage(msg);
		for (int i=0;i<faceNum;i++) {
			Renderer tempRenderer = faceObj[i].GetComponent<Renderer>();
			tempRenderer.material.SetColor("_Color", generalColor);
			if (i == snappedFaceIndex) {
				tempRenderer.material.SetColor("_Color", snappedColor);
			}
			if (i == selectFaceIndex) {
				tempRenderer.material.SetColor("_Color", selectColor);
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
		meshManipulator.GetComponent<MeshManipulator>().focusTriangleIndex = findFirstTriangle(selectTriangleIndex);
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
}
