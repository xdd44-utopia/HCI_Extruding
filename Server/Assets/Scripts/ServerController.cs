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

	public Text ipText;
	public Text rcvText;
	public GameObject obj;
	public GameObject touchProcessor;
	public GameObject faceTracker;
	public GameObject panVisualizer;
	public GameObject rotateVisualizer;
	public Camera renderCamera;

	private Color disconnectColor = new Color(0.8156f, 0.3529f, 0.4313f);
	//private Color connectColor = new Color(0.5254f, 0.7568f, 0.4f);
	private Color connectColor = new Color(0f, 0f, 0f);

	private TcpListener tcpListener;
	private Thread tcpListenerThread;
	private TcpClient connectedTcpClient;
	private string rcvMsg = "";
	private bool refreshed = false;

	private bool noConnection = true;
	
	void Start () {
		tcpListenerThread = new Thread (new ThreadStart(ListenForIncommingRequests));
		tcpListenerThread.IsBackground = true;
		tcpListenerThread.Start();
	}
	
	void Update () {
		ipText.text = getIPAddress();
		rcvText.text = rcvMsg;
		renderCamera.backgroundColor = (connectedTcpClient == null ? disconnectColor : connectColor);
		if (connectedTcpClient != null && noConnection) {
			sendMessage();
			noConnection = false;
		}
		if (refreshed) {
			getVector();
			refreshed = false;
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
							string clientMessage = Encoding.ASCII.GetString(incommingData);
							rcvMsg = clientMessage;
							refreshed = true;
						}
					}
				}
			}
		}
		catch (SocketException socketException) {
			Debug.Log("SocketException " + socketException.ToString());
		}
	}
	
	public void sendMessage() {
		string serverMessage =
			faceTracker.GetComponent<FaceTracker>().currentObserve.x + "," +
			faceTracker.GetComponent<FaceTracker>().currentObserve.y + "," +
			faceTracker.GetComponent<FaceTracker>().currentObserve.z + ","
		;
		if (connectedTcpClient == null) {
			return;
		}
		
		try {			
			NetworkStream stream = connectedTcpClient.GetStream();
				if (stream.CanWrite) {
					byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
					stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
					Debug.Log("Server sent his message - should be received by client");
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
		// if (!touchProcessor.GetComponent<TouchProcessor>().isLocked) {
		// 	if (rcvMsg[0] == 'T') {
		// 		panVisualizer.GetComponent<PanVisualizer>().pan();
		// 	}
		// 	rotateVisualizer.GetComponent<RotateVisualizer>().isRotating = (rcvMsg[1] == 'T');
		// 	rcvMsg = rcvMsg.Substring(2);
		// 	string[] temp = rcvMsg.Split(',');
		// 	touchProcessor.GetComponent<TouchProcessor>().pos =
		// 		new Vector3(
		// 			System.Convert.ToSingle(temp[0]),
		// 			System.Convert.ToSingle(temp[1]),
		// 			System.Convert.ToSingle(temp[2])
		// 		);
		// 	touchProcessor.GetComponent<TouchProcessor>().rot = System.Convert.ToSingle(temp[3]);
		// }
	}

}