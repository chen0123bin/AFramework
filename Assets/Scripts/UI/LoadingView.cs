using LWUI;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

[UIViewData("Assets/0Res/Prefabs/UI/LoadingView.prefab", (int)FindType.Name, "LWFramework/Canvas/Top")]
public class LoadingBarView : BaseUIView
{

	[UIElement("PnlCard/TxtTip")]
	private Text m_TxtTip;
	[UIElement("PnlCard/SldProgress")]
	private Slider m_SldProgress;
	[UIElement("PnlCard/TxtPercent")]
	private Text m_TxtPercent;
	[UIElement("PnlCard/TxtVersion")]
	private Text m_TxtVersion;

	/// <summary>
	/// 加载提示
	/// </summary>
	public string Tip
	{
		get { return m_TxtTip.text; }
		set { m_TxtTip.text = value; }
	}
	/// <summary>
	/// 版本信息
	/// </summary>
	public string Version
	{
		get { return m_TxtVersion.text; }
		set { m_TxtVersion.text = value; }
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
			m_TxtPercent.text = $"{(int)(value * 100)}%";
		}
	}
	public override void CreateView(GameObject gameObject)
	{
		base.CreateView(gameObject);
	}
}
