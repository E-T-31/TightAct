using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using System.IO;
using System;
using TMPro;

public class Questionnaire : MonoBehaviour
{
    public static string questionnaire_file = Application.streamingAssetsPath + "/output/Test_questionnaire.csv";

    //-// Time Frame
    public static DateTime? Exploration_start;
    public static DateTime? Exploration_stop;
    public static DateTime? Trial_start;
    public static DateTime? Trial_stop;
    public static DateTime? PostQuest_start;
    public static DateTime? PostQuest_stop;

    //-// PostQuest: Today
    public GameObject PostQuest_Today_survey_panel;
    public GameObject PostQuest_Today_AttentionMarkers;
    public TMP_Text Base_Pressures_Question;
    // Values
    public Slider Easiness_Likert;
    public Slider Enjoyability_Likert;
    public TMP_Text ReallyLiked_Text;
    public TMP_Text DidntLike_Text;
    public TMP_Text Cuff_Positions_Ranking_best;
    public TMP_Text Cuff_Positions_Ranking_medium;
    public TMP_Text Cuff_Positions_Ranking_worst;
    public TMP_Text Cuff_Positions_Ranking_Remarks_Text;
    public TMP_Text Base_Pressures_Ranking_best;
    public TMP_Text Base_Pressures_Ranking_medium;
    public TMP_Text Base_Pressures_Ranking_worst;
    public TMP_Text Base_Pressures_Ranking_Remarks_Text;
    public TMP_Text Scale_Resolution_Ranking_best;
    public TMP_Text Scale_Resolution_Ranking_medium;
    public TMP_Text Scale_Resolution_Ranking_worst;
    public TMP_Text Scale_Resolution_Ranking_Remarks_Text;
    public TMP_Text Further_Combination_Remarks_Text;

    //-// PostQuest: Future
    public GameObject PostQuest_Future_info_panel;
    public GameObject PostQuest_Future_survey_panel;
    public GameObject PostQuest_Future_AttentionMarkers;
    // Values
    public Slider Quickness_Likert;
    public TMP_Text Quickness_Likert_Remarks_Text;
    public Slider Accuracy_Likert;
    public TMP_Text Accuracy_Likert_Remarks_Text;
    public TMP_Text Usability_Changes_Text;
    public Slider WantToUse_Likert;
    public TMP_Text WantToUse_Likert_Remarks_Text;
    public TMP_Text WhereUsed_Text;
    public TMP_Text Additional_Remarks_Text;

    //-// Demographics
    public GameObject Demographics_info_panel;
    public GameObject Demographics_survey_panel;
    public GameObject Demographics_AttentionMarkers;
    // Values
    public TMP_Text Age_Text;
    public TMP_Text Gender_Dropdown;
    public TMP_Text Handedness_Dropdown;
    public TMP_Text UpperArm_Circumference_Text;
    public TMP_Text Forearm_Circumference_Text;
    public Slider Sport_Likert;
    public Slider Kraftsport_Likert;
    public Slider Ausdauersport_Likert;
    public TMP_Text SportPerWeek_Text;

    //-// Finished
    public GameObject Finished_Survey_panel;

    public GameObject WrongAnswerInfo;
    public Log log;


    // Start is called before the first frame update
    void Start()
    {
        PostQuest_start = System.DateTime.Now;
        log.Event("P" + Main.participant_nr + " – Post Questionnaire started");
        Base_Pressures_Question.text = "6. Rank the three Base Pressure Levels " + 1*Main.Pressure_interval + " (0),\n" + 2*Main.Pressure_interval + " (1) and " + 3*Main.Pressure_interval + " (2) from best to worst.";
    }

    public void nextFromToday() {
        int passed_conditions = 0;

        //Debug.Log(Really_Liked_Text.text.Length + ">"+ Really_Liked_Text.text + "< == " + Really_Liked_Text.text[0..^1].Length + ">" + Really_Liked_Text.text[0..^1] + "<");
        if (Easiness_Likert.value <= 7) {
            passed_conditions++;
            todayMarker(0, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Today #1' before continuing...");
            todayMarker(0, "!");
        }
        if (Enjoyability_Likert.value <= 7) {
            passed_conditions++;
            todayMarker(1, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Today #2' before continuing...");
            todayMarker(1, "!");
        }
        if (ReallyLiked_Text.text.Length > 1) {
            passed_conditions++;
            todayMarker(2, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Today #3' before continuing...");
            todayMarker(2, "!");
        }
        if (DidntLike_Text.text.Length > 1) {
            passed_conditions++;
            todayMarker(3, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Today #4' before continuing...");
            todayMarker(3, "!");
        }
        if (Cuff_Positions_Ranking_best.text.Length == 2 && Cuff_Positions_Ranking_medium.text.Length == 2 && Cuff_Positions_Ranking_worst.text.Length == 2
        && (Cuff_Positions_Ranking_best.text != Cuff_Positions_Ranking_medium.text) && (Cuff_Positions_Ranking_best.text != Cuff_Positions_Ranking_worst.text) && (Cuff_Positions_Ranking_medium.text != Cuff_Positions_Ranking_worst.text)
        && (int.Parse(Cuff_Positions_Ranking_best.text[0..^1]) + int.Parse(Cuff_Positions_Ranking_medium.text[0..^1]) + int.Parse(Cuff_Positions_Ranking_worst.text[0..^1])) == 3) {
            passed_conditions++;
            todayMarker(4, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Today #5' isn't right...");
            todayMarker(4, "!");
        }
        if (Base_Pressures_Ranking_best.text.Length == 2 && Base_Pressures_Ranking_medium.text.Length == 2 && Base_Pressures_Ranking_worst.text.Length == 2
        && (Base_Pressures_Ranking_best.text != Base_Pressures_Ranking_medium.text) && (Base_Pressures_Ranking_best.text != Base_Pressures_Ranking_worst.text) && (Base_Pressures_Ranking_medium.text != Base_Pressures_Ranking_worst.text)
        && (int.Parse(Base_Pressures_Ranking_best.text[0..^1]) + int.Parse(Base_Pressures_Ranking_medium.text[0..^1]) + int.Parse(Base_Pressures_Ranking_worst.text[0..^1])) == 3) {
            passed_conditions++;
            todayMarker(5, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Today #6' isn't right...");
            todayMarker(5, "!");
        }
        if (Scale_Resolution_Ranking_best.text.Length == 2 && Scale_Resolution_Ranking_medium.text.Length == 2 && Scale_Resolution_Ranking_worst.text.Length == 2
        && (Scale_Resolution_Ranking_best.text != Scale_Resolution_Ranking_medium.text) && (Scale_Resolution_Ranking_best.text != Scale_Resolution_Ranking_worst.text) && (Scale_Resolution_Ranking_medium.text != Scale_Resolution_Ranking_worst.text)
        && (int.Parse(Scale_Resolution_Ranking_best.text[0..^1]) + int.Parse(Scale_Resolution_Ranking_medium.text[0..^1]) + int.Parse(Scale_Resolution_Ranking_worst.text[0..^1])) == 3) {
            passed_conditions++;
            todayMarker(6, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Today #7' isn't right...");
            todayMarker(6, "!");
        }
        
        WrongAnswerInfo.SetActive(true);
        if (passed_conditions >= 7) {
            PostQuest_Today_survey_panel.SetActive(false);
            PostQuest_Future_info_panel.SetActive(true);
            WrongAnswerInfo.SetActive(false);
        }
    }

    public void nextFromFuture() {
        int passed_conditions = 0;

        if (Quickness_Likert.value <= 7) {
            passed_conditions++;
            futureMarker(0, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Future #1' before continuing...");
            futureMarker(0, "!");
        }
        if (Accuracy_Likert.value <= 7) {
            passed_conditions++;
            futureMarker(1, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Future #2' before continuing...");
            futureMarker(1, "!");
        }
        if (Usability_Changes_Text.text.Length > 1) {
            passed_conditions++;
            futureMarker(2, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Future #3' before continuing...");
            futureMarker(2, "!");
        }
        if (WantToUse_Likert.value <= 7) {
            passed_conditions++;
            futureMarker(3, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Future #4' before continuing...");
            futureMarker(3, "!");
        }
        if (WhereUsed_Text.text.Length > 1) {
            passed_conditions++;
            futureMarker(4, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Future #5' before continuing...");
            futureMarker(4, "!");
        }
        
        WrongAnswerInfo.SetActive(true);
        if (passed_conditions >= 5) {
            PostQuest_Future_survey_panel.SetActive(false);
            Demographics_info_panel.SetActive(true);
            WrongAnswerInfo.SetActive(false);
        }
    }

    public void nextFromDemographics() {
        int passed_conditions = 0;

        if (Age_Text.text.Length > 2) {
            passed_conditions++;
            demographicsMarker(0, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Demographics #1' isn't right...");
            demographicsMarker(0, "!");
        }
        if (!(Gender_Dropdown.text == "<i><color=#bebebe><unset>")) {
            passed_conditions++;
            demographicsMarker(1, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #2' before continuing...");
            demographicsMarker(1, "!");
        }
        if (!(Handedness_Dropdown.text == "<i><color=#bebebe><unset>")) {
            passed_conditions++;
            demographicsMarker(2, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #3' before continuing...");
            demographicsMarker(2, "!");
        }
        if (UpperArm_Circumference_Text.text.Length > 2) {
            passed_conditions++;
            demographicsMarker(3, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Demographics #3a' isn't right...");
            demographicsMarker(3, "!");
        }
        if (Forearm_Circumference_Text.text.Length > 2) {
            passed_conditions++;
            demographicsMarker(4, "*");
        } else {
            Debug.Log("Something about your Answer to Question 'Demographics #3b' isn't right...");
            demographicsMarker(4, "!");
        }
        if (Sport_Likert.value <= 7) {
            passed_conditions++;
            demographicsMarker(5, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #4' before continuing...");
            demographicsMarker(5, "!");
        }
        if (Kraftsport_Likert.value <= 7) {
            passed_conditions++;
            demographicsMarker(6, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #5' before continuing...");
            demographicsMarker(6, "!");
        }
        if (Ausdauersport_Likert.value <= 7) {
            passed_conditions++;
            demographicsMarker(7, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #6' before continuing...");
            demographicsMarker(7, "!");
        }
        if (SportPerWeek_Text.text.Length > 1) {
            passed_conditions++;
            demographicsMarker(8, "*");
        } else {
            Debug.Log("Please give an Answer to Question 'Demographics #7' before continuing...");
            demographicsMarker(8, "!");
        }
        
        WrongAnswerInfo.SetActive(true);
        if (passed_conditions >= 9) {
            Demographics_survey_panel.SetActive(false);
            Finished_Survey_panel.SetActive(true);
            WrongAnswerInfo.SetActive(false);
        }
    }

    public void saveDataAndBackToMainMenu() {
        PostQuest_stop = System.DateTime.Now;
        log.Event("P" + Main.participant_nr + " – Post Questionnaire finished");
        log.Flush(1);

        double Exploration_duration = -1f;
        if (Exploration_start != null && Exploration_stop != null)
            Exploration_duration = ((DateTime)Exploration_stop - (DateTime)Exploration_start).TotalMinutes;
        double Trial_duration = -1f;
        if (Trial_start != null && Trial_stop != null)
            Trial_duration = ((DateTime)Trial_stop - (DateTime)Trial_start).TotalMinutes;
        double PostQuest_duration = -1f;
        if (PostQuest_start != null && PostQuest_stop != null)
            PostQuest_duration = ((DateTime)PostQuest_stop - (DateTime)PostQuest_start).TotalMinutes;

        File.AppendAllText(questionnaire_file, "\n" + Main.participant_nr + "," + Main.passes + ","

                                                    + "???" + ",\""
                                                    + Exploration_start.ToString() + "\",\"" + Exploration_stop.ToString() + "\",\"" + Exploration_duration + "\",\""
                                                    + Trial_start.ToString() + "\",\"" + Trial_stop.ToString() + "\",\"" + Trial_duration + "\",\""
                                                    + PostQuest_start.ToString() + "\",\"" + PostQuest_stop.ToString() + "\",\"" + PostQuest_duration + "\","
        
                                                    + Age_Text.text[0..^1] + ",\"" + Gender_Dropdown.text + "\",\""
                                                    + Handedness_Dropdown.text.Split('-')[0] + "\",\"" + UpperArm_Circumference_Text.text[0..^1] + "\",\"" + Forearm_Circumference_Text.text[0..^1] + "\","
                                                    + Mathf.Clamp(Sport_Likert.value,1,7) + "," + Mathf.Clamp(Kraftsport_Likert.value,1,7) + "," + Mathf.Clamp(Ausdauersport_Likert.value,1,7) + ",\"" + SportPerWeek_Text.text[0..^1] + "\","

                                                    + Mathf.Clamp(Easiness_Likert.value,1,7) + "," + Mathf.Clamp(Enjoyability_Likert.value,1,7) + ",\"" + ReallyLiked_Text.text[0..^1] + "\",\"" + DidntLike_Text.text[0..^1] + "\","
                                                    + Cuff_Positions_Ranking_best.text[0] + "," + Cuff_Positions_Ranking_medium.text[0] + "," + Cuff_Positions_Ranking_worst.text[0] + ",\"" + Cuff_Positions_Ranking_Remarks_Text.text[0..^1] + "\","
                                                    + Base_Pressures_Ranking_best.text[0] + "," + Base_Pressures_Ranking_medium.text[0] + "," + Base_Pressures_Ranking_worst.text[0] + ",\"" + Base_Pressures_Ranking_Remarks_Text.text[0..^1] + "\","
                                                    + Scale_Resolution_Ranking_best.text[0] + "," + Scale_Resolution_Ranking_medium.text[0] + "," + Scale_Resolution_Ranking_worst.text[0] + ",\"" + Scale_Resolution_Ranking_Remarks_Text.text[0..^1] + "\",\""
                                                    + Further_Combination_Remarks_Text.text[0..^1] + "\","

                                                    + Mathf.Clamp(Quickness_Likert.value,1,7) + ",\"" + Quickness_Likert_Remarks_Text.text[0..^1] + "\"," + Mathf.Clamp(Accuracy_Likert.value,1,7) + ",\"" + Accuracy_Likert_Remarks_Text.text[0..^1] + "\",\""
                                                    + Usability_Changes_Text.text[0..^1] + "\","
                                                    + Mathf.Clamp(WantToUse_Likert.value,1,7) + ",\"" + WantToUse_Likert_Remarks_Text.text[0..^1] + "\",\""
                                                    + WhereUsed_Text.text[0..^1] + "\",\"" + Additional_Remarks_Text.text[0..^1] + "\"");

        MenuMain.saved_participant_nr = "";
        Exploration_start = null;
        Exploration_stop = null;
        Trial_start = null;
        Trial_stop = null;
        PostQuest_start = null;
        PostQuest_stop = null;

        SceneManager.LoadScene("MainMenu");
    }

    //public void printDropdownStatus() {
    //    Debug.Log(Handedness_Dropdown.text.Split('-')[0].Length + ">" + Handedness_Dropdown.text.Split('-')[0] + "<");
    //}

    private void todayMarker(int index, string symbol) {
        PostQuest_Today_AttentionMarkers.transform.GetChild(index).GetComponent<TextMeshProUGUI>().text = symbol;
    }
    private void futureMarker(int index, string symbol) {
        PostQuest_Future_AttentionMarkers.transform.GetChild(index).GetComponent<TextMeshProUGUI>().text = symbol;
    }
    private void demographicsMarker(int index, string symbol) {
        Demographics_AttentionMarkers.transform.GetChild(index).GetComponent<TextMeshProUGUI>().text = symbol;
    }
}
