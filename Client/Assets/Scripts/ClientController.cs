using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ClientController : MonoBehaviour {

	public GameObject touchProcessor;
	public GameObject faceTracker;
	public GameObject objectManager;
	public GameObject highlightManager;
	public Camera renderCamera;
	public Text debugText;

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;

	private Color disconnectColor = new Color(0.8156f, 0.3529f, 0.4313f);
	//private Color connectColor = new Color(0.5254f, 0.7568f, 0.4f);
	private Color connectColor = new Color(0f, 0f, 0f);

	private TcpClient socketConnection;
	private Thread clientReceiveThread;

	private bool refreshed = false;
	private string receivedMessage;
	private string msgBuffer = "";

	private bool isConnected = false;

	private float sendTimer = 0;
	
	void Start () {
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
		socketConnection = null;
	}
	
	void Update () {
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		if (!isConnected && socketConnection != null) {
			renderCamera.backgroundColor = connectColor;
			isConnected = true;
			sendMessage("Hello");
		}
		while (msgBuffer.Length > 0 && !refreshed) {
			switch(msgBuffer[0]) {
				case '?':
					receivedMessage = "";
					break;
				case '!':
					refreshed = true;
					break;
				default:
					receivedMessage += msgBuffer[0];
					break;
			}
			msgBuffer = msgBuffer.Substring(1);
		}
		if (refreshed) {
			Debug.Log(receivedMessage);
			refreshed = false;
			getVector();
		}
		if (sendTimer >= 0.1f) {
			Vector3 accConverted = Input.acceleration;
			sendMessage("Acc\n" + accConverted.x + "," + accConverted.y + "," + accConverted.z);
			sendTimer = 0;
		}
		else {
			sendTimer += Time.deltaTime;
		}
	}
	
	public void ConnectToTcpServer (string ipText) {
		try {
			clientReceiveThread = new Thread(() => ListenForData(ipText));
			clientReceiveThread.IsBackground = true;
			clientReceiveThread.Start();
		}
		catch (Exception e) {
			Debug.Log("On client connect exception " + e);
		}
	}
	
	private void ListenForData(string ipText) {
		socketConnection = null;
		try {
			socketConnection = new TcpClient(ipText, 8052);
			Byte[] bytes = new Byte[1024];
			while (true) {
				Debug.Log("listening");
				using (NetworkStream stream = socketConnection.GetStream()) {
					int length;
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
						var incommingData = new byte[length];
						Array.Copy(bytes, 0, incommingData, 0, length);
						string temp = Encoding.ASCII.GetString(incommingData);
						msgBuffer += temp;
					}
				}
			}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}
	
	public void sendMessage(string msg) {
		//Debug.Log(msg);
		if (socketConnection == null) {
			return;
		}
		try {		
			NetworkStream stream = socketConnection.GetStream();
				if (stream.CanWrite) {
					byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
					//Debug.Log("Client sent his message - should be received by server");
				}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private Vector3 convertToServer(Vector3 v) {
		Vector3 origin = new Vector3(- camWidth / 2 - camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
		Vector3 z = new Vector3(-Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private void getVector() {
		switch (receivedMessage[0]) {
			case 'F':
				string[] temp1 = receivedMessage.Split('\n');
				string[] temp2 = temp1[1].Split(',');
				faceTracker.GetComponent<FaceTracker>().observeOther =
					new Vector3(
						System.Convert.ToSingle(temp2[0]),
						System.Convert.ToSingle(temp2[1]),
						System.Convert.ToSingle(temp2[2])
					);
				break;
			case 'M':
				objectManager.GetComponent<ObjectManager>().updateMesh(receivedMessage);
				break;
			case 'T':
				objectManager.GetComponent<ObjectManager>().updateTransform(receivedMessage);
				break;
			case 'H':
				highlightManager.GetComponent<HighlightManager>().updateHighlight(receivedMessage);
				break;
			case 'A':
				temp1 = receivedMessage.Split('\n');
				GameObject.Find("Angles").GetComponent<SliderController>().angle = System.Convert.ToSingle(temp1[1]);
				break;
		}
	}

	public void connect() {
		string address = "192.168.0.104";
		//Samsung connecting to SCM: 144.214.112.225
		//Samsung connecting to CS Lab: 144.214.112.123
		//Samsung connecting to iPhone hotspot: 172.20.10.6
		//Samsung connecting to xdd44's wifi: 192.168.0.104
		//Macbook local connecting to xdd44's wifi: 192.168.0.101
		//Macbook local connecting to iPhone hotspot: 172.20.10.2
		//iPhone connecting to iPhone hotspot: 10.150.153.190
		//Debug.Log("233");
		ConnectToTcpServer(address);
	}

}