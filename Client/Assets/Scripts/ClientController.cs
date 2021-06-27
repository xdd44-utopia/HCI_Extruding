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
	public GameObject sliceTraceVisualizer;
	public Camera renderCamera;
	public Text debugText;
	public Text errorText;

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
	private string rcvBuffer = "";
	private const int msgTypes = 5;
	private string[] sendBuffer = new string[msgTypes];

	private bool isConnected = false;

	private float sendTimer = 0;
	private const float sendInterval = 0.04f;
	private Vector3 accPrev = Vector3.zero;
	
	void Start () {
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
		socketConnection = null;
		cleanSendBuffer();
	}
	
	void Update () {
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		if (!isConnected && socketConnection != null) {
			renderCamera.backgroundColor = connectColor;
			isConnected = true;
			sendMessage("Hello");
		}
		while (rcvBuffer.Length > 0 && !refreshed) {
			switch(rcvBuffer[0]) {
				case '?':
					receivedMessage = "";
					break;
				case '!':
					refreshed = true;
					break;
				default:
					receivedMessage += rcvBuffer[0];
					break;
			}
			rcvBuffer = rcvBuffer.Substring(1);
		}
		if (refreshed) {
			Debug.Log(receivedMessage);
			refreshed = false;
			getVector();
		}

		sendTimer += Time.deltaTime;
		if (sendTimer > sendInterval) {
			sendMsgInBuffer();
			sendTimer = 0;
		}
		Vector3 accConverted = Input.acceleration;
		sendMessage("Acc\n" + accConverted.x + "," + accConverted.y + "," + accConverted.z + "\n");
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
						rcvBuffer += temp;
					}
				}
			}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}
	
	public void sendMessage(string msg) {
		int pointer = -1;
		switch (msg[0]) {
			case 'H': pointer = 0; break;
			case 'T': pointer = 1; break;
			case 'A': pointer = 2; break;
			case 'E': pointer = 3; break;
			case 'F': pointer = 4; break;
		}
		sendBuffer[pointer] = msg + "@";
	}
	public void sendMsgInBuffer() {
		bool hasNewMsg = false;
		string msg = "";
		for (int i=0;i<msgTypes;i++) {
			msg += sendBuffer[i];
			if (sendBuffer[i] != "") {
				hasNewMsg = true;
			}
		}
		if (socketConnection == null || !hasNewMsg) {
			return;
		}
		try {		
			NetworkStream stream = socketConnection.GetStream();
				if (stream.CanWrite) {
					byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
					//Debug.Log("Client sent his message - should be received by server");
					cleanSendBuffer();
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
		Debug.Log(receivedMessage);
		string[] receivedMessageSplit = receivedMessage.Split('@');
		for (int i=0;i<receivedMessageSplit.Length;i++) {
			if (receivedMessageSplit[i].Length > 0) {
				try {
					switch (receivedMessageSplit[i][0]) {
						case 'F':
							string[] temp1 = receivedMessageSplit[i].Split('\n');
							if (temp1[1][0] == 'O') {
								faceTracker.GetComponent<FaceTracker>().useOrtho = true;
							}
							else {
								faceTracker.GetComponent<FaceTracker>().useOrtho = false;
								string[] temp2 = temp1[1].Split(',');
								faceTracker.GetComponent<FaceTracker>().observeOther =
									new Vector3(
										System.Convert.ToSingle(temp2[0]),
										System.Convert.ToSingle(temp2[1]),
										System.Convert.ToSingle(temp2[2])
									);
							}
							break;
						case 'S':
							string[] tempSlice = receivedMessageSplit[i].Split('\n');
							string[] tempThisScreen = tempSlice[1].Split(',');
							string[] tempOtherScreen = tempSlice[2].Split(',');
							Vector3 touchPointThisScreen = convertFromServer(new Vector3(
								System.Convert.ToSingle(tempThisScreen[0]),
								System.Convert.ToSingle(tempThisScreen[1]),
								System.Convert.ToSingle(tempThisScreen[2])
							));
							Vector3 touchPointOtherScreen = convertFromServer(new Vector3(
								System.Convert.ToSingle(tempOtherScreen[0]),
								System.Convert.ToSingle(tempOtherScreen[1]),
								System.Convert.ToSingle(tempOtherScreen[2])
							));
							int count = System.Convert.ToInt32(tempSlice[3]);
							Vector3[] vertices = new Vector3[count];
							for (int j=0;j<count;j++) {
								string[] tempVertex = tempSlice[4 + j].Split(',');
								vertices[j] = convertFromServer(new Vector3(
									System.Convert.ToSingle(tempVertex[0]),
									System.Convert.ToSingle(tempVertex[1]),
									System.Convert.ToSingle(tempVertex[2])
								));
							}
							sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().updateCuttingPlane(vertices, touchPointThisScreen, touchPointOtherScreen);
							break;
						case 'M':
							objectManager.GetComponent<ObjectManager>().updateMesh(receivedMessageSplit[i]);
							break;
						case 'T':
							objectManager.GetComponent<ObjectManager>().updateTransform(receivedMessageSplit[i]);
							break;
						case 'H':
							highlightManager.GetComponent<HighlightManager>().updateHighlight(receivedMessageSplit[i]);
							break;
						case 'A':
							temp1 = receivedMessageSplit[i].Split('\n');
							GameObject.Find("Angles").GetComponent<SliderController>().angle = System.Convert.ToSingle(temp1[1]);
							break;
					}
				}
				catch (Exception e) {
					errorText.text = receivedMessageSplit[i] + "\n" + e.Message;
				}
			}
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

	private void cleanSendBuffer() {
		for (int i=0;i<msgTypes;i++) {
			sendBuffer[i] = "";
		}
	}


}