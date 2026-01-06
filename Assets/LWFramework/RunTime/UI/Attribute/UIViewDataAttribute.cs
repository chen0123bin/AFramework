using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LWUI
{

    /// <summary>
    /// 查找对象的方式
    /// </summary>
    public enum FindType
    {
        Tag = 0,
        Name = 1
    }
    /// <summary>
    /// 显示隐藏的方式
    /// </summary>
    public enum ShowHideType
    {
        GameObject = 0, CanvasGroup = 1
    }
    public class UIViewDataAttribute : Attribute
    {
        public string m_LoadPath;
        public FindType m_FindType;
        public string m_Param;
        public ShowHideType m_ShowHideType = ShowHideType.GameObject;

        public UIViewDataAttribute(string p_LoadPath, int p_FindType, string p_Param)
        {
            this.m_LoadPath = p_LoadPath;
            this.m_FindType = (FindType)p_FindType;
            this.m_Param = p_Param;
        }
        public UIViewDataAttribute(string p_LoadPath, int p_FindType, string p_Param, int p_ShowHideType)
        {
            this.m_LoadPath = p_LoadPath;
            this.m_FindType = (FindType)p_FindType;
            this.m_Param = p_Param;
            this.m_ShowHideType = (ShowHideType)p_ShowHideType;
        }
    }
}


