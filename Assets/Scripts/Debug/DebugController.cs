using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugController : MonoBehaviour
{
    // Debugログを表示するかどうかを制御するフラグ
    public static bool DebugLogEnabled = true;

    // どこからでも使えるようにするため、静的なメソッドを作る
    public static void Log(string message)
    {
        if (DebugLogEnabled)
        {
            Debug.Log(message);  // ログを表示
        }
    }
    public static void LogVil(string message,int vilnum)
    {

        if (DebugLogEnabled)
        {
            Debug.Log(message+"--"+vilnum.ToString());  // ログを表示
        }
    }
}
