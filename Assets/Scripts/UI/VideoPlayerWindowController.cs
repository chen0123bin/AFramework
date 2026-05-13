using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 横屏视频播放器窗口控制器：负责视频源切换、播放控制、进度显示、窗口放大与 Canvas 内全屏。
/// </summary>
public sealed class VideoPlayerWindowController : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private RectTransform m_WindowRect;
    [SerializeField] private Text m_TxtTitle;
    [SerializeField] private RawImage m_RImgVideo;
    [SerializeField] private Text m_TxtEmptyHint;
    [SerializeField] private Button m_BtnPlay;
    [SerializeField] private Button m_BtnPause;
    [SerializeField] private Button m_BtnZoom;
    [SerializeField] private Button m_BtnShrink;
    [SerializeField] private Button m_BtnFullscreen;
    [SerializeField] private Button m_BtnClose;
    [SerializeField] private Slider m_SldProgress;
    [SerializeField] private Text m_TxtCurrentTime;
    [SerializeField] private Text m_TxtDuration;
    [SerializeField] private VideoPlayer m_VideoPlayer;
    [SerializeField] private Vector2 m_ZoomSize = new Vector2(1560f, 860f);
    [SerializeField] private Vector2Int m_RenderTextureSize = new Vector2Int(1920, 1080);

    private RenderTexture m_RenderTexture;
    private RectLayoutState m_NormalLayout;
    private bool m_IsPrepared;
    private bool m_IsDraggingProgress;
    private bool m_WasPlayingBeforeDrag;
    private bool m_PlayWhenPrepared;
    private double m_DurationSeconds;

    /// <summary>
    /// 普通态窗口布局快照，用于放大和全屏后恢复。
    /// </summary>
    [Serializable]
    private struct RectLayoutState
    {
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
    }

    /// <summary>
    /// 初始化控件引用、播放器依赖和默认显示状态。
    /// </summary>
    private void Awake()
    {
        ResolveReferencesIfNeeded();
        EnsureVideoPlayer();
        CacheNormalLayout();
        EnsureRenderTexture();
        BindEvents();
        ResetDisplay();
        SetTitle(m_TxtTitle != null ? m_TxtTitle.text : "视频播放器");
        SetEmptyHintVisible(true);

        PrepareCurrentSource();
    }

    /// <summary>
    /// 每帧同步当前播放时间和进度条显示，拖动进度条时暂停自动刷新。
    /// </summary>
    private void Update()
    {
        if (!m_IsPrepared || m_IsDraggingProgress || m_VideoPlayer == null)
        {
            return;
        }

        if (!m_VideoPlayer.isPlaying && !m_VideoPlayer.isPaused)
        {
            return;
        }

        UpdateProgressDisplay(m_VideoPlayer.time);
    }

    /// <summary>
    /// 销毁时解绑事件并释放渲染纹理资源。
    /// </summary>
    private void OnDestroy()
    {
        UnbindEvents();
        ReleaseRenderTexture();
    }

    /// <summary>
    /// 设置 VideoClip 片源，并按需要在准备完成后自动播放。
    /// </summary>
    /// <param name="clip">视频资源</param>
    /// <param name="autoPlay">准备完成后是否自动播放</param>
    public void SetVideoClip(VideoClip clip, bool autoPlay = false)
    {
        ResetPlaybackState();
        EnsureVideoPlayer();

        m_VideoPlayer.source = VideoSource.VideoClip;
        m_VideoPlayer.clip = clip;
        m_VideoPlayer.url = string.Empty;
        m_PlayWhenPrepared = autoPlay;

        if (HasSource())
        {
            PrepareCurrentSource();
        }
    }

    /// <summary>
    /// 设置 URL 片源，并按需要在准备完成后自动播放。
    /// </summary>
    /// <param name="url">远程或本地 URL</param>
    /// <param name="autoPlay">准备完成后是否自动播放</param>
    public void SetVideoUrl(string url, bool autoPlay = false)
    {
        ResetPlaybackState();
        EnsureVideoPlayer();

        m_VideoPlayer.source = VideoSource.Url;
        m_VideoPlayer.clip = null;
        m_VideoPlayer.url = url ?? string.Empty;
        m_PlayWhenPrepared = autoPlay;

        if (HasSource())
        {
            PrepareCurrentSource();
        }
    }

    /// <summary>
    /// 播放当前视频；若还未准备完成，则记录准备完成后的自动播放意图。
    /// </summary>
    public void Play()
    {
        if (m_VideoPlayer == null)
        {
            return;
        }

        if (!HasSource())
        {
            Debug.LogWarning("VideoPlayerWindowController 未设置视频源，无法播放。");
            return;
        }

        if (!m_IsPrepared)
        {
            m_PlayWhenPrepared = true;
            PrepareCurrentSource();
            return;
        }

        m_VideoPlayer.Play();
    }

    /// <summary>
    /// 暂停当前视频。
    /// </summary>
    public void Pause()
    {
        if (m_VideoPlayer != null && m_IsPrepared)
        {
            m_VideoPlayer.Pause();
        }
    }

    /// <summary>
    /// 停止当前视频，并将时间和进度恢复为初始状态。
    /// </summary>
    public void Stop()
    {
        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.Stop();
        }

        m_PlayWhenPrepared = false;
        m_IsPrepared = false;
        m_DurationSeconds = 0d;
        ResetDisplay();
        SetEmptyHintVisible(!HasSource());
    }

    /// <summary>
    /// 按秒数跳转到指定播放位置，并同步时间和进度显示。
    /// </summary>
    /// <param name="seconds">目标秒数</param>
    public void SeekToSeconds(float seconds)
    {
        double clampedSeconds = Mathf.Max(0f, Mathf.Min((float)m_DurationSeconds, seconds));

        if (m_VideoPlayer != null && m_IsPrepared)
        {
            m_VideoPlayer.time = clampedSeconds;
        }

        UpdateProgressDisplay(clampedSeconds);
    }

    /// <summary>
    /// 按 0 到 1 的归一化进度跳转，并同步总时长显示。
    /// </summary>
    /// <param name="normalizedValue">归一化进度值</param>
    public void SeekToNormalized(float normalizedValue)
    {
        float clamped = Mathf.Clamp01(normalizedValue);
        SeekToSeconds((float)(m_DurationSeconds * clamped));

        if (m_TxtDuration != null)
        {
            m_TxtDuration.text = FormatTime(m_DurationSeconds);
        }
    }

    /// <summary>
    /// 进入放大态窗口，保持窗口语义但扩大显示区域。
    /// </summary>
    public void EnterZoomMode()
    {
        if (m_WindowRect == null)
        {
            return;
        }

        m_WindowRect.anchorMin = new Vector2(0.5f, 0.5f);
        m_WindowRect.anchorMax = new Vector2(0.5f, 0.5f);
        m_WindowRect.pivot = new Vector2(0.5f, 0.5f);
        m_WindowRect.anchoredPosition = Vector2.zero;
        m_WindowRect.sizeDelta = m_ZoomSize;
    }

    /// <summary>
    /// 退出放大态并恢复普通窗口布局。
    /// </summary>
    public void ExitZoomMode()
    {
        RestoreNormalLayout();
    }

    /// <summary>
    /// 在当前 Canvas 中将播放器窗口拉伸铺满显示。
    /// </summary>
    public void EnterCanvasFullscreen()
    {
        if (m_WindowRect == null)
        {
            return;
        }

        m_WindowRect.anchorMin = Vector2.zero;
        m_WindowRect.anchorMax = Vector2.one;
        m_WindowRect.pivot = new Vector2(0.5f, 0.5f);
        m_WindowRect.anchoredPosition = Vector2.zero;
        m_WindowRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// 退出 Canvas 内全屏并恢复普通窗口布局。
    /// </summary>
    public void ExitCanvasFullscreen()
    {
        RestoreNormalLayout();
    }

    /// <summary>
    /// 更新窗口标题文本。
    /// </summary>
    /// <param name="title">标题内容</param>
    public void SetTitle(string title)
    {
        if (m_TxtTitle != null)
        {
            m_TxtTitle.text = string.IsNullOrEmpty(title) ? "视频播放器" : title;
        }
    }

    /// <summary>
    /// 关闭窗口、停止播放并释放当前渲染纹理。
    /// </summary>
    public void CloseWindow()
    {
        Stop();
        ReleaseRenderTexture();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 记录进度条按下事件，用于识别拖动前的播放状态。
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsDraggingProgress = IsProgressEvent(eventData);
        m_WasPlayingBeforeDrag = m_VideoPlayer != null && m_VideoPlayer.isPlaying;
    }

    /// <summary>
    /// 标记进度条开始拖动，暂停自动进度刷新。
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsProgressEvent(eventData))
        {
            return;
        }

        m_IsDraggingProgress = true;
        m_WasPlayingBeforeDrag = m_VideoPlayer != null && m_VideoPlayer.isPlaying;
    }

    /// <summary>
    /// 在进度条拖动结束时执行跳转，并按拖动前状态决定是否继续播放。
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsProgressEvent(eventData))
        {
            return;
        }

        m_IsDraggingProgress = false;
        SeekToNormalized(m_SldProgress != null ? m_SldProgress.value : 0f);

        if (m_WasPlayingBeforeDrag && m_IsPrepared && m_VideoPlayer != null)
        {
            m_VideoPlayer.Play();
        }
    }

    /// <summary>
    /// 自动查找未显式绑定的窗口控件，兼容直接挂到 JSON 生成的界面根节点使用。
    /// </summary>
    private void ResolveReferencesIfNeeded()
    {
        if (m_WindowRect == null)
        {
            m_WindowRect = FindRectTransform("PnlWindow");
            if (m_WindowRect == null)
            {
                m_WindowRect = transform as RectTransform;
            }
        }

        if (m_TxtTitle == null)
        {
            m_TxtTitle = FindComponent<Text>("PnlTop/TxtTitle");
        }
        if (m_RImgVideo == null)
        {
            m_RImgVideo = FindComponent<RawImage>("PnlVideo/RImgVideo");
        }
        if (m_TxtEmptyHint == null)
        {
            m_TxtEmptyHint = FindComponent<Text>("PnlVideo/TxtEmptyHint");
        }
        if (m_BtnPlay == null)
        {
            m_BtnPlay = FindComponent<Button>("PnlBottom/BtnPlay");
        }
        if (m_BtnPause == null)
        {
            m_BtnPause = FindComponent<Button>("PnlBottom/BtnPause");
        }
        if (m_BtnZoom == null)
        {
            m_BtnZoom = FindComponent<Button>("PnlTop/BtnZoom");
        }
        if (m_BtnShrink == null)
        {
            m_BtnShrink = FindComponent<Button>("PnlBottom/BtnShrink");
        }
        if (m_BtnFullscreen == null)
        {
            m_BtnFullscreen = FindComponent<Button>("PnlTop/BtnFullscreen");
        }
        if (m_BtnClose == null)
        {
            m_BtnClose = FindComponent<Button>("PnlTop/BtnClose");
        }
        if (m_SldProgress == null)
        {
            m_SldProgress = FindComponent<Slider>("PnlBottom/SldProgress");
        }
        if (m_TxtCurrentTime == null)
        {
            m_TxtCurrentTime = FindComponent<Text>("PnlBottom/TxtCurrentTime");
        }
        if (m_TxtDuration == null)
        {
            m_TxtDuration = FindComponent<Text>("PnlBottom/TxtDuration");
        }
    }

    /// <summary>
    /// 绑定按钮、进度条与播放器事件。
    /// </summary>
    private void BindEvents()
    {
        if (m_BtnPlay != null)
        {
            m_BtnPlay.onClick.AddListener(Play);
        }
        if (m_BtnPause != null)
        {
            m_BtnPause.onClick.AddListener(Pause);
        }
        if (m_BtnZoom != null)
        {
            m_BtnZoom.onClick.AddListener(EnterZoomMode);
        }
        if (m_BtnShrink != null)
        {
            m_BtnShrink.onClick.AddListener(RestoreNormalLayout);
        }
        if (m_BtnFullscreen != null)
        {
            m_BtnFullscreen.onClick.AddListener(EnterCanvasFullscreen);
        }
        if (m_BtnClose != null)
        {
            m_BtnClose.onClick.AddListener(CloseWindow);
        }
        if (m_SldProgress != null)
        {
            m_SldProgress.onValueChanged.AddListener(HandleSliderValueChanged);
        }
        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.playOnAwake = false;
            m_VideoPlayer.waitForFirstFrame = true;
            //m_VideoPlayer.renderMode = VideoRenderMode.APIOnly;
            m_VideoPlayer.prepareCompleted += HandlePrepareCompleted;
            m_VideoPlayer.errorReceived += HandleErrorReceived;
        }
    }

    /// <summary>
    /// 解绑按钮、进度条与播放器事件。
    /// </summary>
    private void UnbindEvents()
    {
        if (m_BtnPlay != null)
        {
            m_BtnPlay.onClick.RemoveListener(Play);
        }
        if (m_BtnPause != null)
        {
            m_BtnPause.onClick.RemoveListener(Pause);
        }
        if (m_BtnZoom != null)
        {
            m_BtnZoom.onClick.RemoveListener(EnterZoomMode);
        }
        if (m_BtnShrink != null)
        {
            m_BtnShrink.onClick.RemoveListener(RestoreNormalLayout);
        }
        if (m_BtnFullscreen != null)
        {
            m_BtnFullscreen.onClick.RemoveListener(EnterCanvasFullscreen);
        }
        if (m_BtnClose != null)
        {
            m_BtnClose.onClick.RemoveListener(CloseWindow);
        }
        if (m_SldProgress != null)
        {
            m_SldProgress.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }
        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.prepareCompleted -= HandlePrepareCompleted;
            m_VideoPlayer.errorReceived -= HandleErrorReceived;
        }
    }

    /// <summary>
    /// 准备当前视频源并绑定渲染纹理到 RawImage。
    /// </summary>
    private void PrepareCurrentSource()
    {
        if (m_VideoPlayer == null || !HasSource())
        {
            return;
        }

        EnsureRenderTexture();
        m_VideoPlayer.targetTexture = m_RenderTexture;

        if (m_RImgVideo != null)
        {
            m_RImgVideo.texture = m_RenderTexture;
        }

        m_VideoPlayer.Prepare();
    }

    /// <summary>
    /// 在准备阶段前重置播放状态与显示内容。
    /// </summary>
    private void ResetPlaybackState()
    {
        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.Stop();
        }

        m_IsPrepared = false;
        m_DurationSeconds = 0d;
        m_PlayWhenPrepared = false;
        ResetDisplay();
        SetEmptyHintVisible(true);
    }

    /// <summary>
    /// 在视频准备完成后记录时长、刷新显示，并按需自动播放。
    /// </summary>
    /// <param name="source">准备完成的播放器实例</param>
    private void HandlePrepareCompleted(VideoPlayer source)
    {
        m_IsPrepared = true;
        m_DurationSeconds = source != null && source.length > 0d ? source.length : 0d;
        SetEmptyHintVisible(false);
        UpdateProgressDisplay(0d);

        if (m_PlayWhenPrepared && source != null)
        {
          m_PlayWhenPrepared = false;
          source.Play();
        }
    }

    /// <summary>
    /// 在视频准备或播放失败时恢复为可重新设置片源的状态。
    /// </summary>
    /// <param name="source">播放器实例</param>
    /// <param name="message">错误信息</param>
    private void HandleErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogWarning($"VideoPlayerWindowController 播放失败：{message}");
        Stop();
        SetEmptyHintVisible(true);
    }

    /// <summary>
    /// 在拖动进度条过程中实时刷新当前时间预览文本。
    /// </summary>
    /// <param name="value">当前滑块值</param>
    private void HandleSliderValueChanged(float value)
    {
        if (!m_IsDraggingProgress)
        {
            return;
        }

        double previewSeconds = m_DurationSeconds * Mathf.Clamp01(value);
        if (m_TxtCurrentTime != null)
        {
            m_TxtCurrentTime.text = FormatTime(previewSeconds);
        }
    }

    /// <summary>
    /// 刷新当前时间、总时长和进度条显示。
    /// </summary>
    /// <param name="currentSeconds">当前秒数</param>
    private void UpdateProgressDisplay(double currentSeconds)
    {
        double clampedCurrent = Math.Max(0d, Math.Min(m_DurationSeconds, currentSeconds));

        if (m_TxtCurrentTime != null)
        {
            m_TxtCurrentTime.text = FormatTime(clampedCurrent);
        }
        if (m_TxtDuration != null)
        {
            m_TxtDuration.text = FormatTime(m_DurationSeconds);
        }
        if (m_SldProgress != null)
        {
            float progressValue = m_DurationSeconds > 0d ? (float)(clampedCurrent / m_DurationSeconds) : 0f;
            m_SldProgress.SetValueWithoutNotify(progressValue);
        }
    }

    /// <summary>
    /// 将时间文本与进度条恢复为初始状态。
    /// </summary>
    private void ResetDisplay()
    {
        if (m_TxtCurrentTime != null)
        {
            m_TxtCurrentTime.text = "00:00:00";
        }
        if (m_TxtDuration != null)
        {
            m_TxtDuration.text = "00:00:00";
        }
        if (m_SldProgress != null)
        {
            m_SldProgress.SetValueWithoutNotify(0f);
        }
    }

    /// <summary>
    /// 缓存普通态窗口布局，用于还原放大态或全屏态。
    /// </summary>
    private void CacheNormalLayout()
    {
        if (m_WindowRect == null)
        {
            return;
        }

        m_NormalLayout.AnchorMin = m_WindowRect.anchorMin;
        m_NormalLayout.AnchorMax = m_WindowRect.anchorMax;
        m_NormalLayout.Pivot = m_WindowRect.pivot;
        m_NormalLayout.AnchoredPosition = m_WindowRect.anchoredPosition;
        m_NormalLayout.SizeDelta = m_WindowRect.sizeDelta;
    }

    /// <summary>
    /// 恢复普通态窗口布局。
    /// </summary>
    private void RestoreNormalLayout()
    {
        if (m_WindowRect == null)
        {
            return;
        }

        m_WindowRect.anchorMin = m_NormalLayout.AnchorMin;
        m_WindowRect.anchorMax = m_NormalLayout.AnchorMax;
        m_WindowRect.pivot = m_NormalLayout.Pivot;
        m_WindowRect.anchoredPosition = m_NormalLayout.AnchoredPosition;
        m_WindowRect.sizeDelta = m_NormalLayout.SizeDelta;
    }

    /// <summary>
    /// 确保 VideoPlayer 组件存在。
    /// </summary>
    private void EnsureVideoPlayer()
    {
        if (m_VideoPlayer == null)
        {
            m_VideoPlayer = GetComponent<VideoPlayer>();
        }
        if (m_VideoPlayer == null)
        {
            m_VideoPlayer = gameObject.AddComponent<VideoPlayer>();
        }
    }

    /// <summary>
    /// 确保渲染纹理存在，供 VideoPlayer 输出到 RawImage。
    /// </summary>
    private void EnsureRenderTexture()
    {
        if (m_RenderTexture != null)
        {
            return;
        }

        m_RenderTexture = new RenderTexture(m_RenderTextureSize.x, m_RenderTextureSize.y, 0, RenderTextureFormat.ARGB32);
        m_RenderTexture.Create();
    }

    /// <summary>
    /// 释放渲染纹理并清空对 VideoPlayer 与 RawImage 的绑定。
    /// </summary>
    private void ReleaseRenderTexture()
    {
        if (m_VideoPlayer != null && m_VideoPlayer.targetTexture == m_RenderTexture)
        {
            m_VideoPlayer.targetTexture = null;
        }
        if (m_RImgVideo != null && m_RImgVideo.texture == m_RenderTexture)
        {
            m_RImgVideo.texture = null;
        }
        if (m_RenderTexture != null)
        {
            m_RenderTexture.Release();
            DestroyImmediate(m_RenderTexture);
            m_RenderTexture = null;
        }
    }

    /// <summary>
    /// 判断当前播放器是否已经持有有效片源。
    /// </summary>
    /// <returns>存在可播放片源时返回 true</returns>
    private bool HasSource()
    {
        if (m_VideoPlayer == null)
        {
            return false;
        }

        if (m_VideoPlayer.source == VideoSource.VideoClip)
        {
            return m_VideoPlayer.clip != null;
        }

        if (m_VideoPlayer.source == VideoSource.Url)
        {
            return !string.IsNullOrEmpty(m_VideoPlayer.url);
        }

        return false;
    }

    /// <summary>
    /// 控制无片源提示文本的显示与隐藏。
    /// </summary>
    /// <param name="visible">是否显示提示文本</param>
    private void SetEmptyHintVisible(bool visible)
    {
        if (m_TxtEmptyHint != null)
        {
            m_TxtEmptyHint.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 判断当前事件是否来自进度条或其子节点。
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>来自进度条区域时返回 true</returns>
    private bool IsProgressEvent(PointerEventData eventData)
    {
        if (eventData == null || m_SldProgress == null)
        {
            return false;
        }

        return eventData.pointerDrag == m_SldProgress.gameObject
            || eventData.pointerPress == m_SldProgress.gameObject
            || (eventData.pointerPress != null && eventData.pointerPress.transform.IsChildOf(m_SldProgress.transform));
    }

    /// <summary>
    /// 按相对路径查找组件，兼容直接挂在根节点或 PnlWindow 节点两种用法。
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="path">相对路径</param>
    /// <returns>找到的组件；找不到则返回 null</returns>
    private T FindComponent<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            target = transform.Find("PnlWindow/" + path);
        }

        return target != null ? target.GetComponent<T>() : null;
    }

    /// <summary>
    /// 按名称查找窗口主体的 RectTransform。
    /// </summary>
    /// <param name="path">窗口节点路径</param>
    /// <returns>窗口 RectTransform</returns>
    private RectTransform FindRectTransform(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            target = transform.Find("PnlWindow/" + path);
        }

        return target as RectTransform;
    }

    /// <summary>
    /// 将秒数格式化为统一的 hh:mm:ss 文本。
    /// </summary>
    /// <param name="seconds">总秒数</param>
    /// <returns>格式化后的时间字符串</returns>
    private static string FormatTime(double seconds)
    {
        double safeSeconds = Math.Max(0d, seconds);
        int totalSeconds = (int)Math.Floor(safeSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, secs);
    }
}
