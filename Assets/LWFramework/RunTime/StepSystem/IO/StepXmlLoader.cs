using System.Collections.Generic;
using System.Xml;
using LWCore;

namespace LWStep
{
    /// <summary>
    /// XML步骤图加载器
    /// </summary>
    public class StepXmlLoader
    {
        /// <summary>
        /// 从资源路径加载步骤图
        /// </summary>
        public StepGraph LoadFromAsset(string xmlAssetPath, StepActionFactory factory)
        {
            if (string.IsNullOrEmpty(xmlAssetPath))
            {
                LWDebug.LogError("步骤XML路径为空");
                return null;
            }

            string xmlText = ManagerUtility.AssetsMgr.LoadRawFileText(xmlAssetPath);
            if (string.IsNullOrEmpty(xmlText))
            {
                LWDebug.LogError("步骤XML内容为空: " + xmlAssetPath);
                return null;
            }
            return LoadFromText(xmlText, factory);
        }

        /// <summary>
        /// 从XML文本加载步骤图
        /// </summary>
        public StepGraph LoadFromText(string xmlText, StepActionFactory factory)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);

            XmlElement graphElement = doc.DocumentElement;
            if (graphElement == null || graphElement.Name != "graph")
            {
                LWDebug.LogError("步骤XML缺少graph根节点");
                return null;
            }

            string startNodeId = GetAttr(graphElement, "start");
            StepGraph graph = new StepGraph(startNodeId);

            XmlNode nodesNode = graphElement.SelectSingleNode("nodes");
            if (nodesNode != null)
            {
                XmlNodeList nodeList = nodesNode.SelectNodes("node");
                for (int i = 0; i < nodeList.Count; i++)
                {
                    XmlElement nodeElement = nodeList[i] as XmlElement;
                    if (nodeElement == null)
                    {
                        continue;
                    }
                    string nodeId = GetAttr(nodeElement, "id");
                    string nodeName = GetAttr(nodeElement, "name");
                    StepNode stepNode = new StepNode(nodeId, nodeName);

                    XmlNode actionsNode = nodeElement.SelectSingleNode("actions");
                    if (actionsNode != null)
                    {
                        XmlNodeList actionList = actionsNode.SelectNodes("action");
                        for (int j = 0; j < actionList.Count; j++)
                        {
                            XmlElement actionElement = actionList[j] as XmlElement;
                            if (actionElement == null)
                            {
                                continue;
                            }
                            string typeName = GetAttr(actionElement, "type");
                            int phase = GetAttrInt(actionElement, "phase", -1);
                            bool isBlocking = GetAttrBool(actionElement, "isBlocking", true);
                            BaseStepAction action = factory.CreateAction(typeName);
                            if (action == null)
                            {
                                LWDebug.LogError("步骤动作创建失败: " + typeName);
                                continue;
                            }
                            Dictionary<string, string> parameters = ReadActionParameters(actionElement);
                            action.SetParameters(parameters);
                            stepNode.AddAction(action, phase, isBlocking);
                        }
                    }

                    if (!graph.AddNode(stepNode))
                    {
                        LWDebug.LogError("步骤节点重复: " + nodeId);
                    }
                }
            }

            XmlNode edgesNode = graphElement.SelectSingleNode("edges");
            if (edgesNode != null)
            {
                XmlNodeList edgeList = edgesNode.SelectNodes("edge");
                for (int i = 0; i < edgeList.Count; i++)
                {
                    XmlElement edgeElement = edgeList[i] as XmlElement;
                    if (edgeElement == null)
                    {
                        continue;
                    }
                    string fromId = GetAttr(edgeElement, "from");
                    string toId = GetAttr(edgeElement, "to");
                    string condition = GetAttr(edgeElement, "condition");
                    string tag = GetAttr(edgeElement, "tag");
                    int priority = GetAttrInt(edgeElement, "priority", 0);
                    StepEdge edge = new StepEdge(fromId, toId, priority, condition, tag);
                    graph.AddEdge(edge);
                }
            }

            graph.BuildIndex();

            List<string> errors = graph.Validate();
            if (errors.Count > 0)
            {
                for (int i = 0; i < errors.Count; i++)
                {
                    LWDebug.LogError("步骤图校验失败: " + errors[i]);
                }
                return null;
            }
            return graph;
        }

        private Dictionary<string, string> ReadActionParameters(XmlElement actionElement)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (actionElement.HasAttributes)
            {
                for (int i = 0; i < actionElement.Attributes.Count; i++)
                {
                    XmlAttribute attr = actionElement.Attributes[i];
                    if (attr.Name == "type" || attr.Name == "phase" || attr.Name == "isBlocking")
                    {
                        continue;
                    }
                    parameters[attr.Name] = attr.Value;
                }
            }

            XmlNodeList paramList = actionElement.SelectNodes("param");
            for (int i = 0; i < paramList.Count; i++)
            {
                XmlElement paramElement = paramList[i] as XmlElement;
                if (paramElement == null)
                {
                    continue;
                }
                string key = GetAttr(paramElement, "key");
                string value = GetAttr(paramElement, "value");
                if (!string.IsNullOrEmpty(key))
                {
                    parameters[key] = value;
                }
            }
            return parameters;
        }

        private string GetAttr(XmlElement element, string name)
        {
            if (element.HasAttribute(name))
            {
                return element.GetAttribute(name);
            }
            return string.Empty;
        }

        private int GetAttrInt(XmlElement element, string name, int defaultValue)
        {
            string value = GetAttr(element, name);
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        private bool GetAttrBool(XmlElement element, string name, bool defaultValue)
        {
            string value = GetAttr(element, name);
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}

