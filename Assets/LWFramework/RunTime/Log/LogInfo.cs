using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogInfo
{
    public LogType type;
    public string condition;
    public string stackTrace;
    public string nowDate;
    public bool isOpen;

    public LogInfo(LogType type, string condition, string stackTrace, string nowDate)
    {
        this.type = type;
        this.condition = condition;
        this.stackTrace = stackTrace;
        this.nowDate = nowDate;
        isOpen = false;
    }
    public override string ToString()
    {
        return $"[{nowDate}]   {type}:{condition} \n==>  {stackTrace}";
        //return nowDate + "   " + type.ToString() + "：" + condition + "    ==>：" + stackTrace;
    }
    public string GetCondition() {
        return $"[{nowDate}]   {type}:{condition}";
        //return nowDate + "   " + type.ToString() + "：" + condition ;
    }
}
