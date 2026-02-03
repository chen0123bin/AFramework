using System;
using System.Collections.Generic;
using LWStep;

namespace LWCore
{
    /// <summary>
    /// 步骤管理系统接口（XML数据驱动）
    /// </summary>
    public interface IStepManager
    {
        event Action<string> OnNodeEnter;
        event Action<string> OnNodeLeave;
        event Action<string> OnNodeChanged;
        event Action<string> OnActionChanged;
        event Action<string> OnJumpProgress;
        event Action<string> OnJumpFailed;

        event Action OnAllStepsCompleted;

        /// <summary>
        /// 是否处于运行状态
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 当前节点ID
        /// </summary>
        string CurrentNodeId { get; }

        /// <summary>
        /// 加载并解析XML，构建步骤图
        /// </summary>
        /// <param name="xmlAssetPath">资源路径（Asset路径）</param>
        void LoadGraph(string xmlAssetPath);

        /// <summary>
        /// 启动步骤图
        /// </summary>
        /// <param name="graphName">图名称（XML文件名）</param>
        /// <param name="startNodeId">开始节点ID（为空则使用图配置默认）</param>
        void Start(string graphName, string startNodeId = null);

        /// <summary>
        /// 停止步骤图并清理上下文
        /// </summary>
        void Stop();

        /// <summary>
        /// 重启当前图（从开始节点重新进入）
        /// </summary>
        void Restart();

        /// <summary>
        /// 重置上下文（保留当前节点）
        /// </summary>
        void ResetContext();

        /// <summary>
        /// 前进到下一个节点（优先级最高的出边）
        /// </summary>
        void Forward(string requiredTag = null);

        /// <summary>
        /// 后退到上一个节点（不回滚结果，MVP策略）
        /// </summary>
        void Backward();

        /// <summary>
        /// 跳转到目标节点（中间节点执行快速应用Apply）
        /// </summary>
        /// <param name="targetNodeId">目标节点ID</param>
        /// <param name="requiredTag"></param>
        void JumpTo(string targetNodeId, string requiredTag = null);
        /// <summary>
        /// 获取当前图所有节点
        /// </summary>
        /// <returns>当前图所有节点ID集合</returns>
        List<StepNode> GetAllNodes(string graphName = null);
        /// <summary>
        /// 获取当前图所有显示节点（排除隐藏节点）
        /// </summary>
        /// <returns>当前图所有显示节点ID集合</returns>
        List<StepNode> GetAllDisplayNodes(string graphName = null);
        StepNodeStatus GetNodeStatus(string nodeId);
        /// <summary>
        /// 获取当前节点的可前进目标集合
        /// </summary>
        /// <returns>可前进的节点ID集合</returns>
        List<string> GetAvailableNextNodes();
        /// <summary>
        /// 获取当前节点的可前进目标集合（指定标签）
        /// </summary>
        /// <param name="requiredTag">要求的标签</param>
        /// <returns>可前进的节点ID集合</returns>
        List<string> GetAvailableNextNodes(string requiredTag);
        /// <summary>
        /// 获取当前StepContext
        /// </summary>
        /// <returns>当前StepContext</returns>
        StepContext GetStepContext();
        /// <summary>
        /// 获取当前StepContext的JSON字符串表示
        /// </summary>
        /// <returns>JSON字符串</returns>
        string GetContextToJson();
        /// <summary>
        /// 从JSON字符串加载上下文数据
        /// </summary>
        /// <param name="json">JSON字符串</param>
        void LoadContextFromJson(string json);
    }
}
