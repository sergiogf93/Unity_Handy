using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {

    [SerializeField] Light sun;
    [SerializeField] int checkDistance = 100;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        
	}

    public bool IsUnderShadow()
    {
        bool underSun = false;
        Vector3 sunDir = sun.transform.forward;
        sunDir.Normalize();
        sunDir *= checkDistance;

        foreach (Transform child in transform)
        {
            if (!Physics.Raycast(child.position, -sunDir, checkDistance))
            {
                Debug.DrawLine(child.position, child.position - sunDir, Color.red);
                underSun = true;
            }
            else
            {
                Debug.DrawLine(child.position, child.position - sunDir, Color.green);
            }
        }

        return !underSun;
    }
}
