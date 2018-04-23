using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour {

    public Player player;

    public Vector3 offset = new Vector3(0f, 8f, -1.5f);
    public float smoothTime = 0.2f;

    private Vector3 velocity;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void LateUpdate () {
        transform.position = Vector3.SmoothDamp(transform.position, player.transform.position + offset, ref velocity, smoothTime);
	}
}
