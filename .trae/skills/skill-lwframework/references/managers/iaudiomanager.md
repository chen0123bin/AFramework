# IAudioManager 音频管理器

## 概述

IAudioManager 是 LWFramework 的音频系统核心接口，提供 2D/3D 音效播放、通道控制、全局音量设置等功能。

- **接口位置**: `Assets/LWFramework/RunTime/Core/InterfaceManager/IAudioManager.cs`
- **默认实现**: `Assets/LWFramework/RunTime/Audio/AudioManager.cs`
- **访问入口**: `ManagerUtility.AudioMgr`

---

## 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `AudioVolume` | `float` | 全局音量（0-1），作用于当前已激活的所有通道 |

---

## API 参考

### 2D 音频播放

```csharp
/// <summary>
/// 播放 2D 音效
/// </summary>
/// <param name="audioClip">音频剪辑</param>
/// <param name="loop">是否循环</param>
/// <param name="fadeInSeconds">淡入时间（秒）</param>
/// <param name="volume">音量（-1 使用默认）</param>
/// <returns>音频通道，可用于后续控制</returns>
AudioChannel Play(AudioClip audioClip, bool loop = false, float fadeInSeconds = 0f, float volume = -1f);
```

### 3D 音频播放

```csharp
/// <summary>
/// 播放 3D 音效（跟随挂点）
/// </summary>
/// <param name="audioClip">音频剪辑</param>
/// <param name="followTarget">跟随目标</param>
/// <param name="loop">是否循环</param>
/// <param name="fadeInSeconds">淡入时间（秒）</param>
/// <param name="volume">音量（-1 使用默认）</param>
/// <param name="settings">3D 音频设置</param>
/// <returns>音频通道</returns>
AudioChannel Play(AudioClip audioClip, Transform followTarget, bool loop = false, float fadeInSeconds = 0f, float volume = -1f, Audio3DSettings settings = null);

/// <summary>
/// 播放 3D 音效（定点）
/// </summary>
/// <param name="audioClip">音频剪辑</param>
/// <param name="worldPosition">世界坐标</param>
/// <param name="loop">是否循环</param>
/// <param name="fadeInSeconds">淡入时间（秒）</param>
/// <param name="volume">音量（-1 使用默认）</param>
/// <param name="settings">3D 音频设置</param>
/// <returns>音频通道</returns>
AudioChannel Play(AudioClip audioClip, Vector3 worldPosition, bool loop = false, float fadeInSeconds = 0f, float volume = -1f, Audio3DSettings settings = null);
```

### 通道控制

```csharp
/// <summary>
/// 暂停指定通道
/// </summary>
void Pause(AudioChannel channel);

/// <summary>
/// 恢复指定通道
/// </summary>
void Resume(AudioChannel channel);

/// <summary>
/// 停止指定通道（可能走淡出逻辑）
/// </summary>
void Stop(AudioChannel channel);

/// <summary>
/// 立即停止并回收通道
/// </summary>
void StopImmediate(AudioChannel channel);
```

### 全局控制

```csharp
/// <summary>
/// 停止所有通道
/// </summary>
void StopAll();

/// <summary>
/// 暂停所有通道
/// </summary>
void PauseAll();

/// <summary>
/// 恢复所有通道
/// </summary>
void ResumeAll();
```

---

## Audio3DSettings 3D音频设置

```csharp
public class Audio3DSettings
{
    /// <summary>
    /// 默认 3D 设置
    /// </summary>
    public static Audio3DSettings Default3D = new Audio3DSettings();

    public float MinDistance = 1f;      // 最小距离
    public float MaxDistance = 500f;    // 最大距离
    public float PanLevel = 1f;         // 声像级别
    public float Spread = 0f;           // 扩散
    public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;  // 衰减模式
}
```

---

## 使用示例

### 播放 2D 音效

```csharp
using LWAudio;
using LWCore;
using UnityEngine;

public class AudioManagerExamples : MonoBehaviour
{
    [SerializeField] private AudioClip m_UIClickClip;
    private AudioChannel m_UIClickChannel;

    /// <summary>
    /// 播放 2D 音效：返回的 AudioChannel 可用于后续控制
    /// </summary>
    public void PlayUIClick()
    {
        if (m_UIClickClip == null)
        {
            return;
        }

        m_UIClickChannel = ManagerUtility.AudioMgr.Play(
            m_UIClickClip, 
            loop: false, 
            fadeInSeconds: 0f, 
            volume: -1f);
    }
}
```

### 播放 3D 音效

```csharp
using LWAudio;
using LWCore;
using UnityEngine;

public class AudioManagerExamples : MonoBehaviour
{
    [SerializeField] private AudioClip m_ExplosionClip;
    [SerializeField] private Transform m_Emitter;

    /// <summary>
    /// 播放 3D 音效：跟随挂点，并可按需覆写 3D 参数
    /// </summary>
    public void PlayExplosionOnEmitter()
    {
        if (m_ExplosionClip == null)
        {
            return;
        }

        // 使用默认 3D 设置并修改参数
        Audio3DSettings audio3DSettings = Audio3DSettings.Default3D;
        audio3DSettings.MinDistance = 2f;
        audio3DSettings.MaxDistance = 30f;

        ManagerUtility.AudioMgr.Play(
            m_ExplosionClip,
            m_Emitter,
            loop: false,
            fadeInSeconds: 0.05f,
            volume: -1f,
            settings: audio3DSettings);
    }

    /// <summary>
    /// 在指定位置播放 3D 音效
    /// </summary>
    public void PlayExplosionAtPosition(Vector3 position)
    {
        if (m_ExplosionClip == null)
        {
            return;
        }

        ManagerUtility.AudioMgr.Play(
            m_ExplosionClip,
            position,
            loop: false,
            fadeInSeconds: 0.05f,
            volume: -1f,
            settings: Audio3DSettings.Default3D);
    }
}
```

### 设置全局音量

```csharp
/// <summary>
/// 设置全局音量：会作用于当前已激活的所有通道
/// </summary>
public void SetGlobalVolume(float volume01)
{
    float volume = Mathf.Clamp01(volume01);
    ManagerUtility.AudioMgr.AudioVolume = volume;
}
```

### 控制指定通道

```csharp
/// <summary>
/// 控制指定通道：暂停/恢复/停止（Stop 会触发淡出逻辑，取决于 Play 配置）
/// </summary>
public void ControlChannel()
{
    if (m_UIClickChannel == null)
    {
        return;
    }

    // 暂停
    ManagerUtility.AudioMgr.Pause(m_UIClickChannel);
    
    // 恢复
    ManagerUtility.AudioMgr.Resume(m_UIClickChannel);
    
    // 停止（带淡出）
    ManagerUtility.AudioMgr.Stop(m_UIClickChannel);
    
    // 立即停止
    // ManagerUtility.AudioMgr.StopImmediate(m_UIClickChannel);
    
    m_UIClickChannel = null;
}
```

### 停止所有音频

```csharp
/// <summary>
/// 停止所有通道：适用于切场景/回主城/进入战斗等全局切换
/// </summary>
public void StopAll()
{
    ManagerUtility.AudioMgr.StopAll();
}
```

### 背景音乐管理器示例

```csharp
using LWAudio;
using LWCore;
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    [SerializeField] private AudioClip m_MainMenuBGM;
    [Serialize