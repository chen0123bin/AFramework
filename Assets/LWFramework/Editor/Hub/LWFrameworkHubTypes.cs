#if UNITY_EDITOR
using System;
using UnityEngine;

namespace LWCore.Editor
{
    /// <summary>
    /// Hub 树节点类型。
    /// </summary>
    internal enum ItemKind
    {
        Folder = 0,
        MenuItem = 1,
        Asset = 2,
    }

    /// <summary>
    /// Hub 树节点负载数据：用于右侧面板决定显示与执行行为。
    /// </summary>
    [Serializable]
    internal sealed class ItemPayload
    {
        public ItemKind Kind;
        public string MenuPath;
        public UnityEngine.Object Asset;
    }
}
#endif
