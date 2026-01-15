
using LWCore;
using UnityEngine;
using UnityEngine.UI;

namespace LWUI
{
    public class BaseUIView
    {
        /// <summary>
        /// UIGameObject
        /// </summary>
        protected GameObject m_Entity;
        protected CanvasGroup m_CanvasGroup;
        private bool m_IsOpen = false;
        /// <summary>
        /// 显示隐藏的处理方式
        /// </summary>
        private ShowHideType m_ShowHideType;
        public bool IsOpen
        {
            get => m_IsOpen;
            set => m_IsOpen = value;
        }
        public GameObject Entity { get => m_Entity; }
        public ShowHideType ShowHideType { get => m_ShowHideType; set => m_ShowHideType = value; }

        public virtual void CreateView(GameObject gameObject)
        {
            m_Entity = gameObject;
            //view上的组件
            if (ManagerUtility.UIMgr != null)
            {
                ManagerUtility.UIMgr.IUIUtility.SetViewElement(this, this.GetType(), this);
            }
            m_CanvasGroup = m_Entity.GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                LWDebug.LogError(string.Format("{0}上没有CanvasGroup这个组件", m_Entity.name));
            }

        }

        /// <summary>
        /// 打开view
        /// </summary>
        public virtual void OpenView(object data = null)
        {
            switch (m_ShowHideType)
            {
                case ShowHideType.GameObject:
                    m_Entity.SetActive(true);
                    break;
                case ShowHideType.CanvasGroup:
                    m_CanvasGroup.SetActive(true);
                    break;
                default:
                    break;
            }
            m_IsOpen = true;
        }
        /// <summary>
        ///关闭view 
        /// </summary>
        public virtual void CloseView()
        {
            // m_CanvasGroup.SetActive(false);
            if (!m_IsOpen || m_Entity == null)
            {
                return;
            }
            switch (m_ShowHideType)
            {
                case ShowHideType.GameObject:
                    m_Entity.SetActive(false);
                    break;
                case ShowHideType.CanvasGroup:
                    m_CanvasGroup.SetActive(false);
                    break;
                default:
                    break;
            }
            m_IsOpen = false;
        }
        /// <summary>
        /// 暂停view
        /// </summary>
        public virtual void OnPause()
        {
            m_CanvasGroup.interactable = false;
        }
        /// <summary>
        /// 恢复view
        /// </summary>
        public virtual void OnResume()
        {
            m_CanvasGroup.interactable = true;
        }

        //更新VIEW
        public virtual void UpdateView()
        {

        }
        //删除VIEW
        public virtual void ClearView()
        {
            Object.Destroy(m_Entity);
        }
        /// <summary>
        /// 设置view层级
        /// </summary>
        /// <param name="isLastSibling">是否置于最前 默认false</param>
        public virtual void SetViewLastSibling(bool isLastSibling = false)
        {
            if (isLastSibling)
            {
                m_Entity.transform.SetAsLastSibling();
            }
        }
        public virtual void ResetView()
        {

        }
        /// <summary>
        /// 关闭自身
        /// </summary>
        protected void CloseSelf()
        {
            ManagerUtility.UIMgr.CloseView(GetType().Name);
        }
        /// <summary>
        /// 处理本地化
        /// </summary>
        public void Localization()
        {
            TextAsset textAsset = ManagerUtility.AssetsMgr.LoadAsset<TextAsset>($"Assets/@Resources/Localization/{this.GetType()}.txt");
            LocalizationData localizationData = LitJson.JsonMapper.ToObject<LocalizationData>(textAsset.text);
            for (int i = 0; i < localizationData.localItemList.Count; i++)
            {
                LocalItem localItem = localizationData.localItemList[i];
                Transform child = m_Entity.transform.Find(localItem.localkey);
                if (child == null)
                {
                    LWDebug.LogWarning(localItem.localkey + " 不存在");
                    continue;
                }
                if (localItem.type == "text")
                {
#if TMPRO
                    TMPro.TMP_Text text = child.GetComponent<TMPro.TMP_Text>();

#else
                    Text text = child.GetComponent<Text>();
#endif

                    switch (LocalizationUtility.Instance.LocalizationType)
                    {
                        case LocalizationType.zh:
                            text.text = localItem.localdata.zh;
                            break;
                        case LocalizationType.en:
                            text.text = localItem.localdata.en;
                            break;
                        case LocalizationType.jp:
                            text.text = localItem.localdata.jp;
                            break;
                        case LocalizationType.ko:
                            text.text = localItem.localdata.ko;
                            break;
                        default:
                            break;
                    }

                }
                else if (localItem.type == "image")
                {
                    Image image = child.GetComponent<Image>();
                    switch (LocalizationUtility.Instance.LocalizationType)
                    {
                        case LocalizationType.zh:
                            image.sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(localItem.localdata.zh);
                            break;
                        case LocalizationType.en:
                            image.sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(localItem.localdata.en);
                            break;
                        case LocalizationType.jp:
                            image.sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(localItem.localdata.jp);
                            break;
                        case LocalizationType.ko:
                            image.sprite = ManagerUtility.AssetsMgr.LoadAsset<Sprite>(localItem.localdata.ko);
                            break;
                        default:
                            break;
                    }

                }
            }
        }
    }
}

