using Cysharp.Threading.Tasks;
using LWCore;
using UnityEngine;

public class StepDemoRunner : MonoBehaviour
{
    [SerializeField] private string m_XmlPath = "Assets/0Res/RawFiles/StepMinimal.xml";
    [SerializeField] private string m_GraphId = "step_demo";
    [SerializeField] private int m_AutoForwardCount = 3;
    [SerializeField] private int m_AutoForwardDelayMs = 2000;

    private async void Start()
    {
        await WaitForStepReady();
        IStepManager stepManager = ManagerUtility.StepMgr;
        stepManager.OnAllStepsCompleted += OnAllStepsCompleted;
        stepManager.OnNodeChanged += OnNodeChanged;
        stepManager.OnActionChanged += OnActionChanged;
        stepManager.LoadGraph(m_XmlPath);
        stepManager.Start(m_GraphId);

        // for (int i = 0; i < m_AutoForwardCount; i++)
        // {
        //     await UniTask.Delay(m_AutoForwardDelayMs);
        //     stepManager.Forward();
        // }
    }

    private void OnNodeChanged(string nodeId)
    {
        LWDebug.Log($"当前节点ID：{nodeId}");
    }

    private void OnActionChanged(string actionId)
    {
        LWDebug.Log($"当前操作ID：{actionId}");
    }

    private async UniTask WaitForStepReady()
    {
        while (ManagerUtility.StepMgr == null || ManagerUtility.AssetsMgr == null || !ManagerUtility.AssetsMgr.IsInitialized)
        {
            await UniTask.Yield();
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ManagerUtility.StepMgr.JumpTo("step_move");
        }
    }
    private void OnDestroy()
    {
        IStepManager stepManager = ManagerUtility.StepMgr;
        if (stepManager == null)
        {
            return;
        }
        stepManager.OnAllStepsCompleted -= OnAllStepsCompleted;
    }

    private void OnAllStepsCompleted()
    {
        LWDebug.Log("所有步骤完成");
    }
}
