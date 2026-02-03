using System.Globalization;
using System.IO;
using System.Xml;
using LWStep;
using UnityEditor;

namespace LWStep.Editor
{
    public static class StepXmlExporter
    {
        /// <summary>
        /// 将编辑器步骤图数据导出为XML文本
        /// </summary>
        public static string ExportToText(StepEditorGraphData data)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement graphElement = doc.CreateElement("graph");
            doc.AppendChild(graphElement);
            graphElement.SetAttribute("start", data.StartNodeId ?? string.Empty);

            XmlElement nodesElement = doc.CreateElement("nodes");
            graphElement.AppendChild(nodesElement);

            for (int i = 0; i < data.Nodes.Count; i++)
            {
                StepEditorNodeData node = data.Nodes[i];
                XmlElement nodeElement = doc.CreateElement("node");
                nodeElement.SetAttribute("id", node.Id ?? string.Empty);
                nodeElement.SetAttribute("name", node.Name ?? string.Empty);
                nodeElement.SetAttribute("x", node.Position.x.ToString("F2", CultureInfo.InvariantCulture));
                nodeElement.SetAttribute("y", node.Position.y.ToString("F2", CultureInfo.InvariantCulture));
                if (node.Mode == StepNodeMode.Parallel)
                {
                    nodeElement.SetAttribute("mode", "parallel");
                }

                XmlElement actionsElement = doc.CreateElement("actions");
                nodeElement.AppendChild(actionsElement);

                for (int j = 0; j < node.Actions.Count; j++)
                {
                    StepEditorActionData action = node.Actions[j];
                    XmlElement actionElement = doc.CreateElement("action");
                    actionElement.SetAttribute("type", action.TypeName ?? string.Empty);

                    for (int k = 0; k < action.Parameters.Count; k++)
                    {
                        StepEditorParameterData parameter = action.Parameters[k];
                        if (string.IsNullOrEmpty(parameter.Key))
                        {
                            continue;
                        }
                        XmlElement paramElement = doc.CreateElement("param");
                        paramElement.SetAttribute("key", parameter.Key);
                        paramElement.SetAttribute("value", parameter.Value ?? string.Empty);
                        actionElement.AppendChild(paramElement);
                    }

                    actionsElement.AppendChild(actionElement);
                }

                nodesElement.AppendChild(nodeElement);
            }

            XmlElement edgesElement = doc.CreateElement("edges");
            graphElement.AppendChild(edgesElement);

            for (int i = 0; i < data.Edges.Count; i++)
            {
                StepEditorEdgeData edge = data.Edges[i];
                XmlElement edgeElement = doc.CreateElement("edge");
                edgeElement.SetAttribute("from", edge.FromId ?? string.Empty);
                edgeElement.SetAttribute("to", edge.ToId ?? string.Empty);
                edgeElement.SetAttribute("priority", edge.Priority.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(edge.Condition))
                {
                    edgeElement.SetAttribute("condition", edge.Condition);
                }
                edgesElement.AppendChild(edgeElement);
            }

            using (StringWriter writer = new StringWriter())
            {
                doc.Save(writer);
                return writer.ToString();
            }

        }

        /// <summary>
        /// 将编辑器步骤图数据保存为XML文件
        /// </summary>
        public static void SaveToFile(string path, StepEditorGraphData data)
        {
            string xmlText = ExportToText(data);
            File.WriteAllText(path, xmlText);
        }
    }
}
