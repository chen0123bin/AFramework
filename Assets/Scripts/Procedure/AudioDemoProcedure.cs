using LWAudio;
using LWCore;
using LWFMS;
using LWUI;
using UnityEngine;

[FSMTypeAttribute("Procedure", false)]
public class AudioDemoProcedure : BaseFSMState
{
    private const string BGM_PATH = "Assets/0Res/Audios/bgm.wav";

    private AudioClip m_BgmClip;
    private AudioChannel m_BgmChannel;

    public override void OnInit()
    {
    }

    /// <summary>
    /// 进入音频演示流程：打开界面并注册事件监听。
    /// </summary>
    /// <param name="lastState">上一个状态</param>
    public override void OnEnter(BaseFSMState lastState)
    {
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_LOAD_CLIP, OnAudioLoadClip);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_PLAY_LOOP, OnAudioPlayLoop);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_PLAY_ONCE, OnAudioPlayOnce);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_PLAY_3D, OnAudioPlay3D);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_PAUSE, OnAudioPause);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_RESUME, OnAudioResume);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_STOP, OnAudioStop);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_STOP_IMMEDIATE, OnAudioStopImmediate);
        ManagerUtility.EventMgr.AddListener<float>(AudioDemoView.EVENT_AUDIO_VOLUME_CHANGE, OnAudioVolumeChange);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_PAUSE_ALL, OnAudioPauseAll);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_RESUME_ALL, OnAudioResumeAll);
        ManagerUtility.EventMgr.AddListener(AudioDemoView.EVENT_AUDIO_STOP_ALL, OnAudioStopAll);

        ManagerUtility.UIMgr.OpenView<AudioDemoView>();
    }

    /// <summary>
    /// 离开音频演示流程：关闭界面并移除事件监听。
    /// </summary>
    /// <param name="nextState">下一个状态</param>
    public override void OnLeave(BaseFSMState nextState)
    {
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_CLOSE, OnClose);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_LOAD_CLIP, OnAudioLoadClip);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_PLAY_LOOP, OnAudioPlayLoop);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_PLAY_ONCE, OnAudioPlayOnce);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_PLAY_3D, OnAudioPlay3D);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_PAUSE, OnAudioPause);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_RESUME, OnAudioResume);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_STOP, OnAudioStop);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_STOP_IMMEDIATE, OnAudioStopImmediate);
        ManagerUtility.EventMgr.RemoveListener<float>(AudioDemoView.EVENT_AUDIO_VOLUME_CHANGE, OnAudioVolumeChange);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_PAUSE_ALL, OnAudioPauseAll);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_RESUME_ALL, OnAudioResumeAll);
        ManagerUtility.EventMgr.RemoveListener(AudioDemoView.EVENT_AUDIO_STOP_ALL, OnAudioStopAll);

        if (m_BgmChannel != null)
        {
            ManagerUtility.AudioMgr.StopImmediate(m_BgmChannel);
            m_BgmChannel = null;
        }
        m_BgmClip = null;

        ManagerUtility.UIMgr.CloseView<AudioDemoView>();
    }

    public override void OnTermination()
    {
    }

    public override void OnUpdate()
    {
    }

    /// <summary>
    /// 关闭音频演示界面并返回菜单流程。
    /// </summary>
    private void OnClose()
    {
        ManagerUtility.FSMMgr.GetFSMProcedure().SwitchState<MenuProcedure>();
    }

    /// <summary>
    /// 加载音频 Clip（依赖资源系统已初始化）。
    /// </summary>
    private void OnAudioLoadClip()
    {
        m_BgmClip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>(BGM_PATH);
        LWDebug.Log(m_BgmClip != null ? ("音频加载成功: " + BGM_PATH) : ("音频加载失败: " + BGM_PATH));
    }

    /// <summary>
    /// 循环播放音频。
    /// </summary>
    private void OnAudioPlayLoop()
    {
        AudioClip clip = EnsureClip();
        if (clip == null)
        {
            return;
        }
        m_BgmChannel = ManagerUtility.AudioMgr.Play(clip, true, 2f, -1f);
    }

    /// <summary>
    /// 播放一次音频。
    /// </summary>
    private void OnAudioPlayOnce()
    {
        AudioClip clip = EnsureClip();
        if (clip == null)
        {
            return;
        }
        m_BgmChannel = ManagerUtility.AudioMgr.Play(clip, false, 0f, -1f);
    }
    /// <summary>
    /// 播放一次3d音频。
    /// </summary>
    private void OnAudioPlay3D()
    {
        AudioClip clip = EnsureClip();
        if (clip == null)
        {
            return;
        }
        m_BgmChannel = ManagerUtility.AudioMgr.Play(clip, new Vector3(5, 0, 0), false, 0f, -1f, Audio3DSettings.Default3D);
    }
    /// <summary>
    /// 暂停当前通道。
    /// </summary>
    private void OnAudioPause()
    {
        ManagerUtility.AudioMgr.Pause(m_BgmChannel);
    }

    /// <summary>
    /// 恢复当前通道。
    /// </summary>
    private void OnAudioResume()
    {
        ManagerUtility.AudioMgr.Resume(m_BgmChannel);
    }

    /// <summary>
    /// 停止当前通道（可能走淡出）。
    /// </summary>
    private void OnAudioStop()
    {
        ManagerUtility.AudioMgr.Stop(m_BgmChannel);
        m_BgmChannel = null;
    }

    /// <summary>
    /// 立刻停止当前通道并回收。
    /// </summary>
    private void OnAudioStopImmediate()
    {
        ManagerUtility.AudioMgr.StopImmediate(m_BgmChannel);
        m_BgmChannel = null;
    }

    /// <summary>
    /// 根据滑动条值设置全局音量。
    /// </summary>
    /// <param name="value">滑动条归一化值（0-1）</param>
    private void OnAudioVolumeChange(float value)
    {
        ManagerUtility.AudioMgr.AudioVolume = value;
    }
    /// <summary>
    /// 暂停所有通道。
    /// </summary>
    private void OnAudioPauseAll()
    {
        ManagerUtility.AudioMgr.PauseAll();
    }

    /// <summary>
    /// 恢复所有通道。
    /// </summary>
    private void OnAudioResumeAll()
    {
        ManagerUtility.AudioMgr.ResumeAll();
    }

    /// <summary>
    /// 停止所有通道。
    /// </summary>
    private void OnAudioStopAll()
    {
        ManagerUtility.AudioMgr.StopAll();
        m_BgmChannel = null;
    }

    /// <summary>
    /// 确保音频 Clip 已加载；若资源系统未初始化则给出提示。
    /// </summary>
    /// <returns>可播放的 AudioClip</returns>
    private AudioClip EnsureClip()
    {

        if (ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
        {
            LWDebug.LogWarning("AssetsMgr 未初始化，请先在 AssetsDemo 中点击 AssetsInit");
            return null;
        }

        m_BgmClip = ManagerUtility.AssetsMgr.LoadAsset<AudioClip>(BGM_PATH);
        return m_BgmClip;
    }
}

