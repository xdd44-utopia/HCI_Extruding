using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	public GameObject slider;
	public GameObject cam;

	public Text debugText;

	[HideInInspector]
	public float angle;

	private float defaultAngle = - Mathf.PI / 2;
	private const float leftMost = 18.75f;
	private const float rightMost = -18.75f;

	// Start is called before the first frame update
	void Start()
	{
		angle = defaultAngle;
	}

	// Update is called once per frame
	void Update()
	{

		float size = cam.GetComponent<Camera>().orthographicSize;
		slider.transform.localScale = new Vector3(size * 0.025f, size * 0.025f, size * 0.025f);

		float pos = leftMost - (leftMost - rightMost) * (angle + Mathf.PI / 2) / (Mathf.PI / 2);
		transform.localPosition = new Vector3(pos, transform.localPosition.y, 0);
	}
}
