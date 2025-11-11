using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class Screenshot : MonoBehaviour
{
    private string sc_path = Application.streamingAssetsPath + "/screenshot/";
    public int size = 1;

    void Start()
    {
        if (!Directory.Exists(sc_path)) {
            Directory.CreateDirectory(sc_path);
            Debug.Log("\"screenshot\" Directory created");
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ScreenCapture.CaptureScreenshot(sc_path + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png", size);
        }
    }
}
