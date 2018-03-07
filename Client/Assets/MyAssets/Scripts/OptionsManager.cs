using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    public void Activate(GameObject go)
    {
        transform.position = go.transform.position;
    }

    public void Placement()
    {
        DebugManager.Instance.PrintToInfoLog("Placement");
    }
}
