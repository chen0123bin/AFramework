#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace LWCore.Editor
{
    /// <summary>
    /// Hub 右侧页面基类：通过继承并重写 DrawContent 绘制交互内容。
    /// </summary>
    public abstract class BaseHubTreeView
    {
        private readonly string m_NodePath;
        private readonly string m_IconPath;
        private Texture2D m_Icon;

        /// <summary>
        /// 创建 Hub 页面。
        /// </summary>
        /// <param name="nodePath">左侧树节点路径，例如："Event/EventMonitor"。</param>
        /// <param name="iconPath">图标资源路径（Assets/...png），可为空。</param>
        protected BaseHubTreeView(string nodePath, string iconPath)
        {
            m_NodePath = nodePath;
            m_IconPath = iconPath;
        }

        /// <summary>
        /// 获取左侧树节点路径。
        /// </summary>
        public string NodePath => m_NodePath;

        /// <summary>
        /// 获取图标（首次访问时按路径加载）。
        /// </summary>
        public Texture2D Icon
        {
            get
            {
                if (m_Icon != null)
                {
                    return m_Icon;
                }
                if (string.IsNullOrEmpty(m_IconPath))
                {
                    return null;
                }
                m_Icon = AssetDatabase.LoadAssetAtPath<Texture2D>(m_IconPath);
                if (m_Icon == null)
                {
                    m_Icon = Resources.Load<Texture2D>(m_IconPath);
                }
                return m_Icon;
            }
        }

        /// <summary>
        /// 页面被选中时回调（可用于初始化/刷新）。
        /// </summary>
        public virtual void OnSelected()
        {
        }

        /// <summary>
        /// 页面被取消选中时回调（可用于释放临时资源）。
        /// </summary>
        public virtual void OnDeselected()
        {
        }

        /// <summary>
        /// 绘制右侧内容区入口（由窗口调用）。
        /// </summary>
        public void OnContentGUI()
        {
            DrawContent();
        }

        /// <summary>
        /// 绘制右侧交互内容。
        /// </summary>
        protected virtual void DrawContent()
        {
        }
    }
}
#endif
