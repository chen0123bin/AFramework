using System;
namespace LWUI
{
    public class UILogicAttribute : Attribute
    {
        public readonly string m_Logic;
        /// <summary>
        /// 界面对象
        /// </summary>
        /// <param name="p_Logic">查找的路径</param>
        /// <param name="p_ResPath">默认的资源主要是图片</param>
        public UILogicAttribute(string p_Logic)
        {
            this.m_Logic = p_Logic;
        }
    }

}
