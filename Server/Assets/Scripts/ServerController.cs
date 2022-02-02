using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ServerController : MonoBehaviour {

	public Text rcvText;
	public Text errorText;
	public Text ipText;
	public GameObject meshManipulator;
	public GameObject objectController;
	public GameObject sliderController;
	public GameObject touchProcessor;
	public GameObject faceTracker;
	public Camera renderCamera;

	private Color disconnectColor = new Color(0.8156f, 0.3529f, 0.4313f);
	//private Color connectColor = new Color(0.5254f, 0.7568f, 0.4f);
	private Color connectColor = new Color(0f, 0f, 0f);

	private TcpListener tcpListener;
	private Thread tcpListenerThread;
	private TcpClient connectedTcpClient;
	private string receivedMessage;
	private string rcvBuffer = "";
	private const int msgTypes = 10;
	private string[] sendBuffer = new string[msgTypes];
	private bool refreshed = false;
	
	private float sendTimer = 0;
	private const float sendInterval = 0.2f;

	
	void Start () {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests));
		tcpListenerThread.IsBackground = true;
		tcpListenerThread.Start();
		for (int i=0;i<msgTypes;i++) {
			sendBuffer[i] = "";
		}
	}
	
	void Update () {
		ipText.text = getIPAddress();
		renderCamera.backgroundColor = (connectedTcpClient == null ? disconnectColor : connectColor);
		
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
			refreshed = false;
			getVector();
		}

		sendTimer += Time.deltaTime;
		if (sendTimer > sendInterval) {
			sendMsgInBuffer();
			sendTimer = 0;
		}
	}
	
	private void ListenForIncommingRequests () {
		try {
			tcpListener = new TcpListener(IPAddress.Any, 8052);
			tcpListener.Start();
			Debug.Log("Server is listening");
			Byte[] bytes = new Byte[1024];
			while (true) {
				using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
					using (NetworkStream stream = connectedTcpClient.GetStream()) {
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
		}
		catch (SocketException socketException) {
			Debug.Log("SocketException " + socketException.ToString());
		}
	}

	public void sendMessage(string msg) {
		int pointer = -1;
		switch (msg[0]) {
			case 'M': pointer = 0; break;
			case 'A': pointer = 1; break;
			case 'H': pointer = 2; break;
			case 'F': pointer = 3; break;
			case 'S': pointer = 4; break;
			case 'T': pointer = 5; break;
			case 'C': pointer = 6; break;
			case 'G': pointer = 7; break;
			case 'E': pointer = 8; break;
			case 'D': pointer = 9; errorText.text = msg; break;
		}
		sendBuffer[pointer] = msg;
	}
	public void sendMsgInBuffer() {
		if (connectedTcpClient == null) {
			return;
		}
		for (int i=0;i<msgTypes;i++) {
			if (sendBuffer[i].Length > 0) {
				string msg = sendBuffer[i];
				try {			
					NetworkStream stream = connectedTcpClient.GetStream();
						if (stream.CanWrite) {
							byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
							stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
							Debug.Log("Server sent his message： " + "?" + msg + "!");
							sendBuffer[i] = "";
						}
				}
				catch (SocketException socketException) {
					Debug.Log("Socket exception: " + socketException);
				}
			}
		}
	}
	
	private string getIPAddress() {
		var host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (var ip in host.AddressList) {
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				return ip.ToString();
			}
		}
		throw new System.Exception("No network adapters with an IPv4 address in the system!");
	}

	private void getVector() {
		Debug.Log(receivedMessage);
		try {
			switch (receivedMessage[0]) {
				case 'T': {
					string[] temp1 = receivedMessage.Split('\n');
					int touchCount = System.Convert.ToInt32(temp1[1]);
					Vector3[] touchPos = new Vector3[touchCount];
					Vector3[] touchPrevPos = new Vector3[touchCount];
					TouchPhase[] phases = new TouchPhase[touchCount];
					for (int j=0;j<touchCount;j++) {
						string[] posStr = temp1[j+2].Split(',');
						touchPos[j] = new Vector3(
							System.Convert.ToSingle(posStr[0]),
							System.Convert.ToSingle(posStr[1]),
							System.Convert.ToSingle(posStr[2])
						);
						touchPrevPos[j] = new Vector3(
							System.Convert.ToSingle(posStr[3]),
							System.Convert.ToSingle(posStr[4]),
							System.Convert.ToSingle(posStr[5])
						);
					}
					for (int j=touchCount;j<2*touchCount;j++) {
						switch (temp1[j+2][0]) {
							case 'B':
								phases[j-touchCount] = TouchPhase.Began;
								break;
							case 'M':
								phases[j-touchCount] = TouchPhase.Moved;
								break;
							case 'S':
								phases[j-touchCount] = TouchPhase.Stationary;
								break;
							case 'E':
								phases[j-touchCount] = TouchPhase.Ended;
								break;
							case 'C':
								phases[j-touchCount] = TouchPhase.Canceled;
								break;
						}
					}
					touchProcessor.GetComponent<TouchProcessor>().updateTouchPoint(touchCount, touchPos, touchPrevPos, phases);
					break;
				}
				case 'H': {
					break;
				}
				case 'A': {
					string[] temp1 = receivedMessage.Split('\n');
					string[] temp2 = temp1[1].Split(',');
					Vector3 acc =
						new Vector3(
							System.Convert.ToSingle(temp2[0]),
							System.Convert.ToSingle(temp2[1]),
							System.Convert.ToSingle(temp2[2])
						);
					sliderController.GetComponent<SliderController>().acceOther = acc;
					break;
				}
				case 'F': {
					string[] temp1 = receivedMessage.Split('\n');
					if (temp1[1][0] == 'X') {
						faceTracker.GetComponent<FaceTracker>().faceOther = Vector3.zero;
					}
					else {
						string[] temp2 = temp1[1].Split(',');
						faceTracker.GetComponent<FaceTracker>().faceOther =
							new Vector3(
								System.Convert.ToSingle(temp2[0]),
								System.Convert.ToSingle(temp2[1]),
								System.Convert.ToSingle(temp2[2])
							);
					}
					break;
				}
				case 'S': {
					meshManipulator.GetComponent<MeshManipulator>().startNewFocusOtherScreen();
					break;
				}
				case 'E': {
					if (receivedMessage[7] == 'c') {
						meshManipulator.GetComponent<MeshManipulator>().enableCuttingPlaneOtherScreen();
					}
					else if (receivedMessage[8] == 'c') {
						meshManipulator.GetComponent<MeshManipulator>().executeCuttingPlaneOtherScreen();
					}
					else if (receivedMessage[7] == 'd') {
						meshManipulator.GetComponent<MeshManipulator>().enableDrillSimulation();
					}
					break;
				}
				case 'D': {

					meshManipulator.GetComponent<MeshManipulator>().exitDrillSimulation();
					break;
				}
				case 'R': {
					if (receivedMessage[1] == 'M') {
						objectController.GetComponent<ObjectController>().isMeshUpdated = true;
					}
					else if (receivedMessage[1] == 'T') {
						objectController.GetComponent<ObjectController>().isTransformUpdated = true;
					}
					break;
				}
			}
		}
		catch (Exception e) {
			errorText.text = receivedMessage + "\n" + e.Message;
		}
	}

}