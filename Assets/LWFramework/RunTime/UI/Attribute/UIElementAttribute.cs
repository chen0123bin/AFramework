using System;
namespace LWUI
{
    public enum UIElementType
    {
        Common, Style
    }
    public class UIElementAttribute : Attribute
    {
        public readonly string m_RootPath;
        public readonly string m_ResPath;
        public readonly UIElementType m_Type = UIElementType.Common;
        public readonly string m_Style;
        /// <summary>
        /// 界面对象
        /// </summary>
        /// <param name="p_RootPath">查找的路径</param>
        /// <param name="p_ResPath">默认的资源主要是图片</param>
        public UIElementAttribute(string p_RootPath, string p_ResPath)
        {
            this.m_RootPath = p_RootPath;
            this.m_ResPath = p_ResPath;
        }
        /// <summary>
        /// 界面对象
        /// </summary>
        /// <param name="p_RootPath">查找的路径</param>
        public UIElementAttribute(string p_RootPath)
        {
            this.m_RootPath = p_RootPath;
        }
        /// <summary>
        /// 界面对象
        /// </summary>
        /// <param name="p_RootPath">查找的路径</param>
        /// <param name="p_Type">类型0-通用 1-样式</param>
        /// <param name="p_Style">样式名称</param>
        public UIElementAttribute(string p_RootPath, int p_Type, string p_Style)
        {
            this.m_RootPath = p_RootPath;
            this.m_Type = (UIElementType)p_Type;
            this.m_Style = p_Style;
        }
    }

}
