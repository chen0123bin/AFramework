using LWUI;
using UnityEngine.UI;
using UnityEngine;
using LWCore;
using System;
using System.Collections.Generic;
using LWStep;

[UIViewData("Assets/0Res/Prefabs/UI/StepView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class StepView : BaseUIView
{
    internal static readonly string EVENT_CLOSE = "StepView_Close";
    internal static readonly string EVENT_PREV = "StepView_Prev";
    internal static readonly string EVENT_NEXT = "StepView_Next";
    internal static readonly string EVENT_JUMP = "StepView_Jump";

    [UIElement("PnlLeft/PnlStepList/Viewport/Content/PnlStepItem")]
    private Transform m_PnlStepItem;
    [UIElement("BtnPrev")]
    private Button m_BtnPrev;
    [UIElement("BtnNext")]
    private Button m_BtnNext;
    [UIElement("BtnBack")]
    private Button m_BtnBack;
    private GameObjectPool<StepItem> m_StepItemPool;
    private Dictionary<string, StepItem> m_NodeItemMap;
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        m_BtnPrev.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_PREV);
        });

        m_BtnNext.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_NEXT);
        });

        m_BtnBack.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });
        m_StepItemPool = new GameObjectPool<StepItem>(5, m_PnlStepItem.gameObject);
        m_NodeItemMap = new Dictionary<string, StepItem>();
    }



    public override void OpenView(object data = null)
    {
        base.OpenView(data);
        List<StepNode> nodeList = data as List<StepNode>;
        if (nodeList != null && nodeList.Count > 0)
        {
            m_NodeItemMap.Clear();
            for (int i = 0; i < nodeList.Count; i++)
            {
                int index = i;
                StepNode node = nodeList[i];
                StepItem item = m_StepItemPool.Spawn();
                item.StepIndex = (i + 1).ToString();
                item.StepTitle = node.Name;
                item.StepStatus = node.Status;
                item.OnClickStep = () =>
                {
                    ManagerUtility.EventMgr.DispatchEvent(EVENT_JUMP, node.Id);
                };
                if (!m_NodeItemMap.ContainsKey(node.Id))
                {
                    m_NodeItemMap.Add(node.Id, item);
                }
            }
        }


    }

    public void UpdateNodeStatus(string nodeId, StepNodeStatus status)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return;
        }
        StepItem item;
        if (m_NodeItemMap != null && m_NodeItemMap.TryGetValue(nodeId, out item) && item != null)
        {
            item.StepStatus = status;
        }
    }
}
