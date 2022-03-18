using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
	public GameObject depthFrame;
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
		this.GetComponent<SpriteRenderer>().enabled = isFixed;
		depthFrame.SetActive(!isFixed);
	}
}
