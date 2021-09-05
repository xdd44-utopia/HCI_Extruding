using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
	public GameObject sender;
	public GameObject hitObj;
	[HideInInspector]
	public bool isFixed;
	private float prevScale = 0;
	// Start is called before the first frame update
	void Start()
	{
		isFixed = false;
	}

	// Update is called once per frame
	void Update()
	{
		float scale;
		if (isFixed) {
			this.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
			this.transform.position = new Vector3(43.5f * 0.15f - 2.5f, 83.5f * 0.15f - 5f, 0);
			scale = 0.15f;
		}
		else {
			scale = hitObj.transform.localScale.x / hitObj.GetComponent<ObjectController>().realMeasure.x;
			scale *= 0.15f;
			this.transform.localScale = new Vector3(scale, scale, scale);
			this.transform.position = new Vector3(43.5f * scale - 2.5f, 83.5f * scale - 5f, 0);
		}
		if (Mathf.Abs(prevScale - scale) > 0.01f) {
			sender.GetComponent<ServerController>().sendMessage("Grid\n" + scale);
			prevScale = scale;
		}
	}
}
