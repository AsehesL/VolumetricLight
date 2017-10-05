using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DpTest : MonoBehaviour {

    public DepthTextureMode depthTextureMode;

	// Use this for initialization
	void Start () {
        GetComponent<Camera>().depthTextureMode = depthTextureMode;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
