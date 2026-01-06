using System.Collections;
using System.Collections.Generic;
using LWCore;
using UnityEngine;
/*
*Creator:陈斌
*/
/// <summary>
/// 本地化工具
/// </summary>
namespace LWUI
{
    public class LocalizationUtility : Singleton<LocalizationUtility>
    {
        /// <summary>
        /// 本地化类型
        /// </summary>
        public LocalizationType LocalizationType { get; set; } = LocalizationType.zh;
        private LocalizationData m_LocalizationData;
        protected override void OnSingletonInit()
        {
            base.OnSingletonInit();
            TextAsset textAsset = ManagerUtility.AssetsMgr.LoadAsset<TextAsset>($"Assets/@Resources/Localization/Localization.txt");
            m_LocalizationData = LitJson.JsonMapper.ToObject<LocalizationData>(textAsset.text);
        }
        /// <summary>
        /// 通过key获取本地化文本 或者路径
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetStringByKey(string key)
        {
            string ret = "";
            LocalItem item = m_LocalizationData.localItemList.Find(find => key == find.localkey);

            switch (LocalizationType)
            {
                case LocalizationType.zh:
                    ret = item.localdata.zh;
                    break;
                case LocalizationType.en:
                    ret = item.localdata.en;
                    break;
                case LocalizationType.jp:
                    ret = item.localdata.jp;
                    break;
                case LocalizationType.ko:
                    ret = item.localdata.ko;
                    break;
                default:
                    break;
            }
            return ret;
        }
    }
}

