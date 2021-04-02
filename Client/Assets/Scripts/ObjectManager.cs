using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
	public GameObject prefab;
	public GameObject sender;
	private GameObject[] objects;

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;
	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
	}

	// Update is called once per frame
	void Update()
	{
		objects = GameObject.FindGameObjectsWithTag("Object");
	}

	public void updateMesh(string msg) {
		string[] temp1 = msg.Split('\n');
		Debug.Log(temp1[1]);
		Debug.Log(temp1[2]);
		Debug.Log(temp1[3]);
		Debug.Log(temp1[4]);
		Debug.Log(temp1[5]);
		int index = System.Convert.ToInt32(temp1[1]);

		int verticesNum = System.Convert.ToInt32(temp1[2]);
		string[] verticesStr = temp1[3].Split(',');
		Vector3[] vertices = new Vector3[verticesNum];
		for (int i=0;i<verticesNum;i++) {
			vertices[i] = new Vector3(
				System.Convert.ToSingle(verticesStr[i * 3 + 0]),
				System.Convert.ToSingle(verticesStr[i * 3 + 1]),
				System.Convert.ToSingle(verticesStr[i * 3 + 2])
			);
		}

		int trianglesNum = System.Convert.ToInt32(temp1[4]);
		string[] trianglesStr = temp1[5].Split(',');
		int[] triangles = new int[trianglesNum];
		for (int i=0;i<trianglesNum;i++) {
			triangles[i] = System.Convert.ToInt32(trianglesStr[i]);
		}

		GameObject target = null;
		for (int i=0;i<objects.Length;i++) {
			if (objects[i].GetComponent<ObjectController>().index == index) {
				target = objects[i];
			}
		}
		if (target == null) {
			target = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);
		}

		Mesh mesh = target.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.MarkModified();
		mesh.RecalculateNormals();
		target.GetComponent<MeshFilter>().mesh = mesh;
		target.GetComponent<MeshCollider>().sharedMesh = mesh;
	}

	public void updateTransform(string msg) {
		string[] temp1 = msg.Split('\n');
		int index = System.Convert.ToInt32(temp1[1]);
		Debug.Log(temp1[1]);
		Debug.Log(temp1[2]);
		Debug.Log(temp1[3]);
		Debug.Log(temp1[4]);

		string[] positionStr = temp1[2].Split(',');
		string[] rotationStr = temp1[3].Split(',');
		string[] scaleStr = temp1[4].Split(',');

		GameObject target = null;
		for (int i=0;i<objects.Length;i++) {
			if (objects[i].GetComponent<ObjectController>().index == index) {
				target = objects[i];
			}
		}

		target.transform.position = new Vector3(
			System.Convert.ToSingle(positionStr[0]),
			System.Convert.ToSingle(positionStr[1]),
			System.Convert.ToSingle(positionStr[2])
		);
		target.transform.position = convertFromServer(target.transform.position);
		Quaternion currentRot = target.transform.rotation;
		currentRot.eulerAngles = new Vector3(
			System.Convert.ToSingle(rotationStr[0]),
			System.Convert.ToSingle(rotationStr[1]),
			System.Convert.ToSingle(rotationStr[2])
		);
		currentRot = Quaternion.AngleAxis(- angle * 180 / Mathf.PI, new Vector3(0, 1, 0)) * currentRot;
		target.transform.rotation = currentRot;
		target.transform.localScale = new Vector3(
			System.Convert.ToSingle(scaleStr[0]),
			System.Convert.ToSingle(scaleStr[1]),
			System.Convert.ToSingle(scaleStr[2])
		);

	}
	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(Mathf.PI - angle) / 2, 0, - camWidth * Mathf.Sin(Mathf.PI - angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(Mathf.PI - angle), 0, - Mathf.Sin(Mathf.PI - angle));
		Vector3 z = new Vector3(Mathf.Cos(angle - Mathf.PI / 2), 0, Mathf.Sin(angle - Mathf.PI / 2));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}
}
