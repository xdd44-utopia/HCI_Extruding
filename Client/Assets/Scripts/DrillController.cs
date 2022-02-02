using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrillController : MonoBehaviour
{
	private GameObject sender;
	private GameObject obj;
	private GameObject drilledObj;
	private bool isEnabled = false;

	void Start() {
		sender = GameObject.Find("Client");
		obj = GameObject.Find("OBJECT");
		drilledObj = GameObject.Find("DRILLED");
		drilledObj.SetActive(false);
	}

	void Update() {
		drilledObj.transform.position = obj.transform.position;
		drilledObj.transform.rotation = obj.transform.rotation;
		drilledObj.transform.localScale = obj.transform.localScale / 10;
	}

	public void switcher() {
		if (!isEnabled) {
			isEnabled = true;
			this.GetComponent<MeshRenderer>().enabled = true;
			sender.GetComponent<ClientController>().sendMessage("Enable drilling\n");
			enableDrillSimulation();
		}
		else {
			isEnabled = false;
			this.GetComponent<MeshRenderer>().enabled = false;
			sender.GetComponent<ClientController>().sendMessage("Disable drilling\n");
		}
	}
	private void enableDrillSimulation() {
		GameObject.Find("OBJECT").GetComponent<MeshRenderer>().enabled = false;
		GameObject.Find("Inside").GetComponent<MeshRenderer>().enabled = false;
		GameObject[] faces = GameObject.FindGameObjectsWithTag("FaceObj");
		foreach (GameObject face in faces) {
			face.GetComponent<MeshRenderer>().enabled = false;
		}
		drilledObj.SetActive(true);
	}
}
