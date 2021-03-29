using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ClientController : MonoBehaviour {

	public GameObject obj;
	public GameObject touchProcessor;
	public GameObject faceTracker;
	public Camera renderCamera;

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
	
	void Start () {
		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
		socketConnection = null;
	}
	
	void Update () {
		renderCamera.backgroundColor = (socketConnection == null ? disconnectColor : connectColor);
		if (refreshed) {
			getVector();
			refreshed = false;
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
						receivedMessage = Encoding.ASCII.GetString(incommingData);
						refreshed = true;
					}
				}
			}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}
	
	public void sendMessage() {
		Vector3 tp = touchProcessor.GetComponent<TouchProcessor>().pos;
		tp = convertToServer(tp);
		string clientMessage =
			(touchProcessor.GetComponent<TouchProcessor>().isPanning ? "T" : "F") + 
			(touchProcessor.GetComponent<TouchProcessor>().isRotating ? "T" : "F") +
			tp.x + "," +
			tp.y + "," +
			tp.z + "," +
			(-touchProcessor.GetComponent<TouchProcessor>().rot) + "," +
			touchProcessor.GetComponent<TouchProcessor>().verticalScale + ","
		;
		Debug.Log(clientMessage);
		if (socketConnection == null) {
			return;
		}
		try {		
			NetworkStream stream = socketConnection.GetStream();
				if (stream.CanWrite) {
					byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
					stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
					Debug.Log("Client sent his message - should be received by server");
				}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(Mathf.PI - angle) / 2, 0, - camWidth * Mathf.Sin(Mathf.PI - angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(Mathf.PI - angle), 0, - Mathf.Sin(Mathf.PI - angle));
		Vector3 z = new Vector3(Mathf.Cos(angle - Mathf.PI / 2), 0, Mathf.Sin(angle - Mathf.PI / 2));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private Vector3 convertToServer(Vector3 v) {
		Vector3 origin = new Vector3(- camWidth / 2 - camWidth * Mathf.Cos(Mathf.PI - angle) / 2, 0, - camWidth * Mathf.Sin(Mathf.PI - angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(Mathf.PI - angle), 0, Mathf.Sin(Mathf.PI - angle));
		Vector3 z = new Vector3(-Mathf.Cos(angle - Mathf.PI / 2), 0, Mathf.Sin(angle - Mathf.PI / 2));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private void getVector() {
		string[] temp = receivedMessage.Split(',');
		faceTracker.GetComponent<FaceTracker>().currentObserve =
			convertFromServer(new Vector3(
				System.Convert.ToSingle(temp[0]),
				System.Convert.ToSingle(temp[1]),
				System.Convert.ToSingle(temp[2])
			));
		if (!touchProcessor.GetComponent<TouchProcessor>().isLocked) {
			touchProcessor.GetComponent<TouchProcessor>().pos =
				convertFromServer(new Vector3(
					System.Convert.ToSingle(temp[3]),
					System.Convert.ToSingle(temp[4]),
					System.Convert.ToSingle(temp[5])
				));
			touchProcessor.GetComponent<TouchProcessor>().planarScale = System.Convert.ToSingle(temp[6]);
			touchProcessor.GetComponent<TouchProcessor>().verticalScale = System.Convert.ToSingle(temp[7]);
			touchProcessor.GetComponent<TouchProcessor>().rot = System.Convert.ToSingle(temp[8]);
		}
	}

	public void connect() {
		string address = "144.214.113.33";
		//Macbook local connecting to iPhone hotspot: 172.20.10.2
		//Samsung connecting to iPhone hotspot: 172.20.10.6
		//Samsung connecting to xdd44's wifi: 192.168.0.106
		//Macbook local connecting to xdd44's wifi: 192.168.0.101
		//iPhone connecting to iPhone hotspot: 10.150.153.190
		ConnectToTcpServer(address);
	}

}