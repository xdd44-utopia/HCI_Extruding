using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectManager : MonoBehaviour
{
	public GameObject prefab;
	public Text debugText;
	private GameObject[] objects;

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;
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
		objects = GameObject.FindGameObjectsWithTag("Object");
	}

	public void updateMesh(string msg) {
		string[] temp1 = msg.Split('\n');
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
			target.GetComponent<ObjectController>().index = index;
		}
		target.GetComponent<ObjectController>().isMeshUpdated = true;

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

		string[] positionStr = temp1[2].Split(',');
		string[] rotationStr = temp1[3].Split(',');
		string[] scaleStr = temp1[4].Split(',');

		GameObject target = null;
		for (int i=0;i<objects.Length;i++) {
			if (objects[i].GetComponent<ObjectController>().index == index) {
				target = objects[i];
			}
		}

		if (target == null) {
			return;
		}

		target.transform.position = new Vector3(
			System.Convert.ToSingle(positionStr[0]),
			System.Convert.ToSingle(positionStr[1]),
			System.Convert.ToSingle(positionStr[2])
		);
		target.transform.position = convertFromServer(target.transform.position);
		Quaternion currentRot = target.transform.rotation;
		currentRot = Quaternion.Euler(
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

		//debugText.text = target.transform.position + "\n" + target.transform.rotation.eulerAngles + "\n" + target.transform.localScale;

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
}
