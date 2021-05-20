using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectController : MonoBehaviour
{
	public GameObject inside;
	[HideInInspector]
	public int index;
	[HideInInspector]
	public bool isMeshUpdated;
	// Start is called before the first frame update
	void Start()
	{
		index = 0;
	}

	// Update is called once per frame
	void Update()
	{
		if (isMeshUpdated) {
			inside.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
			isMeshUpdated = true;
		}
	}
}
