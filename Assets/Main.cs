using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TensorFlow;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
        TensorFlowSharp.Android.NativeBinding.Init();
    }

    // Update is called once per frame
    void Update () {
		
	}
}
