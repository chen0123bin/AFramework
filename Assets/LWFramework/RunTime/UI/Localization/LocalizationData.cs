using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
*Creator:陈斌
*/
/// <summary>
/// 本地化数据
/// </summary>
namespace LWUI
{
    public class LocalizationData
    {
        /// <summary>
        /// 
        /// </summary>
        public List<LocalItem> localItemList { get; set; } = new List<LocalItem>();
    }


    public enum LocalizationType
    {
        zh, en, jp, ko
    }
}
