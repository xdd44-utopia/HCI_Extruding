using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CuttingPlaneController : MonoBehaviour
{
	private GameObject sender;
	private bool isEnabled = false;

	void Start() {
		sender = GameObject.Find("Client");
	}

	void Update() {
	}

	public void switcher() {
		if (!isEnabled) {
			isEnabled = true;
			this.GetComponent<MeshRenderer>().enabled = true;
			sender.GetComponent<ClientController>().sendMessage("Enable cutting plane\n");
		}
		else {
			isEnabled = false;
			this.GetComponent<MeshRenderer>().enabled = false;
			sender.GetComponent<ClientController>().sendMessage("Execute cutting plane\n");
		}
	}
}
