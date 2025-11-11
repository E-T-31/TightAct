using UnityEngine;
using System.IO;

public class Log : MonoBehaviour
{
    public static string log_file = Application.streamingAssetsPath + "/output/event_log.txt";
    //public static string log_file = MenuMain.dir_path + "event_log.txt";

    public void Event(string name) {
        File.AppendAllText(log_file, "\n" + System.DateTime.Now.ToString() + ": " + name);
    }

    public void Flush(int lines) {
        for (int i = 0; i < lines; i++)
            File.AppendAllText(log_file, "\n");
    }
}
