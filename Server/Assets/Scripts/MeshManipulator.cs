using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MeshManipulator : MonoBehaviour
{
	public Text modeText;
	public Text focusText;
	public Text debugText;
	public Text debugText2;
	public Image measureButton;
	public Sprite measureRecoverSprite;
	public Sprite measureRecoveredSprite;
	private GameObject sliceTraceVisualizer;
	private GameObject sliderController;
	private ServerController sender;
	private GameObject extrudeHandle;
	private GameObject gridController;
	//interaction
	[HideInInspector]
	public Vector3 touchPosition;
	private Vector3 prevTouchPosition;
	private float touchDelayTolerance = 0.1f;
	private Vector3 INF = new Vector3(10000, 10000, 10000);
	private float prevAngle = 0;

	//hit
	private ObjectController obj;
	public Mesh defaultMesh;
	private RaycastHit hit;
	private Vector3[] vertices;
	private int[] triangles;

	//Focus
	[HideInInspector]
	private Vector3 centerToHitFace;
	private bool focusingThisScreen = false;
	private bool focusingOtherScreen = false;
	private bool isThisScreenCuttingPlane = false;
	private bool isOtherScreenPenetrate = false;
	private Vector3 footDrop;


	//selection
	[HideInInspector]
	public List<int> selectEdgeVertices;
	private List<int> prevEdgeVertices;
	[HideInInspector]
	public List<int> selectTriangles;
	[HideInInspector]
	public Vector3 focusNormal;
	
	//focus
	private int focusTriangleIndex = -1;
	private int prevAngleFitTriangle = -1;
	private Vector3 axisToFocus;
	private float angleToFocus;
	private Vector3 posToFocus;
	private const float focusSpeed = 25;

	//extrude
	private Mesh extrudedMesh;
	private Vector3 extrudeDir = new Vector3(0, 0, 0);
	private Vector3 extrudeStartPos = new Vector3(0, 0, 0);
	private float extrudeDist = 0f;
	private Vector3[] extrudedVertices;
	private Vector3[] extrudedVerticesOriginal;
	private int[] extrudedTriangles;
	private float extrudeTimer = 0;

	//drill
	public float drillDist = 0f;

	//taper
	private Mesh taperedMesh;
	private Vector3[] taperedVertices;
	private int[] taperedTriangles;
	private Vector3 taperCenter;
	private float taperScale;
	private float taperTimer = 0;
	private bool taperStarted = false;

	//slice
	private List<Vector3> remainVerticesList;
	private List<Vector3> rightVerticesList;
	private List<Vector3> edgeVerticesList;
	private List<Vector3> cuttingPlaneVerticesList;
	public MeshRenderer cuttingPlaneRenderer;

	//Undo
	private Vector3[] undoVertices;
	private Vector2[] undoUV;
	private int[] undoTriangles;
	private bool undoAvailable = false;
	private Vector3 undoPos;
	private Vector3 undoScale;
	private Quaternion undoRot;

	//Object status
	private bool isThisScreenFocused = false;
	private bool isOtherScreenFocused = false;
	private bool isEdgeAligned = false;

	//Edge align
	private int closestVertex = -1;
	private int secondVertex = -1;

	private Status state = Status.select;
	private enum Status {
		select,
		focus,
		extrude,
		taper,
		drill
	}

	private SelectMode smode = SelectMode.selectObject;
	private enum SelectMode {
		selectFace,
		selectObject
	}

	/* #region Main */
	void Start()
	{
		GameObject findObject;
		findObject = GameObject.Find("Server");
		if (findObject != null) {
			sender = findObject.GetComponent<ServerController>();
		}

		sliceTraceVisualizer = GameObject.Find("Slice Trace");
		sliderController = GameObject.Find("SliderController");
		extrudeHandle = GameObject.Find("Extrude");
		gridController = GameObject.Find("RulerGrid");

		obj = gameObject.GetComponent<ObjectController>();

		touchPosition = INF;

		cuttingPlaneRenderer.enabled = false;

	
	}

	void Update() {

		measureButton.sprite = (obj.isRealMeasure ? measureRecoveredSprite : measureRecoverSprite);
		
		vertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
		triangles = gameObject.GetComponent<MeshFilter>().mesh.triangles;
		
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				string msg = "Extrude\n" + extrudeDist;
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			else {
				extrudeHandle.SetActive(isOtherScreenFocused);
				extrudeHandle.GetComponent<RectTransform>().anchoredPosition = new Vector2(360 - extrudeDist / VectorCalculator.camWidth * 772, 0);
			}
		}
		else {
			extrudeHandle.SetActive(false);
		}

		if (isThisScreenFocused) {
			focusText.text = "Snapped on this screen";
		}
		else if (isOtherScreenFocused) {
			focusText.text = "Snapped on the other screen";
		}
		else {
			focusText.text = "No snapping";
		}
		if (isEdgeAligned) {
			focusText.text += "\nEdge snapped";
		}

		if (Mathf.Abs(VectorCalculator.angle - prevAngle) > 0.005f) {
			obj.updateTransform();
			if (isOtherScreenFocused) {
				startFocus(false, false);
			}
		}

		modeText.text = state + "";

		switch(state) {
			case Status.select:
				checkAngleFit();
				break;
			case Status.focus:
				focus();
				break;
			case Status.extrude:
				try{
					extrude();
				}
				catch (Exception e) {
					loadUndo();
					state = Status.select;
				}
				break;
			case Status.taper:
				taper();
				break;
		}

		prevAngle = VectorCalculator.angle;

		// if (Input.GetMouseButtonDown(0)) {
		// 	castRay();
		// }

	}
	/* #endregion */

	/* #region Construct highlight */
	public void castRay() {

		Vector3 mousePos = touchPosition;
		// mousePos = Input.mousePosition;
		// mousePos -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
		// mousePos *= Camera.main.orthographicSize / Screen.height * 2;

		Vector3 rayStart;
		Vector3 rayDirection;

		if (!Camera.main.orthographic) {
			rayStart = Camera.main.gameObject.transform.position;
			rayDirection = mousePos - rayStart;
		}
		else {
			rayStart = mousePos;
			if (Mathf.Abs(mousePos.z) < 0.01f) {
				rayDirection = new Vector3(0, 0, 5);
			}
			else {
				rayDirection = new Vector3(- 5 * Mathf.Cos(Mathf.PI / 2 + VectorCalculator.angle), 0, 5 * Mathf.Sin(Mathf.PI / 2 + VectorCalculator.angle));
			}
		}

		Debug.DrawLine(
			rayStart,
			rayStart + rayDirection * 10f,
			Color.yellow,
			100f,
			true
		);

		if (Physics.Raycast(rayStart, rayDirection, out hit)) {
			
			Debug.Log("Cast ray");

			prevEdgeVertices = new List<int>();
			obj.updateSelect(hit.triangleIndex);
			focusTriangleIndex = hit.triangleIndex;
			smode = SelectMode.selectFace;

		}
		
	}

	private void checkAngleFit() {
		int targetTriangle = -1;
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				for (int i=0;i<triangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(VectorCalculator.convertFromServer(transform.TransformPoint(vertices[triangles[i * 3 + j]])).z) < 0.05f) {
							cnt++;
						}
					}
					if (cnt == 3) {
						targetTriangle = i;
						break;
					}
				}
			}
			else if (isOtherScreenFocused) {
				for (int i=0;i<triangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(transform.TransformPoint(vertices[triangles[i * 3 + j]]).z) < 0.05f) {
							cnt++;
						}
					}
					if (cnt == 3) {
						targetTriangle = i;
						break;
					}
				}
			}
		}
		if (targetTriangle != prevAngleFitTriangle) {
			obj.updateHighlight(-2, targetTriangle);
			prevAngleFitTriangle = targetTriangle;
		}
	}
	/* #endregion */

	/* #region Undo */

	private void prepareUndo() {
		undoAvailable = true;
		undoVertices = new Vector3[vertices.Length];
		undoTriangles = new int[triangles.Length];
		for (int i=0;i<vertices.Length;i++) {
			undoVertices[i] = vertices[i];
		}
		for (int i=0;i<triangles.Length;i++) {
			undoTriangles[i] = triangles[i];
		}
		undoPos = transform.position;
		undoScale = transform.localScale;
		undoRot = transform.rotation;
	}

	public void loadUndo() {
		if (!undoAvailable) {
			return;
		}
		else {
			undoAvailable = false;
		}

		Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = undoVertices;
		mesh.triangles = undoTriangles;
		mesh.uv = new Vector2[undoVertices.Length];
		mesh.MarkModified();
		mesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		transform.position = undoPos;
		transform.localScale = undoScale;
		transform.rotation = undoRot;

		obj.updateTransform();
		obj.updateMesh(true);
	}

	/* #endregion */

	/* #region Extrude */
	
	private void prepareExtrude(bool isThisScreen) {

		prepareUndo();

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		
		extrudeDist = 0;
		extrudeStartPos = transform.position;
		if (isThisScreen) {
			extrudeDir = new Vector3(-1, 0, 0);
		}
		else {
			extrudeDir = new Vector3(Mathf.Cos(-VectorCalculator.angle), 0, Mathf.Sin(-VectorCalculator.angle));
		}

		extrudedMesh = new Mesh();
		extrudedMesh.vertices = new Vector3[vertices.Length + edgeLength];
		extrudedMesh.uv = new Vector2[vertices.Length + edgeLength];
		extrudedMesh.triangles = new int[triangles.Length + edgeLength * 6];
		
		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		//Original mesh
		for (int i=0;i<vertices.Length;i++) {
			extrudedVertices[i] = vertices[i];
		}
		//Copy edge
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[vertices.Length + i] = vertices[selectEdgeVertices[i]];
		}
		//Original triangles
		for (int i=0;i<triangles.Length;i++) {
			extrudedTriangles[i] = triangles[i];
		}
		//Reassign triangles of extruded face
		for (int i=0;i<selectTriangles.Count;i++) {
			for (int j=0;j<3;j++) {
				for (int k=0;k<edgeLength;k++) {
					if (Vector3.Distance(extrudedVertices[vertices.Length + k], extrudedVertices[extrudedTriangles[selectTriangles[i] * 3 + j]]) < 0.0001f) {
						extrudedTriangles[selectTriangles[i] * 3 + j] = vertices.Length + k;
					}
				}
			}
		}
		//Assign triangles from extrusion
		for (int i=0;i<edgeLength;i++) {
			extrudedTriangles[triangles.Length + i * 6 + 0] = selectEdgeVertices[i];
			extrudedTriangles[triangles.Length + i * 6 + 1] = selectEdgeVertices[(i+1)%edgeLength];
			extrudedTriangles[triangles.Length + i * 6 + 2] = vertices.Length + (i+1)%edgeLength;
			extrudedTriangles[triangles.Length + i * 6 + 3] = selectEdgeVertices[i];
			extrudedTriangles[triangles.Length + i * 6 + 4] = vertices.Length + (i+1)%edgeLength;
			extrudedTriangles[triangles.Length + i * 6 + 5] = vertices.Length + i;
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;

		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudedVerticesOriginal = new Vector3[extrudedVertices.Length];
		for (int i=0;i<vertices.Length + edgeLength;i++) {
			extrudedVerticesOriginal[i] = extrudedVertices[i];
		}
		state = Status.extrude;
		extrudeTimer = touchDelayTolerance;

	}

	public void updateExtrudeScale(float factor, bool isThisScreen) {
		factor /= transform.localScale.x;
		if (smode != SelectMode.selectFace) {
			return;
		}
		if (!isEdgeAligned) {
			return;
		}
		if (smode == SelectMode.selectFace && isEdgeAligned && state == Status.select) {
			if ((isThisScreenFocused && !isThisScreen) || (isOtherScreenFocused && isThisScreen)) {
				prepareExtrude(isThisScreen);
			}
		}
		if (state == Status.extrude) {
			extrudeDist += factor;
			extrudeDist = extrudeDist > 0 ? extrudeDist : 0;
			extrudeTimer = touchDelayTolerance;
			debugText2.text = factor + "";
		}
	}
	private void extrude() {

		if (smode != SelectMode.selectFace) {
			state = Status.select;
			return;
		}


		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		Vector3 localNormal = transform.InverseTransformPoint(transform.position - extrudeDir);

		localNormal = localNormal.normalized;
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[vertices.Length + i] = extrudedVerticesOriginal[vertices.Length + i] + localNormal * extrudeDist;
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;
		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudeDir = extrudeDir.normalized * transform.localScale.x;

		transform.position = extrudeStartPos + extrudeDir * extrudeDist;

		debugText2.text += " " + extrudeDir * extrudeDist;


		if (extrudeDist > 0.02f) {
			Mesh tempMesh = new Mesh();

			tempMesh.vertices = new Vector3[extrudedVertices.Length];
			tempMesh.uv = new Vector2[extrudedVertices.Length];
			tempMesh.triangles = new int[extrudedTriangles.Length];

			Vector3[] tempVertices = tempMesh.vertices;
			int[] tempTriangles = tempMesh.triangles;
			
			for (int i=0;i<extrudedVertices.Length;i++) {
				tempVertices[i] = extrudedVertices[i];
			}
			for (int i=0;i<extrudedTriangles.Length;i++) {
				tempTriangles[i] = extrudedTriangles[i];
			}
			tempMesh.vertices = tempVertices;
			tempMesh.triangles = tempTriangles;
			tempMesh.MarkModified();
			tempMesh.RecalculateNormals();

			gameObject.GetComponent<MeshFilter>().mesh = tempMesh;
			gameObject.GetComponent<MeshCollider>().sharedMesh = tempMesh;
			obj.updateTransform();
			obj.updateMesh(true);
		}

		extrudeTimer -= Time.deltaTime;
		if (extrudeTimer < 0) {
			state = Status.select;
			if (extrudeDist < 0.02f) {
				loadUndo();
			}
			extrudeDist = 0;
		}

	}
	/* #endregion */

	/* #region Taper */

	public void prepareTaper() {

		if (smode != SelectMode.selectFace) {
			return;
		}

		isEdgeAligned = false;
		closestVertex = -1;
		secondVertex = -1;

		taperStarted = false;

		prepareUndo();

		taperScale = 1;
		taperCenter = new Vector3(0, 0, 0);

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;

		taperedMesh = gameObject.GetComponent<MeshFilter>().mesh;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperCenter += vertices[triangles[selectTriangles[i] * 3 + j]];
			}
		}
		taperCenter /= faceNum * 3;

		state = Status.taper;

		taperTimer = touchDelayTolerance;
		
	}
	public void updateTaperScale(float factor) {

		if (state != Status.taper) {
			return;
		}
		
		taperScale += factor / 2.5f;
		taperScale = taperScale > 0 ? taperScale : 0;
		taperTimer = touchDelayTolerance;
		taperStarted = true;
	}
	private void taper() {

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		
		taperedVertices = taperedMesh.vertices;
		taperedTriangles = taperedMesh.triangles;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperedVertices[triangles[selectTriangles[i] * 3 + j]] = taperScale * (vertices[triangles[selectTriangles[i] * 3 + j]] - taperCenter) + taperCenter;
			}
		}

		debugText2.text = taperScale + "";

		for (int i=0;i<taperedVertices.Length;i++) {
			for (int j=0;j<edgeLength;j++) {
				if (vertices[selectEdgeVertices[j]] == vertices[i]) {
					taperedVertices[i] = taperScale * (vertices[selectEdgeVertices[j]] - taperCenter) + taperCenter;
				}
			}
		}

		taperedMesh.vertices = taperedVertices;
		taperedMesh.triangles = taperedTriangles;
		taperedMesh.MarkModified();
		taperedMesh.RecalculateNormals();

		gameObject.GetComponent<MeshFilter>().mesh = taperedMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
		obj.updateMesh(false);

		// taperScale = 1;

		taperTimer -= Time.deltaTime;
		if ((taperTimer < 0 && taperStarted) || taperScale == 0) {
			state = Status.select;
		}

	}
	/* #endregion */

	/* #region Split */
	public void enableCuttingPlaneOtherScreen() {
		isOtherScreenPenetrate = true;
		isEdgeAligned = false;
	}
	public void executeCuttingPlaneOtherScreen() {
		cut(false);
		isOtherScreenPenetrate = false;
	}
	private void enableCuttingPlaneThisScreen() {
		isThisScreenCuttingPlane = true;
		cuttingPlaneRenderer.enabled = true;
		isEdgeAligned = false;
	}
	private void executeCuttingPlaneThisScreen() {
		cut(true);
		isThisScreenCuttingPlane = false;
		cuttingPlaneRenderer.enabled = false;
	}
	public void cuttingPlaneSwitcher() {
		if (!isThisScreenCuttingPlane) {
			enableCuttingPlaneThisScreen();
		}
		else {
			executeCuttingPlaneThisScreen();
		}
	}
	public void cut(bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		GameObject slicePlane = GameObject.Find("SlicePlane");
		Vector3 planePos = isMainScreen ? new Vector3(0, 0, 0) : slicePlane.transform.position;
		Vector3 planeNormal = isMainScreen ? new Vector3(0, 0, -1) : slicePlane.transform.rotation * new Vector3(0, 1, 0);

		prepareUndo();

		// x ⋅ planePos - VectorCalculator.dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planePos = transform.InverseTransformPoint(planePos);
		planeNormal = transform.InverseTransformPoint(planeNormal + transform.position).normalized;

		Vector3 avoidZeroVector = new Vector3(Random.Range(0.001f, 0.002f), Random.Range(0.001f, 0.002f), Random.Range(0.001f, 0.002f));
		if (planeNormal.x != 0) {
			avoidZeroVector.x = - (avoidZeroVector.y * planeNormal.y + avoidZeroVector.z * planeNormal.z) / planeNormal.x;
		}
		else if (planeNormal.y != 0) {
			avoidZeroVector.y = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.z * planeNormal.z) / planeNormal.y;
		}
		else if (planeNormal.z != 0) {
			avoidZeroVector.z = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.y * planeNormal.y) / planeNormal.z;
		}
		planePos += avoidZeroVector;

		Debug.DrawLine(
			transform.position,
			transform.TransformPoint(planeNormal),
			Color.yellow,
			5000,
			false
		);

		remainVerticesList = new List<Vector3>();
		edgeVerticesList = new List<Vector3>();

		//Calculate edge of cutting plane & reconstruct faces along the edge
		int[] triangleSide = new int[triangles.Length / 3]; // -1 left, 0 cross, 1 right
		float pn = VectorCalculator.dotProduct(planePos, planeNormal);
		for (int i=0;i<triangles.Length / 3;i++) {
			float[] verticesPos = new float[3]{
				VectorCalculator.dotProduct(vertices[triangles[i * 3 + 0]], planeNormal) - pn,
				VectorCalculator.dotProduct(vertices[triangles[i * 3 + 1]], planeNormal) - pn,
				VectorCalculator.dotProduct(vertices[triangles[i * 3 + 2]], planeNormal) - pn
			};
			if (verticesPos[0] <= 0 && verticesPos[1] <= 0 && verticesPos[2] <= 0) {
				triangleSide[i] = -1;
				for (int j=0;j<3;j++) {
					remainVerticesList.Add(vertices[triangles[i * 3 + j]]);
				}
			}
			else if (verticesPos[0] <= 0 || verticesPos[1] <= 0 || verticesPos[2] <= 0) {
				triangleSide[i] = 0;
				for (int j=0;j<3;j++) {
					Vector3[] curVec = new Vector3[3]{
						vertices[triangles[i * 3 + j]],
						vertices[triangles[i * 3 + ((j + 1) % 3)]],
						vertices[triangles[i * 3 + ((j + 2) % 3)]]
					};
					if (verticesPos[j] <= 0 && verticesPos[(j + 1) % 3] <= 0) {
						Vector3 newVec1 = VectorCalculator.getLinePlaneIntersection(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = VectorCalculator.getLinePlaneIntersection(curVec[1], curVec[2], planePos, planeNormal);
						remainVerticesList.Add(curVec[0]);
						remainVerticesList.Add(curVec[1]);
						remainVerticesList.Add(newVec1);
						remainVerticesList.Add(curVec[1]);
						remainVerticesList.Add(newVec2);
						remainVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
					else if (verticesPos[j] > 0 && verticesPos[(j+1)%3] > 0) {
						Vector3 newVec1 = VectorCalculator.getLinePlaneIntersection(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = VectorCalculator.getLinePlaneIntersection(curVec[1], curVec[2], planePos, planeNormal);
						remainVerticesList.Add(curVec[2]);
						remainVerticesList.Add(newVec1);
						remainVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
				}
			}
		}

		if (edgeVerticesList.Count == 0) {
			return;
		}

		//Extract edge as successive vertices
		List<List<Vector3>> sortedEdgeVerticesList = new List<List<Vector3>>();
		bool[] used = new bool[edgeVerticesList.Count / 2];
		int boundaryCount = 0;

		while (true) {

			int curEdge = -1;
			Vector3 curStart = new Vector3(0, 0, 0);
			Vector3 curVect = new Vector3(0, 0, 0);
			for (int i=0;i<edgeVerticesList.Count / 2;i++) {
				if (!used[i]) {
					curEdge = i;
					curStart = edgeVerticesList[i * 2];
					curVect = edgeVerticesList[i * 2 + 1];
					used[i] = true;
					break;
				}
			}
			if (curEdge == -1) {
				break;
			}

			boundaryCount++;
			sortedEdgeVerticesList.Add(new List<Vector3>());
			sortedEdgeVerticesList[boundaryCount - 1].Add(curStart);

			bool done = false;
			do {
				for (int j=0;j<edgeVerticesList.Count / 2;j++) {
					if (curEdge != j) {
						for (int k=0;k<2;k++) {
							if (edgeVerticesList[j * 2 + k] == curVect) {
								sortedEdgeVerticesList[boundaryCount - 1].Add(edgeVerticesList[j * 2 + k]);
								curVect = edgeVerticesList[j * 2 + (1 - k)];
								if (curVect == curStart) {
									done = true;
								}
								curEdge = j;
								used[j] = true;
								break;
							}
						}
					}
					if (done) {
						break;
					}
				}
			} while (!done);

			//Simplify edge
			while (true) {
				int prevCount = sortedEdgeVerticesList[boundaryCount - 1].Count;
				for (int i=0;i<sortedEdgeVerticesList[boundaryCount - 1].Count;i++) {
					int prev = (i + sortedEdgeVerticesList[boundaryCount - 1].Count - 1) % sortedEdgeVerticesList[boundaryCount - 1].Count;
					int next = (i + 1) % sortedEdgeVerticesList[boundaryCount - 1].Count;
					if (VectorCalculator.crossProduct(sortedEdgeVerticesList[boundaryCount - 1][next] - sortedEdgeVerticesList[boundaryCount - 1][i], sortedEdgeVerticesList[boundaryCount - 1][i] - sortedEdgeVerticesList[boundaryCount - 1][prev]).magnitude < 0.001f) {
						sortedEdgeVerticesList[boundaryCount - 1].RemoveAt(i);
						break;
					}
				}
				if (sortedEdgeVerticesList[boundaryCount - 1].Count == prevCount) {
					break;
				}
			}

		}

		List<Vector3> allEdgeVerticesList = new List<Vector3>();
		List<List<int>> boundaries = new List<List<int>>();
		int acc = 0;
		for (int i=0;i<sortedEdgeVerticesList.Count;i++) {
			allEdgeVerticesList.AddRange(sortedEdgeVerticesList[i]);
			boundaries.Add(new List<int>());
			for (int j=0;j<sortedEdgeVerticesList[i].Count;j++) {
				boundaries[i].Add(acc);
				acc++;
			}
		}

		int[] cuttingPlaneTriangles = MeshCalculator.triangulationUnorderedBoundaries(allEdgeVerticesList.ToArray(), boundaries, planeNormal);

		Vector3[] newVertices = new Vector3[remainVerticesList.Count + allEdgeVerticesList.Count];
		int[] newTriangles = new int[remainVerticesList.Count + cuttingPlaneTriangles.Length];
		for (int i=0;i<remainVerticesList.Count;i++) {
			newVertices[i] = remainVerticesList[i];
			newTriangles[i] = i;
		}
		for (int i=0;i<allEdgeVerticesList.Count;i++) {
			newVertices[i + remainVerticesList.Count] = allEdgeVerticesList[i];
		}
		for (int i=0;i<cuttingPlaneTriangles.Length;i++) {
			newTriangles[i + remainVerticesList.Count] = cuttingPlaneTriangles[i] + remainVerticesList.Count;
		}

		//relocate mesh center
		float minx = 2147483647;
		float miny = 2147483647;
		float minz = 2147483647;
		float maxx = -2147483647;
		float maxy = -2147483647;
		float maxz = -2147483647;
		for (int i=0;i<newVertices.Length;i++) {
			minx = Math.Min(minx, newVertices[i].x);
			miny = Math.Min(miny, newVertices[i].y);
			minz = Math.Min(minz, newVertices[i].z);
			maxx = Math.Max(maxx, newVertices[i].x);
			maxy = Math.Max(maxy, newVertices[i].y);
			maxz = Math.Max(maxz, newVertices[i].z);
		}
		Vector3 meshCenter = new Vector3((maxx + minx) / 2, (maxy + miny) / 2, (maxz + minz) / 2);
		for (int i=0;i<newVertices.Length;i++) {
			newVertices[i] -= meshCenter;
		}
		meshCenter = transform.TransformPoint(meshCenter);
		transform.position = meshCenter;

		//update mesh

		Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = newVertices;
		mesh.triangles = newTriangles;
		mesh.MarkModified();
		mesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		obj.updateMesh(true);
		obj.updateTransform();

	}
	/* #endregion */

	/* #region Transform */

	public void startMoving(Vector3 panDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (!isThisScreenFocused && !isOtherScreenFocused) {
			transform.position += panDelta;
		}
		else {
			transform.position += new Vector3(0, panDelta.y, 0);
			if (isThisScreenFocused && isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x < 0) {
						transform.position += new Vector3(panDelta.x, 0, 0);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					transform.position += new Vector3(panDelta.x, 0, 0);
					adjustAlign(true);
				}
			}
			if (isOtherScreenFocused && !isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x > 0) {
						transform.position += new Vector3(panDelta.x, 0, panDelta.z);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					transform.position += new Vector3(panDelta.x, 0, panDelta.z);
					adjustAlign(false);
				}
			}
		}
		obj.updateTransform();
	}

	public void startScaling(float pinchDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (transform.localScale.x + pinchDelta > 0) {
			transform.localScale += new Vector3(pinchDelta, pinchDelta, pinchDelta);
			isEdgeAligned = false;
			closestVertex = -1;
			secondVertex = -1;
		}
		if (isThisScreenFocused) {
			startFocus(true, false);
		}
		else if (isOtherScreenFocused) {
			startFocus(false, false);
		}
	}

	public void startRotating(float turnDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if ((isThisScreenFocused && !isMainScreen) || (isOtherScreenFocused && isMainScreen)) {
			return;
		}
		Quaternion rot = Quaternion.AngleAxis(turnDelta, (isMainScreen ? new Vector3(0, 0, 1) : new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle))));
		transform.rotation = rot * transform.rotation;
		obj.updateTransform();
	}

	private void adjustAlign(bool isMainScreen) {
		return;
		if (isMainScreen && !isOtherScreenPenetrate) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || transform.TransformPoint(vertices[closestVertex]).x < transform.TransformPoint(vertices[selectEdgeVertices[i]]).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || transform.TransformPoint(vertices[secondVertex]).x < transform.TransformPoint(vertices[selectEdgeVertices[i]]).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = transform.TransformPoint(vertices[closestVertex]);
			Vector3 secondVector = transform.TransformPoint(vertices[secondVertex]);
			if (closestVector.x > VectorCalculator.camWidth / 2 && secondVector.x > VectorCalculator.camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
					transform.rotation = rot * transform.rotation;
				}
				closestVector = transform.TransformPoint(vertices[closestVertex]);
				transform.position -= new Vector3(closestVector.x - VectorCalculator.camWidth / 2, 0, 0);
				isEdgeAligned = true;
			}
			else if (closestVector.x > VectorCalculator.camWidth / 2) {
				float dist = (closestVector - new Vector3(transform.position.x, transform.position.y, 0)).magnitude;
				Vector3 targetVector = new Vector3(VectorCalculator.camWidth / 2, Mathf.Sqrt(dist * dist - (VectorCalculator.camWidth / 2 - transform.position.x) * (VectorCalculator.camWidth / 2 - transform.position.x)) * (closestVector.y > transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(transform.position.x, transform.position.y, 0);
				Vector3 b = closestVector - new Vector3(transform.position.x, transform.position.y, 0);
				float deltaAngle = Mathf.Acos(VectorCalculator.dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
				transform.rotation = rot * transform.rotation;
			}
		}
		else if (!isMainScreen && !isThisScreenCuttingPlane) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || VectorCalculator.convertFromServer(transform.TransformPoint(vertices[closestVertex])).x > VectorCalculator.convertFromServer(transform.TransformPoint(vertices[selectEdgeVertices[i]])).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || VectorCalculator.convertFromServer(transform.TransformPoint(vertices[secondVertex])).x > VectorCalculator.convertFromServer(transform.TransformPoint(vertices[selectEdgeVertices[i]])).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = VectorCalculator.convertFromServer(transform.TransformPoint(vertices[closestVertex]));
			Vector3 secondVector = VectorCalculator.convertFromServer(transform.TransformPoint(vertices[secondVertex]));
			if (closestVector.x < - VectorCalculator.camWidth / 2 && secondVector.x < - VectorCalculator.camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle)));
					transform.rotation = rot * transform.rotation;
				}
				closestVector = VectorCalculator.convertFromServer(transform.TransformPoint(vertices[closestVertex]));
				transform.position += new Vector3(Mathf.Cos(-VectorCalculator.angle) * (- closestVector.x - VectorCalculator.camWidth / 2), 0, Mathf.Sin(-VectorCalculator.angle) * (- closestVector.x - VectorCalculator.camWidth / 2));
				isEdgeAligned = true;
			}
			else if (closestVector.x < - VectorCalculator.camWidth / 2) {
				float dist = (closestVector - new Vector3(VectorCalculator.convertFromServer(transform.position).x, VectorCalculator.convertFromServer(transform.position).y, 0)).magnitude;
				Vector3 targetVector = new Vector3(- VectorCalculator.camWidth / 2, Mathf.Sqrt(dist * dist - (VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(transform.position).x) * (VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(transform.position).x)) * (closestVector.y > transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(VectorCalculator.convertFromServer(transform.position).x, VectorCalculator.convertFromServer(transform.position).y, 0);
				Vector3 b = closestVector - new Vector3(VectorCalculator.convertFromServer(transform.position).x, VectorCalculator.convertFromServer(transform.position).y, 0);
				float deltaAngle = Mathf.Acos(VectorCalculator.dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle)));
				transform.rotation = rot * transform.rotation;
			}
		}
	}

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * transform.rotation;
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			transform.position = posToFocus;
			obj.updateTransform();
			if (focusingThisScreen) {
				state = Status.select;
				focusingThisScreen = false;
				isThisScreenFocused = true;
				isOtherScreenFocused = false;
			}
			else if (focusingOtherScreen) {
				state = Status.select;
				focusingOtherScreen = false;
				isOtherScreenFocused = true;
				isThisScreenFocused = false;
			}
			Camera.main.orthographic = true;
			gridController.GetComponent<GridController>().isFixed = true;
		}
		transform.position = Vector3.Lerp(transform.position, posToFocus, focusSpeed * Time.deltaTime);
		obj.updateTransform();
	}

	private void startFocus(bool isThisScreen, bool isNewFocus) {

		if (isNewFocus) {
			for (int i=0;i<selectEdgeVertices.Count;i++) {
				prevEdgeVertices.Add(selectEdgeVertices[i]);
			}
			obj.updateHighlight(focusTriangleIndex, -2);
			focusNormal = hit.normal;
		}

		if (state == Status.select && (smode == SelectMode.selectFace || !isNewFocus)) {
			axisToFocus = VectorCalculator.crossProduct(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			angleToFocus = Vector3.Angle(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			centerToHitFace =
				transform.InverseTransformPoint(
						focusNormal + transform.position
					).normalized *
				VectorCalculator.dotProduct(
					transform.InverseTransformPoint(
						focusNormal + transform.position
					).normalized,
					vertices[triangles[focusTriangleIndex * 3 + 0]]
				);

			float depth = (transform.TransformPoint(centerToHitFace) - transform.position).magnitude;
			depth += 0.025f;
			float offset = VectorCalculator.camWidth / 2;
			if (!isNewFocus) {
				if (isThisScreen) {
					offset = VectorCalculator.camWidth / 2 - transform.position.x;
				}
				else {
					offset = VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(transform.position).x;
				}
			}

			if (isThisScreen) {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 - offset, isNewFocus ? 0 : transform.position.y, depth);
				focusingThisScreen = true;
			}
			else {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 + Mathf.Cos(-VectorCalculator.angle) * offset - Mathf.Sin(-VectorCalculator.angle) * depth, isNewFocus ? 0 : transform.position.y, Mathf.Sin(-VectorCalculator.angle) * offset + Mathf.Cos(-VectorCalculator.angle) * depth);
				focusingOtherScreen = true;
			}
			state = Status.focus;
			focusNormal = (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(-Mathf.Sin(VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle)));
		}
	}

	public void startNewFocusThisScreen() {
		startFocus(true, true);
	}

	public void startNewFocusOtherScreen() {
		startFocus(false, true);
	}

	/* #endregion */

	/* #region Public */

	public void recordRealMeasure() {
		obj.isRealMeasure = true;
		obj.realMeasure = transform.localScale;
	}

	public void recoverRealMeasure() {
		if (!obj.isRealMeasure) {
			obj.isRealMeasure = true;
			transform.localScale = obj.realMeasure;
			if (isThisScreenFocused) {
				startFocus(true, false);
			}
			else if (isOtherScreenFocused) {
				startFocus(false, false);
			}
		}
	}

	public void restart() {
		cancel();

		transform.position = new Vector3(0, 0, 2f);
		transform.localScale = new Vector3(2, 2, 2);
		transform.rotation = Quaternion.Euler(30, 60, 45);

		gameObject.GetComponent<MeshFilter>().mesh = defaultMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = defaultMesh;
		obj.updateTransform();
		obj.updateMesh(true);
	}

	public void cancel() {
		state = Status.select;
		isThisScreenFocused = false;
		isOtherScreenFocused = false;
		isEdgeAligned = false;
		obj.updateHighlight(-1, -1);
		closestVertex = -1;
		secondVertex = -1;
		touchPosition = INF;
		prevTouchPosition = INF;
		Camera.main.orthographic = false;
		gridController.GetComponent<GridController>().isFixed = false;
	}
	/* #endregion */
}