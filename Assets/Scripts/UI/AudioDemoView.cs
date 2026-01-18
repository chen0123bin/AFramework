using LWCore;
using LWUI;
using UnityEngine;
using UnityEngine.UI;

[UIViewData("Assets/0Res/Prefabs/UI/AudioDemoView.prefab", (int)FindType.Name, "LWFramework/Canvas/Normal")]
public class AudioDemoView : BaseUIView
{
    public const string EVENT_CLOSE = "CloseAudioDemoView";
    public const string EVENT_AUDIO_LOAD_CLIP = "AudioDemoView.AudioLoadClip";
    public const string EVENT_AUDIO_PLAY_LOOP = "AudioDemoView.AudioPlayLoop";
    public const string EVENT_AUDIO_PLAY_ONCE = "AudioDemoView.AudioPlayOnce";
    public const string EVENT_AUDIO_PAUSE = "AudioDemoView.AudioPause";
    public const string EVENT_AUDIO_RESUME = "AudioDemoView.AudioResume";
    public const string EVENT_AUDIO_STOP = "AudioDemoView.AudioStop";
    public const string EVENT_AUDIO_STOP_IMMEDIATE = "AudioDemoView.AudioStopImmediate";
    public const string EVENT_AUDIO_VOLUME_100 = "AudioDemoView.AudioVolume100";
    public const string EVENT_AUDIO_VOLUME_50 = "AudioDemoView.AudioVolume50";
    public const string EVENT_AUDIO_VOLUME_0 = "AudioDemoView.AudioVolume0";
    public const string EVENT_AUDIO_PAUSE_ALL = "AudioDemoView.AudioPauseAll";
    public const string EVENT_AUDIO_RESUME_ALL = "AudioDemoView.AudioResumeAll";
    public const string EVENT_AUDIO_STOP_ALL = "AudioDemoView.AudioStopAll";

    [UIElement("PnlTop/BtnClose")]
    private Button m_BtnClose;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioLoadClip")]
    private Button m_BtnAudioLoadClip;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioPlayLoop")]
    private Button m_BtnAudioPlayLoop;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioPlayOnce")]
    private Button m_BtnAudioPlayOnce;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioPause")]
    private Button m_BtnAudioPause;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioResume")]
    private Button m_BtnAudioResume;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioStop")]
    private Button m_BtnAudioStop;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioStopImmediate")]
    private Button m_BtnAudioStopImmediate;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioVolume100")]
    private Button m_BtnAudioVolume100;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioVolume50")]
    private Button m_BtnAudioVolume50;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioVolume0")]
    private Button m_BtnAudioVolume0;

    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioPauseAll")]
    private Button m_BtnAudioPauseAll;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioResumeAll")]
    private Button m_BtnAudioResumeAll;
    [UIElement("PnlCard/ScrActions/Viewport/Content/BtnAudioStopAll")]
    private Button m_BtnAudioStopAll;

    /// <summary>
    /// 创建并初始化音频演示界面：将按钮点击转为事件派发。
    /// </summary>
    /// <param name="gameObject">界面实例对象</param>
    public override void CreateView(GameObject gameObject)
    {
        base.CreateView(gameObject);

        m_BtnClose.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_CLOSE);
        });

        m_BtnAudioLoadClip.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_LOAD_CLIP);
        });
        m_BtnAudioPlayLoop.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_PLAY_LOOP);
        });
        m_BtnAudioPlayOnce.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_PLAY_ONCE);
        });

        m_BtnAudioPause.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_PAUSE);
        });
        m_BtnAudioResume.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_RESUME);
        });
        m_BtnAudioStop.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_STOP);
        });
        m_BtnAudioStopImmediate.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_STOP_IMMEDIATE);
        });

        m_BtnAudioVolume100.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_VOLUME_100);
        });
        m_BtnAudioVolume50.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_VOLUME_50);
        });
        m_BtnAudioVolume0.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_VOLUME_0);
        });

        m_BtnAudioPauseAll.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_PAUSE_ALL);
        });
        m_BtnAudioResumeAll.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_RESUME_ALL);
        });
        m_BtnAudioStopAll.onClick.AddListener(() =>
        {
            ManagerUtility.EventMgr.DispatchEvent(EVENT_AUDIO_STOP_ALL);
        });
    }
}

