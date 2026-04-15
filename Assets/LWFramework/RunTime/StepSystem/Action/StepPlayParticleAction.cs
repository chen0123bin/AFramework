using LWCore;
using UnityEngine;

namespace LWStep
{
    [StepActionInfo("播放粒子", Category = "动画与特效", SummaryTemplate = "Particle:{target}")]
    public class StepPlayParticleAction : BaseTargeStepAction
    {
        [StepParam("waitForFinish", label: "等待播放结束", order: 1)]
        private bool m_WaitForFinish = true;

        [StepParam("restart", label: "重新播放", order: 2)]
        private bool m_Restart = true;

        private ParticleSystem[] m_ParticleSystems;
        private bool m_HasStarted;

        /// <summary>
        /// 进入动作时播放粒子；根据配置决定是否立即完成。
        /// </summary>
        protected override void OnEnter()
        {
            PlayParticles();
            if (m_WaitForFinish && HasLoopingParticleSystem())
            {
                LWDebug.LogWarning("步骤动作-播放粒子：检测到循环粒子，自动转为非阻塞完成。");
                Finish();
                return;
            }

            if (!m_WaitForFinish)
            {
                Finish();
            }
        }

        /// <summary>
        /// 更新动作：等待所有粒子播放完成后结束。
        /// </summary>
        protected override void OnUpdate()
        {
            if (IsFinished || !m_WaitForFinish)
            {
                return;
            }

            if (!IsAnyParticleAlive())
            {
                Finish();
            }
        }

        /// <summary>
        /// 退出动作时清理运行期缓存引用。
        /// </summary>
        protected override void OnExit()
        {
            m_HasStarted = false;
            m_ParticleSystems = null;
        }

        /// <summary>
        /// 快速应用时播放粒子。
        /// </summary>
        protected override void OnApply()
        {
            PlayParticles();
        }

        /// <summary>
        /// 播放目标对象及其子节点上的粒子系统。
        /// </summary>
        private void PlayParticles()
        {
            if (m_HasStarted)
            {
                return;
            }

            if (m_Target == null)
            {
                LWDebug.LogWarning("步骤动作-播放粒子：未找到对象 " + m_TargetName);
                return;
            }

            m_ParticleSystems = m_Target.GetComponentsInChildren<ParticleSystem>(true);
            if (m_ParticleSystems == null || m_ParticleSystems.Length == 0)
            {
                LWDebug.LogWarning("步骤动作-播放粒子：对象缺少 ParticleSystem " + m_Target.name);
                return;
            }

            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particleSystem = m_ParticleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                if (m_Restart)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                particleSystem.Play(true);
            }

            m_HasStarted = true;
        }

        /// <summary>
        /// 判断当前缓存的粒子系统是否仍有任一系统存活。
        /// </summary>
        private bool IsAnyParticleAlive()
        {
            if (m_ParticleSystems == null || m_ParticleSystems.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particleSystem = m_ParticleSystems[i];
                if (particleSystem != null && particleSystem.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前缓存的粒子系统中是否存在循环播放配置。
        /// </summary>
        private bool HasLoopingParticleSystem()
        {
            if (m_ParticleSystems == null || m_ParticleSystems.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particleSystem = m_ParticleSystems[i];
                if (particleSystem != null && particleSystem.main.loop)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
