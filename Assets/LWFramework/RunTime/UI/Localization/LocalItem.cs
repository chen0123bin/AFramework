using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
*Creator:陈斌
*/
namespace LWUI
{
    public class LocalItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string localkey { get; set; }
        /// <summary>
        /// 类型：text-文字 imgae-图片
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public LocalData localdata { get; set; } = new LocalData();
    }

}
