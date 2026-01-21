using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;

namespace LWStep.Editor
{
    public static class StepXmlImporter
    {
        /// <summary>
        /// 从XML文本解析编辑器步骤图数据
        /// </summary>
        public static StepEditorGraphData LoadFromText(string xmlText)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);

            XmlElement graphElement = doc.DocumentElement;
            if (graphElement == null || graphElement.Name != "graph")
            {
                return null;
            }

            StepEditorGraphData data = new StepEditorGraphData();
            data.StartNodeId = GetAttr(graphElement, "start");

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
                    StepEditorNodeData nodeData = new StepEditorNodeData();
                    nodeData.Id = GetAttr(nodeElement, "id");
                    nodeData.Name = GetAttr(nodeElement, "name");
                    nodeData.Position = ReadPosition(nodeElement, i);

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
                            StepEditorActionData actionData = new StepEditorActionData();
                            actionData.TypeName = GetAttr(actionElement, "type");

                            if (actionElement.HasAttributes)
                            {
                                for (int k = 0; k < actionElement.Attributes.Count; k++)
                                {
                                    XmlAttribute attr = actionElement.Attributes[k];
                                    if (attr.Name == "type")
                                    {
                                        continue;
                                    }
                                    StepEditorParameterData param = new StepEditorParameterData();
                                    param.Key = attr.Name;
                                    param.Value = attr.Value;
                                    actionData.Parameters.Add(param);
                                }
                            }

                            XmlNodeList paramList = actionElement.SelectNodes("param");
                            for (int k = 0; k < paramList.Count; k++)
                            {
                                XmlElement paramElement = paramList[k] as XmlElement;
                                if (paramElement == null)
                                {
                                    continue;
                                }
                                StepEditorParameterData param = new StepEditorParameterData();
                                param.Key = GetAttr(paramElement, "key");
                                param.Value = GetAttr(paramElement, "value");
                                if (!string.IsNullOrEmpty(param.Key))
                                {
                                    actionData.Parameters.Add(param);
                                }
                            }

                            nodeData.Actions.Add(actionData);
                        }
                    }

                    data.Nodes.Add(nodeData);
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
                    StepEditorEdgeData edgeData = new StepEditorEdgeData();
                    edgeData.FromId = GetAttr(edgeElement, "from");
                    edgeData.ToId = GetAttr(edgeElement, "to");
                    edgeData.Condition = GetAttr(edgeElement, "condition");
                    edgeData.Tag = GetAttr(edgeElement, "tag");
                    edgeData.Priority = GetAttrInt(edgeElement, "priority", 0);
                    data.Edges.Add(edgeData);
                }
            }
            return data;
        }

        /// <summary>
        /// 读取节点坐标（若缺失则根据索引生成默认布局）
        /// </summary>
        private static Vector2 ReadPosition(XmlElement element, int index)
        {
            float x = GetAttrFloat(element, "x", float.NaN);
            float y = GetAttrFloat(element, "y", float.NaN);
            if (float.IsNaN(x) || float.IsNaN(y))
            {
                float offsetX = 220.0f;
                float offsetY = 140.0f;
                return new Vector2(offsetX * (index % 4), offsetY * (index / 4));
            }
            return new Vector2(x, y);
        }

        /// <summary>
        /// 读取XML属性字符串
        /// </summary>
        private static string GetAttr(XmlElement element, string name)
        {
            if (element.HasAttribute(name))
            {
                return element.GetAttribute(name);
            }
            return string.Empty;
        }

        /// <summary>
        /// 读取XML属性整数
        /// </summary>
        private static int GetAttrInt(XmlElement element, string name, int defaultValue)
        {
            string value = GetAttr(element, name);
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 读取XML属性浮点数（使用InvariantCulture解析）
        /// </summary>
        private static float GetAttrFloat(XmlElement element, string name, float defaultValue)
        {
            string value = GetAttr(element, name);
            float result;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
