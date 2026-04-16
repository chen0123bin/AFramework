using System;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("等待秒数", Category = "流程控制", SummaryTemplate = "Wait:{seconds}s")]
    public class StepWaitSecondsAction : BaseStepAction
    {
        [StepParam("seconds", label: "等待秒数", order: 0)]
        private float m_Seconds;

        private DateTime m_EndTimeUtc;
        private double m_EndRealtimeSeconds;
        private bool m_UseUnityRealtime;

        /// <summary>
        /// 进入动作：记录截止时间。
        /// </summary>
        protected override void OnEnter()
        {
            DateTime nowUtc = DateTime.UtcNow;
            double waitSeconds = NormalizeWaitSeconds(m_Seconds);

            m_EndTimeUtc = GetSafeEndTimeUtc(nowUtc, waitSeconds);

            double realtimeSeconds;
            if (TryGetUnityRealtimeSeconds(out realtimeSeconds))
            {
                m_UseUnityRealtime = true;
                m_EndRealtimeSeconds = realtimeSeconds + waitSeconds;
            }
            else
            {
                m_UseUnityRealtime = false;
            }

            if (waitSeconds <= 0d)
            {
                Finish();
            }
        }

        /// <summary>
        /// 更新动作：到达截止时间后结束。
        /// </summary>
        protected override void OnUpdate()
        {
            if (m_UseUnityRealtime)
            {
                double realtimeSeconds;
                if (TryGetUnityRealtimeSeconds(out realtimeSeconds))
                {
                    if (realtimeSeconds >= m_EndRealtimeSeconds)
                    {
                        Finish();
                    }
                    return;
                }
            }

            if (DateTime.UtcNow >= m_EndTimeUtc)
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作。
        /// </summary>
        protected override void OnExit()
        {
        }

        /// <summary>
        /// 快速应用：直接结束动作。
        /// </summary>
        protected override void OnApply()
        {
            Finish();
        }

        /// <summary>
        /// 获取 Unity 非缩放实时时间（仅在运行态可用）。
        /// </summary>
        protected virtual bool TryGetUnityRealtimeSeconds(out double realtimeSeconds)
        {
            realtimeSeconds = 0d;
            try
            {
                if (!Application.isPlaying)
                {
                    return false;
                }

                realtimeSeconds = Time.realtimeSinceStartupAsDouble;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 归一化等待秒数：将非法值转为 0，负值保持原语义以便立即完成。
        /// </summary>
        private static double NormalizeWaitSeconds(float seconds)
        {
            double normalizedSeconds = seconds;
            if (double.IsNaN(normalizedSeconds) || double.IsInfinity(normalizedSeconds))
            {
                return 0d;
            }

            return normalizedSeconds;
        }

        /// <summary>
        /// 安全计算截止时间：将秒数限制在 DateTime 可表示范围内，避免 AddSeconds 抛异常。
        /// </summary>
        private static DateTime GetSafeEndTimeUtc(DateTime startUtc, double seconds)
        {
            double minSeconds = (DateTime.MinValue - startUtc).TotalSeconds;
            double maxSeconds = (DateTime.MaxValue - startUtc).TotalSeconds;
            double clampedSeconds = Math.Max(minSeconds, Math.Min(maxSeconds, seconds));
            return startUtc.AddSeconds(clampedSeconds);
        }
    }
}
