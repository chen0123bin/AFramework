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
            m_EndTimeUtc = DateTime.UtcNow.AddSeconds(m_Seconds);

            double realtimeSeconds;
            if (TryGetUnityRealtimeSeconds(out realtimeSeconds))
            {
                m_UseUnityRealtime = true;
                m_EndRealtimeSeconds = realtimeSeconds + m_Seconds;
            }
            else
            {
                m_UseUnityRealtime = false;
            }

            if (m_Seconds <= 0f)
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
        private bool TryGetUnityRealtimeSeconds(out double realtimeSeconds)
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
    }
}
