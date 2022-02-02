using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillController : MonoBehaviour
{
	public Vector3 offset;
	public float depth;
	private Vector3[][] sideVertices = new Vector3[4][];
	private Vector3[] bottomVertices;
	private Vector3[] topVertices;
	private Vector3[] objectVertices;
	// Start is called before the first frame update
	void Start()
	{
		topVertices = GameObject.Find("Face 10").GetComponent<MeshFilter>().mesh.vertices;
		bottomVertices = GameObject.Find("Face 9").GetComponent<MeshFilter>().mesh.vertices;
		sideVertices[0] = GameObject.Find("Face 8").GetComponent<MeshFilter>().mesh.vertices;
		sideVertices[1] = GameObject.Find("Face 7").GetComponent<MeshFilter>().mesh.vertices;
		sideVertices[2] = GameObject.Find("Face 6").GetComponent<MeshFilter>().mesh.vertices;
		sideVertices[3] = GameObject.Find("Face 5").GetComponent<MeshFilter>().mesh.vertices;
		objectVertices = GetComponent<MeshFilter>().mesh.vertices;
		offset = new Vector3(0, 0, 0);
		depth = 0.1f;

		for (int i=0;i<sideVertices[2].Length;i++) {
			Debug.Log("Face " + i + " " + sideVertices[2][i]);
		}

	}

	// Update is called once per frame
	void Update()
	{
		//top 0.5 0.5
		Vector3 tf1 = new Vector3(0.5f, 5, 0.5f) + offset;
		Vector3 tc1 = new Vector3(0.5f, 4.9f, 0.5f) + offset;
		topVertices[1] = tf1;
		topVertices[4] = tf1;
		sideVertices[0][5] = tc1 + new Vector3(-0.01f, 0, 0);
		sideVertices[3][0] = tc1 + new Vector3(0, 0, -0.01f);
		sideVertices[3][3] = tc1 + new Vector3(0, 0, -0.01f);
		objectVertices[20] = tf1;
		objectVertices[35] = tf1;
		objectVertices[41] = tf1;

		//top 0.5 -0.5
		Vector3 tf2 = new Vector3(0.5f, 5, -0.5f) + offset;
		Vector3 tc2 = new Vector3(0.5f, 4.9f, -0.5f) + offset;
		topVertices[0] = tf2;
		topVertices[8] = tf2;
		topVertices[13] = tf2;
		sideVertices[0][0] = tc2 + new Vector3(-0.01f, 0, 0);
		sideVertices[0][3] = tc2 + new Vector3(-0.01f, 0, 0);
		sideVertices[1][5] = tc2 + new Vector3(0, 0, 0.01f);
		objectVertices[31] = tf2;
		objectVertices[32] = tf2;
		objectVertices[40] = tf2;

		//top -0.5 0.5
		Vector3 tf3 = new Vector3(-0.5f, 5, 0.5f) + offset;
		Vector3 tc3 = new Vector3(-0.5f, 4.9f, 0.5f) + offset;
		topVertices[5] = tf3;
		topVertices[10] = tf3;
		topVertices[16] = tf3;
		sideVertices[2][0] = tc3 + new Vector3(0.01f, 0, 0);
		sideVertices[2][3] = tc3 + new Vector3(0.01f, 0, 0);
		sideVertices[3][5] = tc3 + new Vector3(0, 0, -0.01f);
		objectVertices[23] = tf3;
		objectVertices[24] = tf3;
		objectVertices[43] = tf3;

		//top -0.5 -0.5
		Vector3 tf4 = new Vector3(-0.5f, 5, -0.5f) + offset;
		Vector3 tc4 = new Vector3(-0.5f, 4.9f, -0.5f) + offset;
		topVertices[12] = tf4;
		topVertices[17] = tf4;
		topVertices[20] = tf4;
		topVertices[22] = tf4;
		sideVertices[1][0] = tc4 + new Vector3(0, 0, 0.01f);
		sideVertices[1][3] = tc4 + new Vector3(0, 0, 0.01f);
		sideVertices[2][5] = tc4 + new Vector3(0.01f, 0, 0);
		objectVertices[27] = tf4;
		objectVertices[28] = tf4;
		objectVertices[46] = tf4;

		//bottom 0.5 0.5
		Vector3 bf1 = new Vector3(0.5f, 5 - depth, 0.5f) + offset;
		Vector3 bc1 = new Vector3(0.5f, 5.1f - depth, 0.5f) + offset;
		bottomVertices[5] = bf1;
		sideVertices[0][2] = bc1 + new Vector3(-0.01f, 0, 0);
		sideVertices[0][4] = bc1 + new Vector3(-0.01f, 0, 0);
		sideVertices[3][1] = bc1 + new Vector3(0, 0, -0.01f);
		objectVertices[21] = bf1;
		objectVertices[34] = bf1;
		objectVertices[39] = bf1;

		//bottom 0.5 -0.5
		Vector3 bf2 = new Vector3(0.5f, 5 - depth, -0.5f) + offset;
		Vector3 bc2 = new Vector3(0.5f, 5.1f - depth, -0.5f) + offset;
		bottomVertices[0] = bf2;
		bottomVertices[3] = bf2;
		sideVertices[0][1] = bc2 + new Vector3(-0.01f, 0, 0);
		sideVertices[1][2] = bc2 + new Vector3(0, 0, 0.01f);
		sideVertices[1][4] = bc2 + new Vector3(0, 0, 0.01f);
		objectVertices[30] = bf2;
		objectVertices[33] = bf2;
		objectVertices[36] = bf2;

		//bottom -0.5 0.5
		Vector3 bf3 = new Vector3(-0.5f, 5 - depth, 0.5f) + offset;
		Vector3 bc3 = new Vector3(-0.5f, 5.1f - depth, 0.5f) + offset;
		bottomVertices[2] = bf3;
		bottomVertices[4] = bf3;
		sideVertices[2][1] = bc3 + new Vector3(0.01f, 0, 0);
		sideVertices[3][2] = bc3 + new Vector3(0, 0, -0.01f);
		sideVertices[3][4] = bc3 + new Vector3(0, 0, -0.01f);
		objectVertices[22] = bf3;
		objectVertices[25] = bf3;
		objectVertices[38] = bf3;

		//bottom -0.5 -0.5
		Vector3 bf4 = new Vector3(-0.5f, 5 - depth, -0.5f) + offset;
		Vector3 bc4 = new Vector3(-0.5f, 5.1f - depth, -0.5f) + offset;
		bottomVertices[1] = bf4;
		sideVertices[1][1] = bc4 + new Vector3(0, 0, 0.01f);
		sideVertices[2][2] = bc4 + new Vector3(0.01f, 0, 0);
		sideVertices[2][4] = bc4 + new Vector3(0.01f, 0, 0);
		objectVertices[26] = bf4;
		objectVertices[29] = bf4;
		objectVertices[37] = bf4;

		GameObject.Find("Face 10").GetComponent<MeshFilter>().mesh.vertices = topVertices;
		GameObject.Find("Face 10").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Face 9").GetComponent<MeshFilter>().mesh.vertices = bottomVertices;
		GameObject.Find("Face 9").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Face 8").GetComponent<MeshFilter>().mesh.vertices = sideVertices[0];
		GameObject.Find("Face 8").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Face 7").GetComponent<MeshFilter>().mesh.vertices = sideVertices[1];
		GameObject.Find("Face 7").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Face 6").GetComponent<MeshFilter>().mesh.vertices = sideVertices[2];
		GameObject.Find("Face 6").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Face 5").GetComponent<MeshFilter>().mesh.vertices = sideVertices[3];
		GameObject.Find("Face 5").GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GetComponent<MeshFilter>().mesh.vertices = objectVertices;
		GetComponent<MeshFilter>().mesh.RecalculateNormals();
		GameObject.Find("Inside Drilled").GetComponent<MeshFilter>().mesh.vertices = objectVertices;
		GameObject.Find("Inside Drilled").GetComponent<MeshFilter>().mesh.RecalculateNormals();

	}
}
