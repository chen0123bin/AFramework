using System.Collections.Generic;
using LWCore;
using UnityEngine;

namespace LWStep
{
    public class StepToggleObjectAction : BaseStepAction
    {
        protected override void OnEnter()
        {
            ExecuteToggle();
            Finish();
        }

        protected override void OnUpdate()
        {
            if (!IsFinished)
            {
                Finish();
            }
        }

        protected override void OnExit()
        {
        }

        protected override void OnApply()
        {
            ExecuteToggle();
        }

        private void ExecuteToggle()
        {
            GameObject target = FindTarget();
            if (target == null)
            {
                return;
            }

            bool isActive = GetBoolParam("isActive", true);
            target.GetComponent<Renderer>().enabled = isActive;
            LWDebug.Log("步骤动作-物体显隐：" + target.name + " -> " + isActive);
        }

        private GameObject FindTarget()
        {
            string targetName = GetStringParam("target", string.Empty);
            if (string.IsNullOrEmpty(targetName))
            {
                LWDebug.LogWarning("步骤动作-物体显隐：target 为空");
                return null;
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                LWDebug.LogWarning("步骤动作-物体显隐：未找到对象 " + targetName);
            }
            return target;
        }

        private string GetStringParam(string key, string defaultValue)
        {
            Dictionary<string, string> parameters = GetParameters();
            if (parameters == null)
            {
                return defaultValue;
            }
            string value;
            if (parameters.TryGetValue(key, out value))
            {
                return value;
            }
            return defaultValue;
        }

        private bool GetBoolParam(string key, bool defaultValue)
        {
            string value = GetStringParam(key, string.Empty);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
