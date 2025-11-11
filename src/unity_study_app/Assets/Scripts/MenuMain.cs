using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

using System;

public class MenuMain : MonoBehaviour {
    public string dir_path = Application.streamingAssetsPath + "/output/";

    public static string saved_participant_nr = "";

    public TMP_InputField ESP32_IP_input_field_object;
    public TMP_Text ESP32_IP_input_field;
    public TMP_InputField participant_input_field_object;
    public TMP_Text participant_input_field;
    public Slider repetitions_slider;
    public TMP_Text get_pressure_delay_input_field;
    public TMP_Text tolerance_input_field;
    public TMP_Text adjustment_rate_input_field;
    public TMP_Text pressure_interval_input_field;
    public TMP_Text absolute_pressure_percentage_input_field;
    public Slider main_pressure_cuff_slider;
    public Toggle semideflate_toggle;
    public Log log;

    public DateTime start;
    public DateTime now;

    public void Start() {
        start = System.DateTime.Now;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        string current_date = System.DateTime.Now.ToString("dd-MM-yyyy");
        dir_path = Application.streamingAssetsPath + "/output/" + current_date + "/";
        if (!Directory.Exists(dir_path)) {
            Directory.CreateDirectory(dir_path);
            Debug.Log("\"output\"" + current_date + "\" Directory created");
        }
        Log.log_file = dir_path + "event_log.txt";

        if (!(Main.participant_nr > 0)) {
            log.Flush(3);
            log.Event("Application launched");
        }

        ESP32_IP_input_field_object.text = Main.ESP32_IP_address;
        participant_input_field_object.text = saved_participant_nr;
        main_pressure_cuff_slider.value = Main.main_pressure_cuff;
        if (Main.semideflate)
            semideflate_toggle.SetIsOnWithoutNotify(true);

        Debug.Log(Application.streamingAssetsPath);
        Debug.Log(Application.dataPath);
    }

    public void StartTrial(bool explore) {
        saved_participant_nr = participant_input_field.text[0..^1];
        int participant = int.Parse(participant_input_field.text[0..^1]);
        Debug.Log("CuffPos: " + ((participant - 1) % 6) + "\nRest: " + ((3*(participant - 1)) % 54));
        Debug.Log(ESP32_IP_input_field.text + " & " + participant_input_field.text);


        // Pass Variables
        Main.explore = explore;
        if (!explore)
            Main.participant_nr = participant;
        else {
            if (participant == 5)
                Main.participant_nr = 6;
            else
                Main.participant_nr = 5;
        }
        Main.ESP32_IP_address = ESP32_IP_input_field.text[0..^1];

        Main.cuff_pos_row = ((Main.participant_nr - 1) % 6);
        Main.other_ivs_row = ((3*(Main.participant_nr + (int)Mathf.Floor((Main.participant_nr-1)/6) - 1)) % 54);
        Main.condition_index = 0;
        if (!explore)
            Main.passes = (int) repetitions_slider.value + 1;

        Main.get_pressure_delay = float.Parse(get_pressure_delay_input_field.text[0..^1])/1000;
        Main.tolerance = int.Parse(tolerance_input_field.text[0..^1]);
        Main.adjustment_rate = int.Parse(adjustment_rate_input_field.text[0..^1]);
        Main.Pressure_interval = int.Parse(pressure_interval_input_field.text[0..^1]);
        Main.absolute_pressure_percentage = float.Parse(absolute_pressure_percentage_input_field.text[0..^1])/100;
        Main.main_pressure_cuff = (int) main_pressure_cuff_slider.value;
        Main.semideflate = false;
        if (semideflate_toggle.isOn)
            Main.semideflate = true;


        if (!explore) {
            // Create Files
            string current_time = System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

            Main.measurements_file = dir_path + "P" + participant + "_measurements_" + current_time + ".csv";
            Main.surveys_file = dir_path + "P" + participant + "_surveys_" + current_time + ".csv";
            Questionnaire.questionnaire_file = dir_path + "P" + participant + "_questionnaire_" + current_time + ".csv";

            File.WriteAllText(Main.measurements_file, "Participant,CuffPos,Pressure,Scale,Target,_MaxPressure,Accuracy,TCT,Crossings,_PressureDiff,Jitter,Retry");
            File.WriteAllText(Main.surveys_file, "Participant,CuffPos,Pressure,Scale,Convenience,Physical Demand,Mental Demand");
            File.WriteAllText(Questionnaire.questionnaire_file, "Participant,Passes,"
                                                                + "TotalDuration,ExplorationStart,ExplorationStop,ExplorationDuration,TrialStart,TrialStop,TrialDuration,PostQuestStart,PostQuestStop,PostQuestDuration,"

                                                                + "Age,Gender,"
                                                                + "Handedness,UpperArmCirc,ForearmCirc,"
                                                                + "Sport,Kraftsport,Ausdauersport,SportPerWeek,"

                                                                + "EasyToUnderstand,EnjoyableToUse,ReallyLiked,DidntLike,"
                                                                + "bestCuffPos,mediumCuffPos,worstCuffPos,CuffPosRemarks,"
                                                                + "bestPressure,mediumPressure,worstPressure,PressureRemarks,"
                                                                + "bestScaleRes,mediumScaleRes,worstScaleRes,ScaleResRemarks,"
                                                                + "FurtherCombinationRemarks,"

                                                                + "QuickToUse,QuickToUseRemarks,AccurateToUse,AccurateToUseRemarks,"
                                                                + "UsabilityChanges,"
                                                                + "WantToUse,WantToUseRemarks,"
                                                                + "WhereUsed,AdditionalRemarks");
        }


        SceneManager.LoadScene("Scales");
    }

    public void StartPostQuestionnaire() {
        SceneManager.LoadScene("Questionnaire");
    }

    public void QuitApp() {
        now = System.DateTime.Now;
        TimeSpan diff = now - start;
        Debug.Log(start.ToString() + " - " + now.ToString() + " = " + diff + "min (" + (now - start).TotalMinutes + ")");

    
        log.Event("Application quit");
        Debug.Log("Quit Application!");
        Application.Quit();
    }
}
