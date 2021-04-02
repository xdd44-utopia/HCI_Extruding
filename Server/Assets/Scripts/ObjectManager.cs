using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
	public GameObject sender;
	private GameObject[] objects;
	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		objects = GameObject.FindGameObjectsWithTag("Object");
		for (int i=0;i<objects.Length;i++) {
			if (objects[i].GetComponent<ObjectController>().isMeshUpdated) {
				string msg =
					"Mesh\n" + 
					objects[i].GetComponent<ObjectController>().index + "\n";
				Vector3[] vertices = objects[i].GetComponent<MeshFilter>().mesh.vertices;
				msg += vertices.Length + "\n";
				for (int j=0;j<vertices.Length;j++) {
					msg += vertices[j].x + "," + vertices[j].y + "," + vertices[j].z + ",";
				}
				msg += "\n";
				int[] triangles = objects[i].GetComponent<MeshFilter>().mesh.triangles;
				msg += triangles.Length + "\n";
				for (int j=0;j<triangles.Length;j++) {
					msg += triangles[j] + ",";
				}
				msg += "\n";
				sender.GetComponent<ServerController>().sendMessage(msg);
				objects[i].GetComponent<ObjectController>().isMeshUpdated = false;
			}
			if (objects[i].GetComponent<ObjectController>().isTransformUpdated) {
				string msg =
					"Transform\n" + 
					objects[i].GetComponent<ObjectController>().index + "\n" +
					objects[i].transform.position.x + "," +
					objects[i].transform.position.y + "," +
					objects[i].transform.position.z + "\n" +
					objects[i].transform.rotation.eulerAngles.x + "," +
					objects[i].transform.rotation.eulerAngles.y + "," +
					objects[i].transform.rotation.eulerAngles.y + "\n" +
					objects[i].transform.localScale.x + "," +
					objects[i].transform.localScale.y + "," +
					objects[i].transform.localScale.z + "\n";
				sender.GetComponent<ServerController>().sendMessage(msg);
				objects[i].GetComponent<ObjectController>().isTransformUpdated = false;
			}
		}

	}

	public int getNum() {
		return objects.Length;
	}
}
