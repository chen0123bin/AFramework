using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-11000)]
public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LWDebug.Log("Start");
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnDestroy()
    {
        LWDebug.Log("OnDestroy");
    }
}
