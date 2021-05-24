using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HighlightManager : MonoBehaviour
{
	private Mesh highlight;
	private MeshRenderer mr;
	public Text debugText;

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;
	// Start is called before the first frame update
	void Start()
	{
		highlight = new Mesh();
		GetComponent<MeshFilter>().mesh = highlight;
		mr = GetComponent<MeshRenderer>();

		highlight.vertices = new Vector3[3];
		highlight.uv = new Vector2[3];
		highlight.triangles = new int[]{0, 1, 2};

		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
	}

	// Update is called once per frame
	void Update()
	{
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
	}

	public void updateHighlight(string msg) {
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Object");
		foreach (GameObject obj in objects) {
			obj.GetComponent<Renderer>().material.SetColor("_Color", Color.yellow);
		}
		highlight.Clear();

		string[] temp1 = msg.Split('\n');
		if (temp1[1][0] == 'O') {

			int index = System.Convert.ToInt32(temp1[2]);
			foreach (GameObject obj in objects) {
				if (obj.GetComponent<ObjectController>().index == index) {
					obj.GetComponent<Renderer>().material.SetColor("_Color", Color.white);
					break;
				}
			}
			
		}
		else {

			int verticesNum = System.Convert.ToInt32(temp1[3]);
			string[] verticesStr = temp1[4].Split(',');
			Vector3[] vertices = new Vector3[verticesNum];
			for (int i=0;i<verticesNum;i++) {
				vertices[i] = new Vector3(
					System.Convert.ToSingle(verticesStr[i * 3 + 0]),
					System.Convert.ToSingle(verticesStr[i * 3 + 1]),
					System.Convert.ToSingle(verticesStr[i * 3 + 2])
				);
			}

			int trianglesNum = System.Convert.ToInt32(temp1[5]);
			string[] trianglesStr = temp1[6].Split(',');
			int[] triangles = new int[trianglesNum];
			for (int i=0;i<trianglesNum;i++) {
				triangles[i] = System.Convert.ToInt32(trianglesStr[i]);
			}

			highlight.vertices = vertices;
			highlight.triangles = triangles;
			highlight.MarkModified();
			highlight.RecalculateNormals();

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
}
