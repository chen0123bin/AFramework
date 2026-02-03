using LWUI;
using UnityEngine.UI;
using UnityEngine;
using System;
using LWStep;

public class StepItem : BaseUIItem
{
	[UIElement("TxtStepIndex")]
	private Text m_TxtStepIndex;
	[UIElement("TxtStepTitle")]
	private Text m_TxtStepTitle;


	private Image m_ImgStepStatus;
	private Button m_BtnStep;

	private StepNodeStatus m_StepStatus;
	public string StepIndex
	{
		get { return m_TxtStepIndex.text; }
		set { m_TxtStepIndex.text = value; }
	}
	public string StepTitle
	{
		get { return m_TxtStepTitle.text; }
		set { m_TxtStepTitle.text = value; }
	}
	public StepNodeStatus StepStatus
	{
		get { return m_StepStatus; }
		set
		{
			m_StepStatus = value;
			switch (m_StepStatus)
			{
				case StepNodeStatus.Unfinished:
					m_ImgStepStatus.color = Color.white;
					break;
				case StepNodeStatus.Running:
					m_ImgStepStatus.color = Color.yellow;
					break;
				case StepNodeStatus.Completed:
					m_ImgStepStatus.color = Color.green;
					break;
			}
		}
	}

	public Action OnClickStep;
	public override void Create(GameObject gameObject)
	{
		base.Create(gameObject);
		m_BtnStep = gameObject.GetComponent<Button>();
		m_ImgStepStatus = gameObject.GetComponent<Image>();
		m_BtnStep.onClick.AddListener(() =>
		{
			OnClickStep?.Invoke();
		});
	}
	public override void OnUnSpawn()
	{
		base.OnUnSpawn();
		StepStatus = StepNodeStatus.Unfinished;
	}
	public override void OnRelease()
	{
		base.OnRelease();
	}
}
