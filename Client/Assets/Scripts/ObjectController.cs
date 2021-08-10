using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour
{
	public GameObject inside;
	[HideInInspector]
	public bool isMeshUpdated;
	public Text debugText;

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;

	//Faces
	private int faceNum = 0;
	private List<List<int>> faceToMeshPointers = new List<List<int>>(); //Categorized triangle indices
	private int[] meshToFacePointers;
	private List<GameObject> faceObj = new List<GameObject>();
	public GameObject facePrefab;
	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
	}

	// Update is called once per frame
	void Update()
	{
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		
	}

	public void updateMesh(string msg) {
		
		//construct mesh

		string[] temp1 = msg.Split('\n');

		int verticesNum = System.Convert.ToInt32(temp1[1]);
		string[] verticesStr = temp1[2].Split(',');
		Vector3[] vertices = new Vector3[verticesNum];
		for (int i=0;i<verticesNum;i++) {
			vertices[i] = new Vector3(
				System.Convert.ToSingle(verticesStr[i * 3 + 0]),
				System.Convert.ToSingle(verticesStr[i * 3 + 1]),
				System.Convert.ToSingle(verticesStr[i * 3 + 2])
			);
		}

		int trianglesNum = System.Convert.ToInt32(temp1[3]);
		string[] trianglesStr = temp1[4].Split(',');
		int[] triangles = new int[trianglesNum];
		for (int i=0;i<trianglesNum;i++) {
			triangles[i] = System.Convert.ToInt32(trianglesStr[i]);
		}

		Mesh mesh = this.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.MarkModified();
		mesh.RecalculateNormals();
		this.GetComponent<MeshFilter>().mesh = mesh;
		this.GetComponent<MeshCollider>().sharedMesh = mesh;
		inside.GetComponent<MeshFilter>().mesh = mesh;

		//categorize coplanar triangles
		
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
					faceVertices[j * 3 + k] = vertices[triangles[faceToMeshPointers[i][j] * 3 + k]];
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
			Mesh faceMesh = faceObj[i].GetComponent<MeshFilter>().mesh;
			faceMesh.Clear();
			faceMesh.vertices = faceVertices;
			faceMesh.triangles = faceTriangles;
			faceMesh.uv = new Vector2[faceToMeshPointers[i].Count * 3];
			faceMesh.MarkModified();
			faceMesh.RecalculateNormals();
			faceObj[i].GetComponent<MeshFilter>().mesh = faceMesh;
		}
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
		this.transform.position = convertFromServer(this.transform.position);
		Quaternion currentRot = this.transform.rotation;
		currentRot = Quaternion.Euler(
			System.Convert.ToSingle(rotationStr[0]),
			System.Convert.ToSingle(rotationStr[1]),
			System.Convert.ToSingle(rotationStr[2])
		);
		currentRot = Quaternion.AngleAxis(- angle * 180 / Mathf.PI, new Vector3(0, 1, 0)) * currentRot;
		this.transform.rotation = currentRot;
		this.transform.localScale = new Vector3(
			System.Convert.ToSingle(scaleStr[0]),
			System.Convert.ToSingle(scaleStr[1]),
			System.Convert.ToSingle(scaleStr[2])
		);

	}

	public void updateHighlight(string msg) {

		string[] temp1 = msg.Split('\n');

		int selectFaceIndex = System.Convert.ToInt32(temp1[1]);
		for (int i=0;i<faceNum;i++) {
			Renderer tempRenderer = faceObj[i].GetComponent<Renderer>();
			tempRenderer.material.SetColor("_Color", (i == selectFaceIndex ? new Color(1f, 1f, 0f, 1f) : new Color(1f, 1f, 1f, 1f)));
		}
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}
}
