using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
	public GameObject sender;
	public GameObject depthFrame;
	[HideInInspector]
	public bool isFixed;
	private float prevScale = 0;
	private bool prevStatus;
	// Start is called before the first frame update
	void Start()
	{
		isFixed = false;
		prevStatus = false;
	}

	// Update is called once per frame
	void Update()
	{
		this.GetComponent<SpriteRenderer>().enabled = isFixed;
		depthFrame.SetActive(!isFixed);
		if (prevStatus != isFixed) {
			sender.GetComponent<ServerController>().sendMessage("Grid\n" + (isFixed ? 'T' : 'F'));
			prevStatus = isFixed;
		}
	}
}
