using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class Test : MonoBehaviour {
    public string ESP32_IP_address = "192.168.178.76";
    
    public bool stop_task = false;
    public float get_pressure_delay = 0.25f;
    private string[] pressures = {"!!!","!!!"};
    private bool new_pressures = false;


    public Slider participant_slider;
    public Image participant_slider_handle;
    public Slider target_slider;
    public Image target_slider_handle;
    public int scale_resolution = 25;


    public int tolerance = 10;
    public int adjustment_rate = 20;
    public bool readjust = false;

    public Slider both_cuffs_toggle;
    public bool both_cuffs = false;
    public Slider target_control_slider;

    public int base_pressure_1 = 500;
    public int max_pressure_1 = 1500;
    int min_pressure_1 = 0;
    public int base_pressure_2 = 500;
    public int max_pressure_2 = 1500;
    int min_pressure_2 = 0;


    public TMP_Text cuff_1_pressure_display;
    public TMP_Text cuff_1_extrema_display;
    public TMP_Text cuff_2_pressure_display;
    public TMP_Text cuff_2_extrema_display;
    public int calibration_duration = 15;
    public int calibration_grace_period = 3;
    public TMP_Text calibration_status_display;


    // Start is called before the first frame update
    public IEnumerator Start() {
        Debug.Log("Started...");
        AdjustCuffPressure();

        SetScaleResolution(scale_resolution);
        // Get Current Pressure(s)
        while(true) {
            StartCoroutine(WebRequest("/get"));

            if (new_pressures) {
                // Display on Screen
                cuff_1_pressure_display.text = "Cuff #1: " + pressures[0];
                cuff_1_extrema_display.text = "↑: " + max_pressure_1 + "\n=: " + base_pressure_1 + "\n↓: " + min_pressure_1;
                cuff_2_pressure_display.text = "Cuff #2: " + pressures[1];
                cuff_2_extrema_display.text = "↑: " + max_pressure_2 + "\n=: " + base_pressure_2 + "\n↓: " + min_pressure_2;

                // Adjust Scale
                int slider_value;
                if (!both_cuffs) {
                    Debug.Log("einzel");
                    slider_value = scale_resolution*(Int32.Parse(pressures[0]) - base_pressure_1)/(max_pressure_1 - base_pressure_1);
                } else {
                    Debug.Log("beide");
                    slider_value = scale_resolution*(Int32.Parse(pressures[0]) - base_pressure_1 + Int32.Parse(pressures[1]) - base_pressure_2)/(max_pressure_1 - base_pressure_1 + max_pressure_1 - base_pressure_2);
                }
                //Debug.Log(Int32.Parse(pressures[0]) + " - 500 => " + (Int32.Parse(pressures[0]) - base_pressure_1) + " / 1000 => " + (Int32.Parse(pressures[0]) - base_pressure_1)/(max_pressure_1 - base_pressure_1) + " --> " + slider_value + "/" + scale_resolution);
                participant_slider.value = slider_value;
                new_pressures = false;
                
                // Turn Participant Scale green when on correct field
                if (target_slider_handle.IsActive() && (participant_slider.value == target_slider.value)) {
                    participant_slider_handle.color = new Color32(0, 255, 0, 255);
                    //participant_slider_handle.color = new Color32(143, 229, 66, 255);
                } else {
                    participant_slider_handle.color = new Color32(0, 0, 255, 255);
                }
            }

            yield return new WaitForSeconds(get_pressure_delay);
        }
    }

    // Update is called once per frame
    // void Update() {
    // }

    public void StartTask() {
        stop_task = false;
        Debug.Log("Task not yet implemented...");
    }

    public void EndTask() {
        stop_task = true;
        Debug.Log("Task ended...");
    }

    public void CalibrateCuffsWrapper() {
        StartCoroutine(CalibrateCuffs());
    }
    IEnumerator CalibrateCuffs() {
        Debug.Log("Started Cuff calibration of " + calibration_duration + "s...");
        max_pressure_1 = 0;
        min_pressure_1 = base_pressure_1;
        max_pressure_2 = 0;
        min_pressure_2 = base_pressure_2;

        calibration_status_display.text = "Relax your limb(s)!";
        yield return new WaitForSeconds(calibration_grace_period);
        AdjustCuffPressure();
        yield return new WaitForSeconds(calibration_grace_period);

        // Loop
        for (int i = Mathf.RoundToInt(calibration_duration/get_pressure_delay); i > 0; i--) {
            calibration_status_display.text = "Try to exert the most pressure possible\n" + Mathf.CeilToInt(i*get_pressure_delay) + "s remaining";

            if (Int32.Parse(pressures[0]) > max_pressure_1)
                max_pressure_1 = Int32.Parse(pressures[0]);
            if (Int32.Parse(pressures[0]) < min_pressure_1)
                min_pressure_1 = Int32.Parse(pressures[0]);

            if (Int32.Parse(pressures[1]) > max_pressure_2)
                max_pressure_2 = Int32.Parse(pressures[1]);
            if (Int32.Parse(pressures[0]) < min_pressure_2)
                min_pressure_2 = Int32.Parse(pressures[1]);

            yield return new WaitForSeconds(get_pressure_delay);
        }

        Debug.Log("Finished Cuff calibration!");
        calibration_status_display.text = "Calibrated!";
    }

    public void AdjustCuffPressure() {
        if (both_cuffs)
            StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + base_pressure_1 + "&lvl_1=1&max_2=" + base_pressure_2 + "&lvl_2=1"));
        else
            StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + base_pressure_1 + "&lvl_1=1&max_2=0&lvl_2=1"));
    }

    public void DeflateCuffs() {
        StartCoroutine(WebRequest("/set?lvl_1=0&lvl_2=0"));
    }

    public void SetTargetFromSlider() {
        SetTarget(target_control_slider.value/target_control_slider.maxValue);
    }

    public void SetTarget(float value) {
        target_slider.value = target_slider.maxValue*value;
    }

    public void SetScaleResolution(int res) {
        scale_resolution = res;

        participant_slider.maxValue = scale_resolution;
        target_slider.maxValue = scale_resolution;

        participant_slider_handle.transform.localScale = new Vector3(1f, 25f/res, 1f);
        target_slider_handle.transform.localScale = new Vector3(1f, 25f/res, 1f);
        SetTargetFromSlider();

        Debug.Log("Set Scale Resolution to: " + res);
    }

    public void ToggleBothCuffs() {
        if (both_cuffs_toggle.value >= 1)
            both_cuffs = true;
        else
            both_cuffs = false;
        
        Debug.Log("Using both cuffs is now set to: " + both_cuffs);
    }

    IEnumerator WebRequest(string page) {
        var request = UnityWebRequest.Get("http://" + ESP32_IP_address + page);
        yield return request.SendWebRequest();

        string response = request.downloadHandler.text;
        //Debug.Log("Got following Response:\n" + response);
        if (page == "/get") {
            string[] parts = response.Split(' ');
            if (parts.Length == 2) {
                pressures = parts;
                new_pressures = true;
            }
        }
    }
}
