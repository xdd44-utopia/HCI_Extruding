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

	private FaceTracker faceTracker;
	private ObjectController objectController;
	private GameObject drilledObject;
	private SliceTraceVisualizer sliceTraceVisualizer;
	private SliderController sliderController;
	private GameObject gridController;
	private GameObject depthFrame;
	private ExtrudeHandle extrudeHandle;
	private GameObject connectButton;
	private GameObject IPInput;


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
	private const int msgTypes = 9;
	private string[] sendBuffer = new string[msgTypes];

	private bool isConnected = false;

	private float sendTimer = 0;
	private const float sendInterval = 0.1f;
	private Vector3 accPrev = Vector3.zero;
	
	void Start () {
		faceTracker = GameObject.Find("FaceTracker").GetComponent<FaceTracker>();
		objectController = GameObject.Find("OBJECT").GetComponent<ObjectController>();
		drilledObject = GameObject.Find("DRILLED");
		sliceTraceVisualizer = GameObject.Find("Slice Trace").GetComponent<SliceTraceVisualizer>();
		sliderController = GameObject.Find("SliderController").GetComponent<SliderController>();
		gridController = GameObject.Find("RulerGrid");
		depthFrame = GameObject.Find("Depth");
		extrudeHandle = GameObject.Find("ExtrudeHandleController").GetComponent<ExtrudeHandle>();
		connectButton = GameObject.Find("Connect");
		IPInput = GameObject.Find("IPInput");

		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
		socketConnection = null;
		for (int i=0;i<msgTypes;i++) {
			sendBuffer[i] = "";
		}
	}
	
	void Update () {
		angle = sliderController.angle;
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
		if (Vector3.Distance(accPrev, accConverted) > 0.01f) {
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
			case 'H': pointer = 0; break;
			case 'T': pointer = 1; break;
			case 'A': pointer = 2; break;
			case 'F': pointer = 3; break;
			case 'S': pointer = 4; break;
			default:
				if (msg[0] == 'E' && msg[7] == 'c')
					pointer = 5;
				if (msg[0] == 'E' && msg[8] == 'c')
					pointer = 6;
				if (msg[0] == 'E' && msg[7] == 'd')
					pointer = 7;
				if (msg[0] == 'D' && msg[8] == 'd')
					pointer = 8;
				break;
		}
		sendBuffer[pointer] = msg;
	}
	public void sendMsgInBuffer() {
		if (socketConnection == null) {
			return;
		}
		for (int i=0;i<msgTypes;i++) {
			if (sendBuffer[i].Length > 0) {
				string msg = sendBuffer[i];
				try {		
					NetworkStream stream = socketConnection.GetStream();
						if (stream.CanWrite) {
							byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
							stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
							Debug.Log("Client sent his message： " + "?" + msg + "!");
							sendBuffer[i] = "";
						}
				}
				catch (SocketException socketException) {
					Debug.Log("Socket exception: " + socketException);
				}
			}
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

		connectButton.SetActive(false);
		IPInput.SetActive(false);

		Debug.Log(receivedMessage);
		try {
			switch (receivedMessage[0]) {
				case 'F':
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
				case 'S':
					string[] tempSlice = receivedMessage.Split('\n');
					int count = System.Convert.ToInt32(tempSlice[1]);
					Vector3[] vertices = new Vector3[count];
					for (int j=0;j<count;j++) {
						string[] tempVertex = tempSlice[2 + j].Split(',');
						vertices[j] = convertFromServer(new Vector3(
							System.Convert.ToSingle(tempVertex[0]),
							System.Convert.ToSingle(tempVertex[1]),
							System.Convert.ToSingle(tempVertex[2])
						));
					}
					sliceTraceVisualizer.updateTrace(vertices);
					break;
				case 'C':
					string[] tempPlane = receivedMessage.Split('\n');
					string[] tempEndThisScreen = tempPlane[1].Split(',');
					string[] tempEndOtherScreen = tempPlane[2].Split(',');
					string[] tempStartThisScreen = tempPlane[3].Split(',');
					string[] tempStartOtherScreen = tempPlane[4].Split(',');
					Vector3 touchPointThisScreen = convertFromServer(new Vector3(
						System.Convert.ToSingle(tempEndThisScreen[0]),
						System.Convert.ToSingle(tempEndThisScreen[1]),
						System.Convert.ToSingle(tempEndThisScreen[2])
					));
					Vector3 touchPointOtherScreen = convertFromServer(new Vector3(
						System.Convert.ToSingle(tempEndOtherScreen[0]),
						System.Convert.ToSingle(tempEndOtherScreen[1]),
						System.Convert.ToSingle(tempEndOtherScreen[2])
					));
					Vector3 touchStartThisScreen = convertFromServer(new Vector3(
						System.Convert.ToSingle(tempStartThisScreen[0]),
						System.Convert.ToSingle(tempStartThisScreen[1]),
						System.Convert.ToSingle(tempStartThisScreen[2])
					));
					Vector3 touchStartOtherScreen = convertFromServer(new Vector3(
						System.Convert.ToSingle(tempStartOtherScreen[0]),
						System.Convert.ToSingle(tempStartOtherScreen[1]),
						System.Convert.ToSingle(tempStartOtherScreen[2])
					));
					sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().updateCuttingPlane(touchPointThisScreen, touchPointOtherScreen, touchStartThisScreen, touchStartOtherScreen);
					break;
				case 'M':
					objectController.updateMesh(receivedMessage);
					break;
				case 'T':
					objectController.updateTransform(receivedMessage);
					break;
				case 'H':
					objectController.updateHighlight(receivedMessage);
					break;
				case 'A':
					temp1 = receivedMessage.Split('\n');
					sliderController.angle = System.Convert.ToSingle(temp1[1]);
					break;
				case 'G':
					temp1 = receivedMessage.Split('\n');
					gridController.SetActive(temp1[1][0] == 'T');
					depthFrame.SetActive(temp1[1][0] == 'F');
					break;
				case 'E':
					temp1 = receivedMessage.Split('\n');
					extrudeHandle.updateDist(System.Convert.ToSingle(temp1[1]));
					break;
				case 'D':
					temp1 = receivedMessage.Split('\n');
					if (temp1[1][0] == 'X') {
						disableDrillSimulation();
					}
					else {
						string[] tempOffset = temp1[1].Split(',');
						string[] tempDir = temp1[2].Split(',');
						Vector3 offset = new Vector3(
							System.Convert.ToSingle(tempOffset[0]),
							System.Convert.ToSingle(tempOffset[1]),
							System.Convert.ToSingle(tempOffset[2])
						);
						Vector3 dir = new Vector3(
							System.Convert.ToSingle(tempDir[0]),
							System.Convert.ToSingle(tempDir[1]),
							System.Convert.ToSingle(tempDir[2])
						);
						debugText.text = receivedMessage + "\n" + dir.x + ", " + dir.y + ", " + dir.z;
						if (drilledObject == null) {
							drilledObject = GameObject.Find("DRILLED");
						}
						drilledObject.GetComponent<DrilledObjectController>().offset = offset;
						drilledObject.GetComponent<DrilledObjectController>().dir = dir;
					}
					break;
			}
		}
		catch (Exception e) {
			// if (receivedMessage[0] == 'M') {
			// 	sendMessage("RM\n");
			// }
			// if (receivedMessage[0] == 'T') {
			// 	sendMessage("RT\n");
			// }
			errorText.text = receivedMessage + "\n" + Time.deltaTime + "\n" + e.Message;
		}
	}
	private void disableDrillSimulation() {
		GameObject.Find("OBJECT").GetComponent<MeshRenderer>().enabled = true;
		GameObject.Find("Inside").GetComponent<MeshRenderer>().enabled = true;
		GameObject[] faces = GameObject.FindGameObjectsWithTag("FaceObj");
		foreach (GameObject face in faces) {
			face.GetComponent<MeshRenderer>().enabled = true;
		}
		drilledObject.SetActive(false);
	}

	public void snap() {
		sendMessage("Snap\n");
	}

	public void connect() {
		string address = IPInput.GetComponent<InputField>().text;
		ConnectToTcpServer(address);
	}


}