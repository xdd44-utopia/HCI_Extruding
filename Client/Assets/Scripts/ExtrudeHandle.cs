using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExtrudeHandle : MonoBehaviour
{
	public GameObject handle;
	public Text debugText;
	private float timer;
	private float timeOut = 0.2f;
	void Start()
	{
		
	}

	void Update()
	{
		handle.SetActive(timer > 0);
		timer -= Time.deltaTime;
	}
	
	public void updateDist(float d) {
		handle.GetComponent<RectTransform>().anchoredPosition = new Vector2(d / Camera.main.orthographicSize * (Screen.height / 2) - (Screen.width / 2), 0);
		timer = timeOut;
	}
}
