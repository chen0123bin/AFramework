using LWUI;
using UnityEngine.UI;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

[UIViewData("Assets/0Res/Prefabs/UI/DialogView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class DialogView : BaseUIView
{


	private UniTaskCompletionSource<bool> m_ResultCompletionSource;

	[UIElement("PnlCard/TxtTitle")]
	private Text m_TxtTitle;
	[UIElement("PnlCard/TxtSubTitle")]
	private Text m_TxtSubTitle;
	[UIElement("PnlCard/LytBtns/BtnConfirm")]
	private Button m_BtnConfirm;
	[UIElement("PnlCard/LytBtns/BtnCancel")]
	private Button m_BtnCancel;
	[UIElement("PnlCard/BtnClose")]
	private Button m_BtnClose;

	/// <summary>
	/// 创建并初始化弹窗界面：注册按钮点击回调。
	/// </summary>
	/// <param name="gameObject">界面实例对象</param>
	public override void CreateView(GameObject gameObject)
	{
		base.CreateView(gameObject);
		m_BtnConfirm.onClick.AddListener(() =>
		{
			CompleteAndClose(true);
		});

		m_BtnCancel.onClick.AddListener(() =>
		{
			CompleteAndClose(false);
		});

		m_BtnClose.onClick.AddListener(() =>
		{
			CompleteAndClose(false);
		});

	}

	/// <summary>
	/// 打开弹窗并异步等待用户选择（确认/取消）。
	/// </summary>
	/// <param name="title">标题</param>
	/// <param name="subTitle">副标题/内容</param>
	/// <param name="hasCancelButton">是否显示取消按钮（false 表示单按钮确认）</param>
	/// <param name="hasCloseButton">是否显示右上角关闭按钮</param>
	/// <returns>用户点击结果</returns>
	public UniTask<bool> ShowAsync(string title, string subTitle, bool hasCancelButton, bool hasCloseButton = true)
	{
		CancelPendingIfNeeded();

		ApplyContent(title, subTitle);
		ApplyButtons(hasCancelButton, hasCloseButton);

		m_ResultCompletionSource = new UniTaskCompletionSource<bool>();
		return m_ResultCompletionSource.Task;
	}

	/// <summary>
	/// 设置标题与内容文本（不改变按钮模式）。
	/// </summary>
	/// <param name="title">标题</param>
	/// <param name="subTitle">副标题/内容</param>
	public void SetContent(string title, string subTitle)
	{
		ApplyContent(title, subTitle);
	}

	/// <summary>
	/// 设置按钮显示模式（单按钮确认/双按钮确认取消），可选关闭按钮。
	/// </summary>
	/// <param name="hasCancelButton">是否显示取消按钮</param>
	/// <param name="hasCloseButton">是否显示关闭按钮</param>
	public void SetButtons(bool hasCancelButton, bool hasCloseButton = true)
	{
		ApplyButtons(hasCancelButton, hasCloseButton);
	}

	/// <summary>
	/// 关闭弹窗：若仍在等待结果，则默认返回取消。
	/// </summary>
	public override void CloseView()
	{
		CancelPendingIfNeeded();
		base.CloseView();
	}

	/// <summary>
	/// 写入标题/副标题，并按空内容自动隐藏副标题。
	/// </summary>
	/// <param name="title">标题</param>
	/// <param name="subTitle">副标题/内容</param>
	private void ApplyContent(string title, string subTitle)
	{
		if (m_TxtTitle != null)
		{
			m_TxtTitle.text = title != null ? title : string.Empty;
		}

		if (m_TxtSubTitle != null)
		{
			string safeSubTitle = subTitle != null ? subTitle : string.Empty;
			m_TxtSubTitle.text = safeSubTitle;
			m_TxtSubTitle.gameObject.SetActive(!string.IsNullOrEmpty(safeSubTitle));
		}
	}

	/// <summary>
	/// 设置按钮可见性：单按钮时隐藏取消按钮；关闭按钮可选显示。
	/// </summary>
	/// <param name="hasCancelButton">是否显示取消按钮</param>
	/// <param name="hasCloseButton">是否显示关闭按钮</param>
	private void ApplyButtons(bool hasCancelButton, bool hasCloseButton)
	{
		if (m_BtnConfirm != null)
		{
			m_BtnConfirm.gameObject.SetActive(true);
		}

		if (m_BtnCancel != null)
		{
			m_BtnCancel.gameObject.SetActive(hasCancelButton);
		}

		if (m_BtnClose != null)
		{
			m_BtnClose.gameObject.SetActive(hasCloseButton);
		}
	}

	/// <summary>
	/// 完成等待并关闭弹窗。
	/// </summary>
	/// <param name="result">点击结果</param>
	private void CompleteAndClose(bool result)
	{
		UniTaskCompletionSource<bool> completionSource = m_ResultCompletionSource;
		m_ResultCompletionSource = null;
		if (completionSource != null)
		{
			completionSource.TrySetResult(result);
		}

		CloseSelf();
	}

	/// <summary>
	/// 若仍存在未完成的等待，则用取消结果结束等待并清空。
	/// </summary>
	private void CancelPendingIfNeeded()
	{
		UniTaskCompletionSource<bool> completionSource = m_ResultCompletionSource;
		m_ResultCompletionSource = null;
		if (completionSource != null)
		{
			completionSource.TrySetResult(false);
		}
	}
}
