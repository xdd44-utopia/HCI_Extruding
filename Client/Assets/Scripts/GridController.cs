using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridController : MonoBehaviour
{
	[HideInInspector]
	public float scale = 1;
	public Text debugText;
	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		this.transform.localScale = new Vector3(scale, scale, scale);
		this.transform.position = new Vector3(43.5f * scale - 2.5f, 83.5f * scale - 5f, 0);
		debugText.text = scale + " ";
	}
}
