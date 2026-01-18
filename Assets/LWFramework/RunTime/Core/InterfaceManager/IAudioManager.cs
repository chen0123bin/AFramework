using LWAudio;
using UnityEngine;
namespace LWCore
{
    public interface IAudioManager
    {
        public float AudioVolume { set; }
        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="clip">音频文件</param>
        /// <param name="loop">是否循环 默认-false</param>
        /// <param name="volume">音量（小于 0 使用全局音量）</param>
        /// <param name="fadeInSeconds">淡入秒数（小于等于 0 表示不淡入）</param>
        /// <returns></returns>
        AudioChannel Play(AudioClip clip, bool loop = false, float fadeInSeconds = 0f, float volume = -1);
        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="clip">音频文件</param>
        /// <param name="emitter">播放跟随的Transform</param>
        /// <param name="loop">是否循环 默认-false</param>
        /// <param name="volume">音量（小于 0 使用全局音量）</param>
        /// <param name="fadeInSeconds">淡入秒数（小于等于 0 表示不淡入）</param>
        /// <returns></returns>
        AudioChannel Play(AudioClip clip, Transform emitter, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null);
        /// <summary>
        ///  播放音频
        /// </summary>
        /// <param name="clip">音频文件</param>
        /// <param name="point">声音播放的位置</param>
        /// <param name="loop">是否循环 默认-false</param>
        /// <param name="volume">音量（小于 0 使用全局音量）</param>
        /// <param name="fadeInSeconds">淡入秒数（小于等于 0 表示不淡入）</param>
        /// <returns></returns>
        AudioChannel Play(AudioClip clip, Vector3 point, bool loop = false, float fadeInSeconds = 0f, float volume = -1, Audio3DSettings? settings = null);

        /// <summary>
        /// 停止播放（如果 Play 配置了淡入，则 Stop 会走同秒数淡出）。
        /// </summary>
        /// <param name="audioChannel"></param>
        void Stop(AudioChannel audioChannel);

        /// <summary>
        /// 立刻停止并回收（不走淡出）。
        /// </summary>
        /// <param name="audioChannel"></param>
        void StopImmediate(AudioChannel audioChannel);

        /// <summary>
        /// 暂停播放
        /// </summary>
        /// <param name="audioChannel"></param>
        void Pause(AudioChannel audioChannel);
        /// <summary>
        /// 恢复播放
        /// </summary>
        /// <param name="audioChannel"></param>
        void Resume(AudioChannel audioChannel);
        /// <summary>
        /// 停止所有的音频
        /// </summary>
        void StopAll();
        void PauseAll();
        void ResumeAll();
    }

}
