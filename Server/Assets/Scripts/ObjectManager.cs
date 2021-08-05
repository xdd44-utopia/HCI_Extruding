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

	}

	public int getNum() {
		return objects.Length;
	}

	public void angleChanged() {
		for (int i=0;i<objects.Length;i++) {
			objects[i].GetComponent<ObjectController>().isTransformUpdated = true;
		}
	}

	public void refresh() {
		for (int i=0;i<objects.Length;i++) {
			objects[i].GetComponent<ObjectController>().isMeshUpdated = true;
			objects[i].GetComponent<ObjectController>().isTransformUpdated = true;
		}
	}
}
