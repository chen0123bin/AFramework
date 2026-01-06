using LWCore;
using UnityEngine;
using UnityEngine.UI;
using LWUI;

[UIViewData("Assets/0Res/Prefabs/UI/TestView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class TestView : BaseUIView
{
    
    [UIElement("Button")]
    private Button m_Button;
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);
        m_Button.onClick.AddListener(() =>
        {
            LWDebug.Log("点击");
        });
    }
}
