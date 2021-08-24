using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
	public GameObject sender;
	public GameObject hitObj;
	[HideInInspector]
	public bool isFixed;
	// Start is called before the first frame update
	void Start()
	{
		isFixed = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (isFixed) {
			this.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
			this.transform.position = new Vector3(43.5f * 0.15f - 2.5f, 83.5f * 0.15f - 5f, 0);
			sender.GetComponent<ServerController>().sendMessage("Grid\n0.15");
		}
		else {
			float scale = hitObj.transform.localScale.x / hitObj.GetComponent<ObjectController>().realMeasure.x;
			scale *= 0.15f;
			this.transform.localScale = new Vector3(scale, scale, scale);
			this.transform.position = new Vector3(43.5f * scale - 2.5f, 83.5f * scale - 5f, 0);
			sender.GetComponent<ServerController>().sendMessage("Grid\n" + scale);
		}
	}
}
