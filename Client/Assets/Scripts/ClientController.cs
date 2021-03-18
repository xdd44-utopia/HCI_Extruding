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
	
	void Start () {
		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
		socketConnection = null;
	}
	
	void Update () {
		renderCamera.backgroundColor = (socketConnection == null ? disconnectColor : connectColor);
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
				using (NetworkStream stream = socketConnection.GetStream()) {
					int length;
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
						var incommingData = new byte[length];
						Array.Copy(bytes, 0, incommingData, 0, length);
						string serverMessage = Encoding.ASCII.GetString(incommingData);
						getVector(serverMessage);
					}
				}
			}
		}
		catch (SocketException socketException) {
			Debug.Log("Socket exception: " + socketException);
		}
	}
	
	public void sendMessage() {
		if (socketConnection == null) {
			return;
		}
		try {		
			NetworkStream stream = socketConnection.GetStream();
			if (stream.CanWrite) {
				Vector3 tp = touchProcessor.GetComponent<TouchProcessor>().pos;
				tp = convertToServer(tp);
				Quaternion tq = touchProcessor.GetComponent<TouchProcessor>().rot;
				tq = convertRotToServer(tq);
				string clientMessage =
					tp.x + "," +
					tp.y + "," +
					tp.z + "," +
					tq.x + "," +
					tq.y + "," +
					tq.z + "," +
					tq.w + "," +
					touchProcessor.GetComponent<TouchProcessor>().verticalScale + ","
				;
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

	private Quaternion convertRotFromServer(Quaternion q) {
		Vector3 axisWorld = new Vector3(0, 1, 0);
		axisWorld = Quaternion.Inverse(q) * axisWorld;
		Quaternion rotChange = new Quaternion(
			axisWorld.x * Mathf.Sin(-angle/2),
			axisWorld.y * Mathf.Sin(-angle/2),
			axisWorld.z * Mathf.Sin(-angle/2),
			Mathf.Cos(-angle/2)
		);
		q *= rotChange;
		return q;
	}

	private Quaternion convertRotToServer(Quaternion q) {
		Vector3 axisWorld = new Vector3(0, 1, 0);
		axisWorld = Quaternion.Inverse(q) * axisWorld;
		Quaternion rotChange = new Quaternion(
			axisWorld.x * Mathf.Sin(angle/2),
			axisWorld.y * Mathf.Sin(angle/2),
			axisWorld.z * Mathf.Sin(angle/2),
			Mathf.Cos(angle/2)
		);
		q *= rotChange;
		return q;
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private void getVector(string str) {
		string[] temp = str.Split(',');
		faceTracker.GetComponent<FaceTracker>().currentObserve =
			convertFromServer(new Vector3(
				System.Convert.ToSingle(temp[0]),
				System.Convert.ToSingle(temp[1]),
				System.Convert.ToSingle(temp[2])
			));
		touchProcessor.GetComponent<TouchProcessor>().pos =
			convertFromServer(new Vector3(
				System.Convert.ToSingle(temp[3]),
				System.Convert.ToSingle(temp[4]),
				System.Convert.ToSingle(temp[5])
			));
		touchProcessor.GetComponent<TouchProcessor>().rot =
			convertRotFromServer(new Quaternion(
				System.Convert.ToSingle(temp[6]),
				System.Convert.ToSingle(temp[7]),
				System.Convert.ToSingle(temp[8]),
				System.Convert.ToSingle(temp[9])
			));
		touchProcessor.GetComponent<TouchProcessor>().planarScale = System.Convert.ToSingle(temp[10]);
	}

}