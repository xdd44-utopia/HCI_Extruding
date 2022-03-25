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

	public FaceTracker faceTracker;
	public ObjectController objectController;
	public GridController gridController;
	public ExtrudeHandle extrudeHandle;
	public GameObject connectButton;
	public GameObject IPInput;


	public Camera renderCamera;
	public Text debugText;
	public Text errorText;
	public Text pingText;

	private Color disconnectColor = new Color(0.8156f, 0.3529f, 0.4313f);
	//private Color connectColor = new Color(0.5254f, 0.7568f, 0.4f);
	private Color connectColor = new Color(0f, 0f, 0f);

	private TcpClient socketConnection;
	private Thread clientReceiveThread;

	private string receivedMessage;
	private string rcvBuffer = "";
	private const int msgTypes = 10;
	private string[] sendBuffer = new string[msgTypes];

	private bool isConnected = false;

	private float sendTimer = 0;
	private const float sendInterval = 0.16f;
	private Vector3 accPrev = Vector3.zero;
	
	void Start () {

		Camera cam = Camera.main;
		VectorCalculator.camHeight = 2f * cam.orthographicSize;
		VectorCalculator.camWidth = VectorCalculator.camHeight * cam.aspect;
		renderCamera.backgroundColor = disconnectColor;

		socketConnection = null;
		for (int i=0;i<msgTypes;i++) {
			sendBuffer[i] = "";
		}
	}
	
	void Update () {
		if (!isConnected && socketConnection != null) {
			renderCamera.backgroundColor = connectColor;
			isConnected = true;
			sendMessage("Hello");
		}
		while (rcvBuffer.Length > 0) {
			switch(rcvBuffer[0]) {
				case '?':
					receivedMessage = "";
					break;
				case '!':
					getVector();
					break;
				default:
					receivedMessage += rcvBuffer[0];
					break;
			}
			rcvBuffer = rcvBuffer.Substring(1);
		}

		sendTimer += Time.deltaTime;
		if (sendTimer > sendInterval) {
			sendMsgInBuffer();
			sendTimer = 0;
		}
		Vector3 accConverted = Input.acceleration;
		if (Vector3.Distance(accPrev, accConverted) > 0.001f) {
			sendMessage("Acc\n" + accConverted.x + "," + accConverted.y + "," + accConverted.z + "\n");
			accPrev = accConverted;
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
			//case 'L': pointer = 0; break;
			case 'H': pointer = 1; break;
			case 'T': pointer = 2; break;
			case 'A': pointer = 3; break;
			case 'F': pointer = 4; break;
			case 'S': pointer = 5; break;
			default:
				if (msg[0] == 'E' && msg[7] == 'c')
					pointer = 6;
				if (msg[0] == 'E' && msg[8] == 'c')
					pointer = 7;
				if (msg[0] == 'R' && msg[1] == 'M')
					pointer = 8;
				if (msg[0] == 'R' && msg[1] == 'T')
					pointer = 9;
				break;
		}
		sendBuffer[pointer] = msg;
		sendBuffer[0] = "Latency\n" + ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 600000);
	}
	public void sendMsgInBuffer() {
		if (socketConnection == null) {
			return;
		}
		// for (int i=0;i<msgTypes;i++) {
		// 	if (sendBuffer[i].Length > 0) {
		// 		string msg = sendBuffer[i];
		// 		try {		
		// 			NetworkStream stream = socketConnection.GetStream();
		// 				if (stream.CanWrite) {
		// 					byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
		// 					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
		// 					// Debug.Log("Client sent his message： " + "?" + msg + "!");
		// 					sendBuffer[i] = "";
		// 				}
		// 		}
		// 		catch (SocketException socketException) {
		// 			Debug.Log("Socket exception: " + socketException);
		// 		}
		// 	}
		// }
		string msg = "";
		for (int i=0;i<msgTypes;i++) {
			if (sendBuffer[i].Length > 0) {
				msg += "?" + sendBuffer[i] + "!\n";
			}
		}
		try {		
			NetworkStream stream = socketConnection.GetStream();
				if (stream.CanWrite) {
					byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(msg);
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
					for (int i=0;i<msgTypes;i++) {
						sendBuffer[i] = "";
					}
				}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}

	private void getVector() {

		connectButton.SetActive(false);
		IPInput.SetActive(false);

		try {
			switch (receivedMessage[0]) {
				case 'L': {
					string[] temp1 = receivedMessage.Split('\n');
					int cur = (int)((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % 600000);
					int sen = System.Convert.ToInt32(temp1[1]);
					pingText.text = (cur - sen) + " ms";
					break;
				}
				case 'M':
					objectController.updateMesh(receivedMessage);
					break;
				case 'A': {
					string[] temp1 = receivedMessage.Split('\n');
					VectorCalculator.angle = System.Convert.ToSingle(temp1[1]);
					break;
				}
				case 'H': {
					string[] temp1 = receivedMessage.Split('\n');
					int selectFace = System.Convert.ToInt32(temp1[1]);
					objectController.updateSelect(selectFace);
					break;
				}
				case 'S': {
					string[] temp1 = receivedMessage.Split('\n');
					int snapFace = System.Convert.ToInt32(temp1[1]);
					int alignFace = System.Convert.ToInt32(temp1[2]);
					objectController.updateHighlight(snapFace, alignFace);
					break;
				}
				case 'F': {
					string[] temp1 = receivedMessage.Split('\n');
					if (temp1[1][0] == 'O') {
						faceTracker.useOrtho = true;
					}
					else {
						faceTracker.useOrtho = false;
						string[] temp2 = temp1[1].Split(',');
						faceTracker.observeOther =
							new Vector3(
								System.Convert.ToSingle(temp2[0]),
								System.Convert.ToSingle(temp2[1]),
								System.Convert.ToSingle(temp2[2])
							);
					}
					break;
				}
				case 'T':
					objectController.updateTransform(receivedMessage);
					break;
				case 'G': {
					string[] temp1 = receivedMessage.Split('\n');
					gridController.isFixed = temp1[1][0] == 'T';
					break;
				}
				case 'E': {
					string[] temp1 = receivedMessage.Split('\n');
					extrudeHandle.updateDist(System.Convert.ToSingle(temp1[1]), (temp1[0].Length > 7));
					break;
				}
			}
		}
		catch (Exception e) {
			if (receivedMessage[0] == 'M') {
				sendMessage("RM\n");
			}
			if (receivedMessage[0] == 'T') {
				sendMessage("RT\n");
			}
			Debug.Log(receivedMessage + "\n" + Time.deltaTime + "\n" + e.Message);
		}
	}

	public void snap() {
		sendMessage("Snap\n");
	}

	public void connect() {
		string address = IPInput.GetComponent<InputField>().text;
		ConnectToTcpServer(address);
	}


}