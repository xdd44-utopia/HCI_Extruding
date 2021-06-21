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
	public GameObject sliderController;
	public GameObject touchProcessor;
	public Camera renderCamera;

	private Color disconnectColor = new Color(0.8156f, 0.3529f, 0.4313f);
	//private Color connectColor = new Color(0.5254f, 0.7568f, 0.4f);
	private Color connectColor = new Color(0f, 0f, 0f);

	private TcpListener tcpListener;
	private Thread tcpListenerThread;
	private TcpClient connectedTcpClient;
	private string receivedMessage;
	private string rcvBuffer = "";
	private const int msgTypes = 6;
	private string[] sendBuffer = new string[msgTypes];
	private bool refreshed = false;
	
	private float sendTimer = 0;
	private const float sendInterval = 0.04f;

	private bool isConnected = false;
	
	void Start () {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests));
		tcpListenerThread.IsBackground = true;
		tcpListenerThread.Start();
		cleanSendBuffer();
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
			if (receivedMessage[0] == 'A') {
				rcvText.text = receivedMessage;
			}
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
			case 'T': pointer = 1; break;
			case 'H': pointer = 2; break;
			case 'F': pointer = 3; break;
			case 'S': pointer = 4; break;
			case 'A': pointer = 5; break;
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
		if (connectedTcpClient == null || !hasNewMsg) {
			return;
		}
		try {			
			NetworkStream stream = connectedTcpClient.GetStream();
				if (stream.CanWrite) {
					byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes("?" + msg + "!");
					stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
					Debug.Log("Server sent his message - should be received by client");
					cleanSendBuffer();
				}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
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
		string[] receivedMessageSplit = receivedMessage.Split('@');
		for (int i=0;i<receivedMessageSplit.Length;i++) {
			if (receivedMessageSplit[i].Length > 0) {
				try {
					switch (receivedMessageSplit[i][0]) {
						case 'T':
							string[] temp1 = receivedMessageSplit[i].Split('\n');
							int touchCount = System.Convert.ToInt32(temp1[1]);
							Vector3[] touchPos = new Vector3[touchCount];
							Vector3[] touchPrevPos = new Vector3[touchCount];
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
								touchProcessor.GetComponent<TouchProcessor>().updateTouchPoint(touchCount, touchPos, touchPrevPos);
							}
							
							break;
						case 'E':
							string extrudeDistStr = receivedMessageSplit[i].Split('\n')[1];
							meshManipulator.GetComponent<MeshManipulator>().updateExtrude(System.Convert.ToSingle(extrudeDistStr));
							break;
						case 'H':
							isConnected = true;
							break;
						case 'A':
							temp1 = receivedMessageSplit[i].Split('\n');
							string[] temp2 = temp1[1].Split(',');
							Vector3 acc =
								new Vector3(
									System.Convert.ToSingle(temp2[0]),
									System.Convert.ToSingle(temp2[1]),
									System.Convert.ToSingle(temp2[2])
								);
							sliderController.GetComponent<SliderController>().acceOther = acc;
							break;
						case 'C':
							temp1 = receivedMessageSplit[i].Split('\n');
							string[] temp3 = temp1[1].Split(',');
							Vector3 camPos =
								new Vector3(
									System.Convert.ToSingle(temp3[0]),
									System.Convert.ToSingle(temp3[1]),
									System.Convert.ToSingle(temp3[2])
								);
							// meshManipulator.GetComponent<MeshManipulator>().camOther = camPos;
							break;
					}
				}
				catch (Exception e) {
					errorText.text = receivedMessageSplit[i] + "\n" + e.Message;
				}
			}
		}
	}

	private void cleanSendBuffer() {
		for (int i=0;i<msgTypes;i++) {
			sendBuffer[i] = "";
		}
	}

}