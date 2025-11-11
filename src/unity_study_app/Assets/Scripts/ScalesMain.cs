using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
//using UnityEngine.Input;
using UnityEngine.UI;
using TMPro;

// TODO:
// ✓ Fix Bug: Calibration Finish != paused
// ✓ Fix Bug: Last Task before Block Survey not retryable?
// ✓ Double button before next task + deflate?
// - Wired get current pressures?
// ✓ Post Questionnaire
// ✓ Explore
// - Irgendwie stimmt nur die ersten Zeile mit dem Originalrechner überein: https://hci-studies.org/balanced-latin-square/
// ✓ Calibration mit selbstbestimmtem Ende, statt fester Zeit
// ✓ Log Time for important Events

public class Main : MonoBehaviour {
    public static bool explore = false;
    public static int participant_nr;
    public static string ESP32_IP_address = "192.168.178.76";
    public static string measurements_file = Application.streamingAssetsPath + "/output/Test_measurements.csv";
    public static string surveys_file = Application.streamingAssetsPath + "/output/Test_surveys.csv";
    
    private bool task_running = false;
    private bool logged_answer = false;
    private bool study_paused = false;
    private bool allow_retry = false;
    private int retry_count;
    private int button_presses = 0;

    public static int tolerance = 10;
    public static int adjustment_rate = 20;
    public bool readjust = false;
    public static float get_pressure_delay = 0.25f;
    public static int main_pressure_cuff = 0;
    public static bool semideflate = false;
    private string[] pressures = {"!!!","!!!"};
    private int total_pressure;
    private bool new_pressures = false;


    public Slider participant_slider;
    public GameObject participant_slider_handle;
    public Image participant_slider_handle_texture;
    public Slider target_slider;
    public GameObject target_slider_handle;
    public Image target_slider_handle_texture;
    public GameObject scale_25;
    public GameObject scale_50;
    public GameObject scale_75;

    public TMP_Text condition_specifics_display;
    public TMP_Text condition_progress_display;
    public GameObject pause_study_toggle;
    public Toggle pause_study_toggle_value;
    public GameObject basic_control_buttons;
    public GameObject advanced_control_buttons;
    public GameObject calibration_control_buttons;
    public Toggle debug_info_toggle;
    public GameObject debug_info_screen;
    public TMP_Text change_main_cuff_button_description;
    public GameObject new_CuffPos_panel;
    public TMP_Text new_CuffPos_panel_value;
    public Slider new_CuffPos_panel_idiot_test;
    public GameObject conditon_info_panel;
    public TMP_Text conditon_info_panel_values;
    public GameObject relax_limbs_panel;
    public GameObject start_task_panel;
    public GameObject completed_task_panel;
    public GameObject finished_calibration_panel;
    public GameObject finished_study_panel;
    public Slider finished_study_panel_idiot_test;

    public GameObject block_survey_panel;
    public TMP_Text block_survey_condition_display;
    public Slider convenience_likert;
    public Slider physical_demand_likert;
    public Slider mental_demand_likert;


    public int base_pressure_1 = 500;
    public int base_pressure_2 = 500;
    int total_base_pressure;
    public static int[] absolute_max_pressure = new int[3] {0, 0, 0};
    public static float absolute_pressure_percentage = 0.6f;
    public static int[] total_max_pressure = new int[3] {1000, 1500, 2000}; // bei Kalibrierung nur der erste?
    public static int[] total_min_pressure = new int[3] {0, 0, 0};

    // Debug Displays
    public TMP_Text cuffs_pressure_display;
    public TMP_Text total_pressure_display;
    public TMP_Text total_extrema_display;
    public TMP_Text dependent_variables_display;
    public GameObject calibration_status_display;
    public TMP_Text calibration_status_text;
    private bool calibration_running = false;
    private int calibration_index;
    private static int calibration_presses;
    private bool calibrate_only_current;
    private bool allow_recalibration = false;

    // Independent Variables
    public static int cuff_pos_row = 0;
    public static int other_ivs_row = 0;
    public static int condition_index = 0;
    private int cuff_pos_column;
    private int other_ivs_column;
    private int target_column;
    // CuffPos
    private int CuffPos;
    private string CuffPos_name;
    public bool both_cuffs = false;
    // Pressure
    public static int Pressure_interval = 30;
    private int Pressure;
    private int Pressure_index;
    // Scale (Resolution)
    private int Scale = 25;
    private int maxValue;
    // Target
    private int[] Targets;
    private float Target;
    // Repetitions
    public static int passes = 1;
    private int iteration = 0;

    // Dependent Variables
    int Accuracy;
    float TCT;
    int Crossings;
    float Jitter;
    int pressure_difference;
    int logged_pressure;

    public Log log;


    // Start is called before the first frame update
    public void Start() {
        Debug.Log("P" + participant_nr + " with IP " + ESP32_IP_address);
        Debug.Log("Loaded " + (condition_index+1) + "th condition, with " + passes + " Repetitions & ...");
        if (condition_index == 0) {
            if (!explore) {
                Questionnaire.Trial_start = System.DateTime.Now;
                log.Event("P" + Main.participant_nr + " – Study started");
            }
            else {
                Questionnaire.Exploration_start = System.DateTime.Now;
                log.Flush(1);
                log.Event("P" + Main.participant_nr + " – Exploration started");
            }
        }
        ChangeMainCuff(main_pressure_cuff);

        // Calculate Latin Square Columns
        cuff_pos_column = (int) Mathf.Floor(condition_index/9);
        other_ivs_column = condition_index % 9;
        //other_ivs_column = condition_index % 27;
        // Get Latin Squares Entries & set up accordingly
        CuffPos = balancedLatinSquare(new int[] {0, 1, 2}, cuff_pos_row)[cuff_pos_column];
        if (CuffPos == 2)
            both_cuffs = true;
        Debug.Log("CuffPos " + cuff_pos_row + "[" + cuff_pos_column + "] = " + CuffPos + " --> both? " + both_cuffs);

        Debug.Log("Other IVs " + (other_ivs_row + cuff_pos_column) + "[" + (other_ivs_column) + "]:");
        setupOtherIVs(balancedLatinSquare(Enumerable.Range(0, 9).ToArray(), (other_ivs_row + cuff_pos_column))[other_ivs_column]);
        //setupOtherIVs27(balancedLatinSquare(Enumerable.Range(0, 27).ToArray(), (other_ivs_row + cuff_pos_column))[other_ivs_column]);

        //if (!explore) {
        //    if (condition_index == 0)
        //        File.AppendAllText(Application.streamingAssetsPath + "/output/my_indices.txt", "\n\n:: Participant #" + participant_nr + " ::");
        //    File.AppendAllText(Application.streamingAssetsPath + "/output/my_indices.txt", "\nCuffPos " + cuff_pos_row + "[" + cuff_pos_column + "] = " + CuffPos + " --> " + "Other IVs " + (other_ivs_row + cuff_pos_column) + "[" + (other_ivs_column) + "] = " + balancedLatinSquare(Enumerable.Range(0, 9).ToArray(), (other_ivs_row + cuff_pos_column))[other_ivs_column]);
        //}
        
        int target_column = UnityEngine.Random.Range(0,6);
        Targets = balancedLatinSquare(new int[] {0, 1, 2}, (target_column)).ToArray();
        Debug.Log("Targets [" + target_column + "/5] = " + Targets[0] + ", " + Targets[1] + " & " + Targets[2]);


        // Display Info
        switch (CuffPos) {
            case 0:
                CuffPos_name = "forearm";
                break;
            case 1:
                CuffPos_name = "upper arm";
                break;
            default:
                CuffPos_name = "forearm & upper arm";
                break;
        }
        new_CuffPos_panel_value.text = CuffPos_name;
        conditon_info_panel_values.text = CuffPos_name + "\n\n" + Pressure + " mmHg\n\n" + Scale;

        if ((condition_index % 9) == 0){
            new_CuffPos_panel.SetActive(true);
            //calibration_presses = 0;
            pause_study_toggle_value.SetIsOnWithoutNotify(true);
            study_paused = true;
            //PauseStudyToggle(1);
        } else
            conditon_info_panel.SetActive(true);
        
        condition_specifics_display.text = (CuffPos_name + " × " + Pressure + " mmHg × " + Scale + " Scale");
        condition_progress_display.text = (3*condition_index*passes + iteration + 0) + "/" + (3*3*3*3*passes);

        //if (explore) {
        //   debug_info_toggle.SetIsOnWithoutNotify(true);
        //   DebugInfoToggle();
        //}

        //StartCoroutine(ScaleRoutine());


        // Debug.Log("10 -> " + to_mmHg(10) + " mmHg");
        // Debug.Log("50 -> " + to_mmHg(50) + " mmHg");
        // Debug.Log("1000 -> " + to_mmHg(1000) + " mmHg");
        // Debug.Log("4000 -> " + to_mmHg(4000) + " mmHg");
        // Debug.Log("1.34 mmHg -> " + from_mmHg(1.34f));
        // Debug.Log("6.72 mmHg -> " + from_mmHg(6.72f));
        // Debug.Log("134.32 mmHg -> " + from_mmHg(134.32f));
        // Debug.Log("537.29 mmHg -> " + from_mmHg(537.29f));
    }

    // Update is called once per frame
    void Update() {
        // Manage Task Completion Time
        if (task_running && !logged_answer)
            TCT += Time.deltaTime;

        // On Button Press
        if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !study_paused && !logged_answer && !Input.GetKeyDown(KeyCode.P)) {  // Any Key except Left Clicks (& Screenshot Button "P")
            //Debug.Log("Button detected!");
            if (calibration_running) {
                Debug.Log("Button Press for Calibration detected!");
                StartCoroutine(CalibrateCuffs(++calibration_presses, calibrate_only_current));
            }
            else {
                Debug.Log("Regular Button Press detected!");
                if (retry_count == 0)
                    calibration_status_display.SetActive(false);
                switch (button_presses) {
                    case 0:     // Prepare Task
                        AdjustCuffPressure();
                        conditon_info_panel.SetActive(false);
                        completed_task_panel.SetActive(false);
                        basic_control_buttons.SetActive(false);
                        //if (allow_retry)
                        //    calibration_status_display.SetActive(false);
                        calibration_status_display.SetActive(false);
                        if (allow_retry)
                            retry_count = 0;
                        if (retry_count > 0) {
                            calibration_status_display.SetActive(true);
                            calibration_status_text.text = "Retry #" + retry_count;
                        }
                        allow_retry = false;
                        //start_task_panel.SetActive(true);
                        relax_limbs_panel.SetActive(true);
                        participant_slider.value = 0;

                        //pause_study_toggle_value.SetIsOnWithoutNotify(false);
                        PauseStudyToggle(0);

                        setupTarget(Targets[iteration % 3]);
                        participant_slider_handle.SetActive(true);
                        target_slider_handle.SetActive(true);

                        Debug.Log("Prepared Task...");
                        condition_specifics_display.text = (CuffPos_name + " × " + Pressure + " mmHg × " + Scale + " Scale");
                        condition_progress_display.text = (3*condition_index*passes + iteration++ + 1) + "/" + (3*3*3*3*passes);
                        break;

                    case 1:     // Second Inflation
                        AdjustCuffPressure();
                        relax_limbs_panel.SetActive(false);
                        start_task_panel.SetActive(true);
                        break;

                    case 2:     // Start Task
                        StartTask();
                        break;

                    case 3:     // Log in Answer
                        logged_answer = true;
                        Accuracy = (int)(participant_slider.value - target_slider.value);
                        logged_pressure = total_pressure;
                        
                        participant_slider_handle.SetActive(false);
                        target_slider_handle.SetActive(false);

                        Debug.Log("Logged Answer...");

                        Invoke("EndTask",3f);
                        // Next Task
                        if (iteration < (3*passes))
                            button_presses = -1;
                        break;

                    case 4:     // Block Survey, including nextCondition()
                        DeflateCuffs();
                        block_survey_panel.SetActive(true);
                        block_survey_condition_display.text = CuffPos_name + " × " + Pressure + " mmHg × " + Scale + " Scale<b>?</b>";
                        pause_study_toggle_value.SetIsOnWithoutNotify(true);
                        study_paused = true;
                        basic_control_buttons.SetActive(false);
                        break;
                    
                    default:    // End Study
                        break;
                }
                button_presses++;
            }
        }
    }

    IEnumerator ScaleRoutine() {
        total_base_pressure = base_pressure_1;
        if (both_cuffs)
            total_base_pressure += base_pressure_2;
        if (calibration_running) {
            total_base_pressure = from_mmHg((calibration_index+1)*Pressure_interval);
            if (both_cuffs)
                total_base_pressure *= 2;
        }
        Debug.Log("Total Base Pressure = " + total_base_pressure + " --> ");

        total_extrema_display.text = "↑: " + total_max_pressure[Pressure_index] + " (" + to_mmHg(total_max_pressure[Pressure_index]).ToString("F1") + " mmHg)\n=: " + total_base_pressure + " (" + to_mmHg(total_base_pressure).ToString("F1") + " mmHg)\n↓: " + total_min_pressure[Pressure_index] + " (" + to_mmHg(total_min_pressure[Pressure_index]).ToString("F1") + " mmHg)";

        participant_slider.value = 0;
        bool approached_from_below = true;
        // Get Current Pressure(s)
        while(task_running || calibration_running) {
            StartCoroutine(WebRequest("/get"));

            if (new_pressures) {
                // Adjust Scale
                int slider_value;
                total_pressure = Int32.Parse(pressures[main_pressure_cuff]);
                if (both_cuffs)
                    total_pressure += Int32.Parse(pressures[(main_pressure_cuff + 1) % 2]);
                
                //Debug.Log("New Slider Value = " + maxValue + " * " + (total_pressure - total_base_pressure) + " / " + (absolute_max_pressure[calibration_index] - total_base_pressure) + " clamped: " + Mathf.Clamp((absolute_max_pressure[calibration_index] - total_base_pressure), 1, 10000));
                if (calibration_running)
                    slider_value = Mathf.Clamp(maxValue*(total_pressure - total_base_pressure)/Mathf.Clamp((absolute_max_pressure[calibration_index] - total_base_pressure), 1, 10000), 0, maxValue);
                    //slider_value = Mathf.Clamp(maxValue*(total_pressure - total_base_pressure)/(absolute_max_pressure[calibration_index] - total_base_pressure), 0, maxValue);
                    //slider_value = Mathf.Clamp(maxValue*(total_pressure - total_base_pressure)/((CuffPos/2 + 1)*total_base_pressure), 0, maxValue); ???
                else
                    slider_value = Mathf.Clamp(maxValue*(total_pressure - total_base_pressure)/Mathf.Clamp((total_max_pressure[Pressure_index] - total_base_pressure), 1, 10000), 0, maxValue);
                    //slider_value = Mathf.Clamp(maxValue*(total_pressure - total_base_pressure)/(total_max_pressure[Pressure_index] - total_base_pressure), 0, maxValue);

                participant_slider.value = slider_value;
                new_pressures = false;


                // Measure Crossings
                if (((slider_value > target_slider.value && approached_from_below) || (slider_value < target_slider.value && !approached_from_below)) && !logged_answer) {
                    Crossings++;
                    approached_from_below ^= true;
                }

                // Measure Jitter
                if (logged_answer && (Mathf.Abs(total_pressure - logged_pressure) > Mathf.Abs(pressure_difference))) {
                    pressure_difference = total_pressure - logged_pressure;
                    Jitter = pressure_difference / (float)(total_max_pressure[Pressure_index] - total_base_pressure);
                }
                //if (logged_answer && (Mathf.Abs(slider_value - target_slider.value) > Mathf.Abs(Jitter)))
                //    Jitter = slider_value - target_slider.value;


                // Display on Screen
                cuffs_pressure_display.text = "#1: " + pressures[0] + " (" + to_mmHg(Int32.Parse(pressures[0])).ToString("F1") + " mmHg)\n#2: " + pressures[1] + " (" + to_mmHg(Int32.Parse(pressures[1])).ToString("F1") + " mmHg)";
                total_pressure_display.text = "∑: " + total_pressure + " (" + to_mmHg(total_pressure).ToString("F1") + " mmHg)";
                if (calibration_running)
                    total_extrema_display.text = "↑: " + total_max_pressure[calibration_index] + " (" + to_mmHg(total_max_pressure[calibration_index]).ToString("F1") + " mmHg)\n=: " + from_mmHg((calibration_index+1)*Pressure_interval) + " (" + ((calibration_index+1)*Pressure_interval) + " mmHg)\n↓: " + total_min_pressure[calibration_index] + " (" + to_mmHg(total_min_pressure[calibration_index]).ToString("F1") + " mmHg)";

                dependent_variables_display.text = "Accuracy = " + Accuracy + "\nTCT = " + TCT.ToString("F2") + " s\nCrossings = " + Crossings + "\nJitter = " + (100*Jitter).ToString("F2")  + "% (" + pressure_difference + ")";
                

                // Turn Participant Scale green when on correct field
                if (target_slider_handle_texture.IsActive() && (participant_slider.value == target_slider.value)) {
                    participant_slider_handle_texture.color = new Color32(0, 255, 0, 255);
                    //participant_slider_handle.color = new Color32(143, 229, 66, 255);
                } else {
                    participant_slider_handle_texture.color = new Color32(0, 0, 255, 255);
                }
            }

            yield return new WaitForSeconds(get_pressure_delay);
        }
    }


    public void CalibrateCuffsWrapper(bool current) {
        if (!calibration_running) {
            calibration_presses = 0;
            StartCoroutine(CalibrateCuffs(0, current));
        } else
            Debug.Log("Finish the ongoing Calibration first...");
    }
    IEnumerator CalibrateCuffs(int step, bool current) {
        if (step == 0)
            calibrate_only_current = current;

        Debug.Log("Calibrating Step #" + step + " for current? " + current + " --> " + calibration_index);
        calibration_status_display.SetActive(true);

        if (step >= 12 || (current && step >= 4)) {
            DeflateCuffs();

            calibration_running = false;
            pause_study_toggle_value.SetIsOnWithoutNotify(true);
            study_paused = true;
            
            completed_task_panel.SetActive(false);
            finished_calibration_panel.SetActive(true);
            calibration_control_buttons.SetActive(false);

            //if (!explore) {
            //    debug_info_toggle.SetIsOnWithoutNotify(false);
            //    DebugInfoToggle();
            //}
            debug_info_toggle.SetIsOnWithoutNotify(false);
            DebugInfoToggle();

            button_presses = 0;
            Debug.Log("Finished Cuff Calibration – Now continuing with the Tasks!");
            yield break;
        }

        Debug.Log("Still recalibrating!");

        switch (step % 4) {
            case 0:
                calibration_index = Pressure_index;
                if (!current)
                    calibration_index = (int)Mathf.Floor(step/3);

                calibration_running = true;
                StartCoroutine(ScaleRoutine());

                AdjustCuffPressure();
                start_task_panel.SetActive(false);
                completed_task_panel.SetActive(false);
                basic_control_buttons.SetActive(false);
                conditon_info_panel.SetActive(false);
                relax_limbs_panel.SetActive(true);
                participant_slider_handle.SetActive(true);
                calibration_control_buttons.SetActive(false);

                debug_info_toggle.SetIsOnWithoutNotify(true);
                DebugInfoToggle();

                if (!current) {
                    condition_specifics_display.text = ("Calibrating: " + CuffPos_name + " × " + (calibration_index+1)*Pressure_interval + " mmHg");
                    calibration_status_text.text = "Preparing Calibration " + (calibration_index+1) + "/3...";
                    Debug.Log("Started Cuff Calibration for Pessure " + (calibration_index+1) + "/3...");
                } else {
                    condition_specifics_display.text = ("Recalibrating: " + CuffPos_name + " × " + (calibration_index+1)*Pressure_interval + " mmHg");
                    calibration_status_text.text = "Preparing Recalibration...";
                    Debug.Log("Started Cuff Recalibration for Pessure #" + (calibration_index+1) + " only...");
                }
                absolute_max_pressure[calibration_index] = 0;
                total_min_pressure[calibration_index] = total_base_pressure;
                break;

            case 1:
                AdjustCuffPressure();
                relax_limbs_panel.SetActive(false);
                start_task_panel.SetActive(true);
                break;

            case 2:
                //logged_answer = true;
                Cursor.visible = false;
                pause_study_toggle.SetActive(false);
                start_task_panel.SetActive(false);

                // Loop
                int current_pressure = 0;
                int current_presses = calibration_presses;
                while (calibration_presses == current_presses) {
                    calibration_status_text.text = "Try to exert the most pressure possible...\nOnly the peak pressure is important, so you do not have to hold your max pressure & can also start multiple attempts to reach it.\n\nRelax your arm when satisfied with your result & press any button to continue.";

                    current_pressure = Int32.Parse(pressures[main_pressure_cuff]);
                    if (both_cuffs)
                        current_pressure += Int32.Parse(pressures[(main_pressure_cuff + 1) % 2]);

                    if (current_pressure > absolute_max_pressure[calibration_index])
                        absolute_max_pressure[calibration_index] = current_pressure;
                    if (current_pressure < total_min_pressure[calibration_index])
                        total_min_pressure[calibration_index] = current_pressure;

                    total_max_pressure[calibration_index] = (int)(absolute_pressure_percentage*absolute_max_pressure[calibration_index]);

                    yield return new WaitForSeconds(get_pressure_delay);
                }

                //total_max_pressure[calibration_index] = (int)(absolute_pressure_percentage*absolute_max_pressure[calibration_index]);
                break;
            
            case 3:
                Cursor.visible = true;
                allow_recalibration = true;
                calibration_control_buttons.SetActive(true);
                if (!current)
                    calibration_status_text.text = "Calibrated " + (calibration_index+1) + "/3!";
                else
                    calibration_status_text.text = "Recalibrated!";

                Debug.Log("New Total Max Pressures: #1: " + total_max_pressure[0] + ", #2: " + total_max_pressure[1] + ", #3: " + total_max_pressure[2]);

                // if (step >= 11 || (current && step >= 3)) {
                //     DeflateCuffs();

                //     calibration_running = false;
                //     pause_study_toggle_value.SetIsOnWithoutNotify(true);
                //     study_paused = true;
                //     finished_calibration_panel.SetActive(true);

                //     //if (!explore) {
                //     //    debug_info_toggle.SetIsOnWithoutNotify(false);
                //     //    DebugInfoToggle();
                //     //}
                //     debug_info_toggle.SetIsOnWithoutNotify(false);
                //     DebugInfoToggle();

                //     button_presses = 0;
                //     Debug.Log("Finished Cuff calibration!");
                // }
                // else {
                //     Debug.Log("Not finished Cuff Calibration yet!");
                //     completed_task_panel.SetActive(true);
                // }
                Debug.Log("Cuff Calibration may not be over yet...");
                completed_task_panel.SetActive(true);

                pause_study_toggle.SetActive(true);
                //logged_answer = false;
                break;

            default:
                break;
        }
    }

    public void RecalibrateLastButton() {
        if (allow_recalibration) {
            calibration_presses -= 4;
            Debug.Log("Opted for immediate Recalibration of #" + (calibration_index+1));
            calibration_status_text.text = "Opted for immediate\nRecalibration of " + (calibration_index+1) + "/3...";
        }
            Debug.Log("The immediate Recalibration for #" + (calibration_index+1) + " is already prepared...");
        allow_recalibration = false;
    }


    public void StartTask() {
        task_running = true;
        logged_answer = false;
        pause_study_toggle.SetActive(false);
        Cursor.visible = false;

        //if (allow_retry)          // Jetzt schon in Prep-Phase
        //    retry_count = 0;
        allow_retry = true;

        start_task_panel.SetActive(false);

        Accuracy = int.MinValue;
        TCT = 0;
        Crossings = 0;
        Jitter = 0;
        pressure_difference = 0;

        Debug.Log("Started Task...");

        StartCoroutine(ScaleRoutine());
    }

    public void EndTask() {
        task_running = false;
        logged_answer = false;
        pause_study_toggle.SetActive(true);
        Cursor.visible = true;

        completed_task_panel.SetActive(true);
        basic_control_buttons.SetActive(true);
        if (semideflate) {
            int deflation_amount = from_mmHg(Pressure_interval);
            if (both_cuffs)
                StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + 4*tolerance + "&max_1=" + (base_pressure_1-deflation_amount) + "&lvl_1=1&max_2=" + (base_pressure_2-deflation_amount) + "&lvl_2=1"));
            else {
                if (main_pressure_cuff == 0)
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + 4*tolerance + "&max_1=" + (base_pressure_1-deflation_amount) + "&lvl_1=1&max_2=0&lvl_2=1"));
                else
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + 4*tolerance + "&max_1=0&lvl_1=1&max_2=" + (base_pressure_1-deflation_amount) + "&lvl_2=1"));
            }
            Debug.Log("Semideflated by " + deflation_amount + " to: " + (base_pressure_1-deflation_amount) + " (" + to_mmHg(base_pressure_1-deflation_amount) + " mmHg) & " + both_cuffs + ": " + (base_pressure_2-deflation_amount) + " (" + to_mmHg(base_pressure_2-deflation_amount) + " mmHg)");
        }

        if (!explore)
            File.AppendAllText(measurements_file, "\n" + participant_nr + "," + CuffPos + "," + Pressure + "," + Scale + "," + Target + "," + total_max_pressure[calibration_index] + "," + Accuracy + ",\"" + TCT + "\"," + Crossings + "," + pressure_difference + ",\"" + Jitter + "\"," + retry_count);

        Debug.Log("Task ended...");
    }

    public void RetryTask() {
        if (allow_retry) {
            Debug.Log("Opted for a retry!");
            button_presses = 0;
            retry_count++;
            iteration--;

            calibration_status_text.text = "Opted for Retry #" + retry_count;
            calibration_status_display.SetActive(true);
        } else {
            Debug.Log("Can't retry yet/again...");
            calibration_status_text.text = "Can't retry yet/again...";
            calibration_status_display.SetActive(true);
        }
        allow_retry = false;
    }

    public void RetryAndRecalibrate() {
        if (allow_retry) {
            RetryTask();
            CalibrateCuffsWrapper(true);
        } else {
            Debug.Log("Can't recalibrate & retry yet/again...");
            calibration_status_text.text = "Can't recalibrate\n& retry yet/again...";
            calibration_status_display.SetActive(true);
        }
    }

    public void ChangedCuffPosButton() {
        if (new_CuffPos_panel_idiot_test.value == 1) {
            new_CuffPos_panel.SetActive(false);
            PauseStudyToggle(0);

            CalibrateCuffsWrapper(false);
            AdjustCuffPressure();
        } else
            Debug.Log("Idiot Test failed (" + (100*new_CuffPos_panel_idiot_test.value).ToString("F1") + "%) - can't continue yet...");
    }

    public void AnswerFinalSurveyButton() {
        if (finished_study_panel_idiot_test.value == 1) {
            switchToScene("Questionnaire");
        } else
            Debug.Log("Idiot Test failed (" + (100*finished_study_panel_idiot_test.value).ToString("F1") + "%) - can't answer Post Questionnaire yet...");
    }

    public void PauseStudyToggle(int i) {
        switch (i) {
            case 0:
                pause_study_toggle_value.SetIsOnWithoutNotify(false);
                study_paused = false;
                break;
            case 1:
                study_paused = true;
                break;
            default:
                study_paused ^= true;
                break;
        }
        if (study_paused)
            advanced_control_buttons.SetActive(true);
        else
            advanced_control_buttons.SetActive(false);
    }

    public void DebugInfoToggle() {
        if (debug_info_toggle.isOn)
            debug_info_screen.SetActive(true);
        else
            debug_info_screen.SetActive(false);
    }

    public void ChangeMainCuff(int mode) {
        if (mode == -1) {
            main_pressure_cuff = (main_pressure_cuff + 1) % 2;
        } else
            main_pressure_cuff = mode;
        Debug.Log("(New) Main Cuff: " + main_pressure_cuff);

        if (main_pressure_cuff == 0)
            change_main_cuff_button_description.text = "Set right as Main Cuff";
        else
            change_main_cuff_button_description.text = "Set left as Main Cuff";
    }


    List<int> balancedLatinSquare(int[] independent_variables, int row) {
        List<int> result = new List<int>();
        // Based on "Balanced Latin Square Generator" (https://github.com/valentin-schwind/balanced-latinsquare-generator/blob/master/index.html)
        // Which itself is based on "Bradley, J. V. Complete counterbalancing of immediate sequential effects in a Latin square design. J. Amer. Statist. Ass.,.1958, 53, 525-528. "
        int j = 0, h = 0;
        for (int i = 0; i < independent_variables.Length; ++i) {
            int val = 0;
            if (i < 2 || i % 2 != 0) {
                val = j++;
            } else {
                val = independent_variables.Length - h - 1;
                ++h;
            }

            int idx = (val + row) % independent_variables.Length;
            result.Add(independent_variables[idx]);
        }

        if (independent_variables.Length % 2 != 0 && row % 2 != 0) {
            result.Reverse();
        }

        return result;
    }

    void setupOtherIVs (int i) {
        // Pressure Level
        Pressure_index = (int)Mathf.Floor(i/3);
        Pressure = (int)((Pressure_index+1)*Pressure_interval);
        base_pressure_1 = from_mmHg(Pressure);
        base_pressure_2 = from_mmHg(Pressure);

        // Scale
        Scale = (int)(25*((i % 3)+1));
        SetScaleResolution(Scale);

        Debug.Log(i + " = (" + Pressure + " mmHg, " + Scale + " Scale)");
    }

    void setupOtherIVs27 (int i) {
        // Pressure Level
        Pressure = (int)((Mathf.Floor(i/9)+1)*Pressure_interval);

        // Scale
        Scale = (int)(25*(Mathf.Floor((i % 9)/3)+1));
        SetScaleResolution(Scale);

        // Target
        setupTarget(i % 3);

        Debug.Log(i + " = (" + Pressure + " mmHg, " + Scale + " Scale, " + Target + " Target)");
    }

    void setupTarget (int i) {
        switch (i) {
            case 0:
                Target = 0.1f;
                break;
            case 1:
                Target = 0.5f;
                break;
            default:
                Target = 0.9f;
                break;
        }
        SetTarget(Target);
    }


    public int from_mmHg(float value) {
        return (int)Math.Round(4095*(45*value/7.50062)/3300);
    }

    public float to_mmHg(int value) {
        return (float)(7.50062*(3300*(float)value/4095)/45);
    }

    public void nextCondition() {
        if (convenience_likert.value <= 7 && physical_demand_likert.value <= 7 & mental_demand_likert.value <= 7) {
            // Save Answers
            if (!explore)
                    File.AppendAllText(surveys_file, "\n" + participant_nr + "," + CuffPos + "," + Pressure + "," + Scale + "," + Mathf.Clamp(convenience_likert.value,1,7) + "," + Mathf.Clamp(physical_demand_likert.value,1,7) + "," + Mathf.Clamp(mental_demand_likert.value,1,7));

            if ((3*condition_index*passes + iteration + 1) >= (3*3*3*3*passes)) {
                // Hier könnte ihr Post Questionnaire stehen!
                Questionnaire.Trial_stop = System.DateTime.Now;
                Debug.Log("Finished Study!");
                log.Event("P" + Main.participant_nr + " – Study finished");
                block_survey_panel.SetActive(false);
                finished_study_panel.SetActive(true);
            }
            else {
                //condition_index += 3;
                condition_index++;

                SceneManager.LoadScene("Scales");
            }
        } else
            Debug.Log("Please answer all questions before continuing.");
    }

    public void switchToScene(string scene) {
        if (explore) {
            Questionnaire.Exploration_stop = System.DateTime.Now;
            log.Event("P" + Main.participant_nr + " – Exploration concluded");
        }
        else if (!finished_study_panel.activeSelf)
            log.Event("P" + Main.participant_nr + " – Study aborted");

        SceneManager.LoadScene(scene);
    }

    // IEnumerator CalibrateCuffs() {
    //     task_running = true;
    //     StartCoroutine(ScaleRoutine());

    //     Debug.Log("Started Cuff calibration of " + calibration_duration + "s...");
    //     absolute_max_pressure_1 = 0;
    //     absolute_min_pressure_1 = base_pressure_1;
    //     absolute_max_pressure_2 = 0;
    //     absolute_min_pressure_2 = base_pressure_2;

    //     calibration_status_display.text = "Relax your limb(s)!";
    //     yield return new WaitForSeconds(calibration_grace_period);
    //     AdjustCuffPressure();
    //     yield return new WaitForSeconds(calibration_grace_period);

    //     // Loop
    //     for (int i = Mathf.RoundToInt(calibration_duration/get_pressure_delay); i > 0; i--) {
    //         calibration_status_display.text = "Try to exert the most pressure possible\n" + Mathf.CeilToInt(i*get_pressure_delay) + "s remaining";

    //         if (Int32.Parse(pressures[0]) > absolute_max_pressure_1)
    //             absolute_max_pressure_1 = Int32.Parse(pressures[0]);
    //         if (Int32.Parse(pressures[0]) < absolute_min_pressure_1)
    //             absolute_min_pressure_1 = Int32.Parse(pressures[0]);

    //         if (Int32.Parse(pressures[1]) > absolute_max_pressure_2)
    //             absolute_max_pressure_2 = Int32.Parse(pressures[1]);
    //         if (Int32.Parse(pressures[0]) < absolute_min_pressure_2)
    //             absolute_min_pressure_2 = Int32.Parse(pressures[1]);

    //         yield return new WaitForSeconds(get_pressure_delay);
    //     }

    //     max_pressure_1 = (int)(absolute_pressure_percentage*absolute_max_pressure_1);
    //     max_pressure_2 = (int)(absolute_pressure_percentage*absolute_max_pressure_2);

    //     task_running = false;
    //     Debug.Log("Finished Cuff calibration!");
    //     calibration_status_display.text = "Calibrated!";
    // }

    public void AdjustCuffPressure() {
        if (calibration_running) {
            if (both_cuffs)
                StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + from_mmHg((calibration_index+1)*Pressure_interval) + "&lvl_1=1&max_2=" + from_mmHg((calibration_index+1)*Pressure_interval) + "&lvl_2=1"));
            else {
                if (main_pressure_cuff == 0)
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + from_mmHg((calibration_index+1)*Pressure_interval) + "&lvl_1=1&max_2=0&lvl_2=1"));
                else
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=0&lvl_1=1&max_2=" + from_mmHg((calibration_index+1)*Pressure_interval) + "&lvl_2=1"));
            }
        }
        else {
            if (both_cuffs)
                StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + base_pressure_1 + "&lvl_1=1&max_2=" + base_pressure_2 + "&lvl_2=1"));
            else {
                if (main_pressure_cuff == 0)
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=" + base_pressure_1 + "&lvl_1=1&max_2=0&lvl_2=1"));
                else
                    StartCoroutine(WebRequest("/set?rea=0&adj=" + adjustment_rate + "&tol=" + tolerance + "&max_1=0&lvl_1=1&max_2=" + base_pressure_1 + "&lvl_2=1"));
            }
        }
    }

    public void DeflateCuffs() {
        StartCoroutine(WebRequest("/set?lvl_1=0&lvl_2=0"));
    }

    public void SetTarget(float value) {
        target_slider.value = target_slider.maxValue*value;
        //Debug.Log("New Target := " + value + "*" + target_slider.maxValue + " = " + (target_slider.maxValue*value) + " ≈ " + target_slider.value);
    }

    public void SetScaleResolution(int newScale) {
        Scale = newScale;
        maxValue = Scale-1;

        participant_slider.maxValue = maxValue;
        target_slider.maxValue = maxValue;

        participant_slider_handle_texture.transform.localScale = new Vector3(1f, 25f/Scale, 1f);
        target_slider_handle_texture.transform.localScale = new Vector3(1f, 25f/Scale, 1f);

        switch (newScale) {
            case 50:
                scale_25.SetActive(false);
                scale_50.SetActive(true);
                scale_75.SetActive(false);
                participant_slider.transform.localScale = new Vector3(1f, 1.0205f, 1f);
                target_slider.transform.localScale = new Vector3(1f, 1.0205f, 1f);
                break;
            case 75:
                scale_25.SetActive(false);
                scale_50.SetActive(false);
                scale_75.SetActive(true);
                participant_slider.transform.localScale = new Vector3(1f, 1.0265f, 1f);
                target_slider.transform.localScale = new Vector3(1f, 1.0265f, 1f);
                break;
            default:
                scale_25.SetActive(true);
                scale_50.SetActive(false);
                scale_75.SetActive(false);
                participant_slider.transform.localScale = new Vector3(1f, 1f, 1f);
                target_slider.transform.localScale = new Vector3(1f, 1f, 1f);
                break;
        }

        Debug.Log("Set Scale Resolution to: " + Scale);
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
