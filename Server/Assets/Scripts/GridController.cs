using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
	public GameObject sender;
	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		GameObject[] allObjects = GameObject.FindGameObjectsWithTag("Object");
		GameObject refObject = allObjects[0];
		float scale = refObject.transform.localScale.x / refObject.GetComponent<ObjectController>().realMeasure.x;
		scale *= 0.15f;
		this.transform.localScale = new Vector3(scale, scale, scale);
		this.transform.position = new Vector3(43.5f * scale - 2.5f, 83.5f * scale - 5f, 0);
		sender.GetComponent<ServerController>().sendMessage("Grid\n" + scale);
	}
}
