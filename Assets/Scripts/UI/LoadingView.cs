using LWUI;
using UnityEngine.UI;
using UnityEngine;
using LWFramework;
using System.Collections.Generic;

[UIViewData("Assets/0Res/Prefabs/UI/LoadingView.prefab", (int)FindType.Name, "LWFramework/Canvas/Top")]
public class LoadingView : BaseUIView
{

	[UIElement("SldProgress")]
	private Slider m_SldProgress;
	[UIElement("TxtProgress")]
	private Text m_TxtProgress;
	[UIElement("TxtTip")]
	private Text m_TxtTip;

	/// <summary>
	/// 加载提示
	/// </summary>
	public string Tip
	{
		get { return m_TxtTip.text; }
		set { m_TxtTip.text = value; }
	}
	/// <summary>
	/// 加载进度
	/// </summary>
	public float Progress
	{
		get { return m_SldProgress.value; }
		set
		{
			m_SldProgress.value = value;
			m_TxtProgress.text = $"{(int)(value * 100)}%";
		}
	}
	public override void CreateView(GameObject gameObject)
	{
		base.CreateView(gameObject);
	}
}
