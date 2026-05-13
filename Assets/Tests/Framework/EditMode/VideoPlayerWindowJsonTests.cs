using System.IO;
using LitJson;
using NUnit.Framework;

namespace LWFramework.Tests.Framework.EditMode
{
    /// <summary>
    /// 视频播放器窗口 JSON 结构验证。
    /// </summary>
    public sealed class VideoPlayerWindowJsonTests
    {
        private const string JsonPath = "Assets/UIJsonData/VideoPlayerWindowView.json";

        /// <summary>
        /// 验证 JSON 文件存在且 Root 信息正确。
        /// </summary>
        [Test]
        public void VideoPlayerWindowJson_ShouldExistAndHaveExpectedRoot()
        {
            Assert.That(File.Exists(JsonPath), Is.True, "视频播放器 JSON 文件不存在。");

            JsonData root = LoadRoot();

            Assert.AreEqual("VideoPlayerWindowView", root["name"].ToString());
            Assert.AreEqual("Dark_Developer_Tool_IDE", root["themeName"].ToString());
        }

        /// <summary>
        /// 验证窗口层级、RawImage 与 Slider 结构齐全。
        /// </summary>
        [Test]
        public void VideoPlayerWindowJson_ShouldContainRequiredControls()
        {
            JsonData root = LoadRoot();
            JsonData window = FindChild(root, "PnlWindow");
            JsonData top = FindChild(window, "PnlTop");
            JsonData video = FindChild(window, "PnlVideo");
            JsonData bottom = FindChild(window, "PnlBottom");
            JsonData progress = FindChild(bottom, "SldProgress");

            Assert.NotNull(window);
            Assert.NotNull(top);
            Assert.NotNull(video);
            Assert.NotNull(bottom);
            Assert.NotNull(FindChild(top, "BtnZoom"));
            Assert.NotNull(FindChild(top, "BtnFullscreen"));
            Assert.NotNull(FindChild(top, "BtnClose"));
            Assert.NotNull(FindChild(video, "RImgVideo"));
            Assert.NotNull(FindChild(video, "TxtEmptyHint"));
            Assert.NotNull(FindChild(bottom, "BtnPlay"));
            Assert.NotNull(FindChild(bottom, "BtnPause"));
            Assert.NotNull(FindChild(bottom, "BtnShrink"));
            Assert.NotNull(FindChild(bottom, "TxtCurrentTime"));
            Assert.NotNull(FindChild(bottom, "TxtDuration"));
            Assert.NotNull(progress);

            JsonData sliderData = GetComponentData(progress, "Slider");
            Assert.NotNull(sliderData);
            Assert.AreEqual("ImgFill", sliderData["fillRect"].ToString());
            Assert.AreEqual("ImgHandle", sliderData["handleRect"].ToString());
            Assert.AreEqual("LeftToRight", sliderData["direction"].ToString());

            Assert.NotNull(GetComponentData(FindChild(video, "RImgVideo"), "RawImage"));
        }

        /// <summary>
        /// 读取 Root 节点，避免重复解析。
        /// </summary>
        private static JsonData LoadRoot()
        {
            string json = File.ReadAllText(JsonPath);
            JsonData rootData = JsonMapper.ToObject(json);
            return rootData["Root"];
        }

        /// <summary>
        /// 在当前节点的直接子节点中按名称查找。
        /// </summary>
        private static JsonData FindChild(JsonData parent, string name)
        {
            if (parent == null || !parent.ContainsKey("children"))
            {
                return null;
            }

            JsonData children = parent["children"];
            for (int i = 0; i < children.Count; i++)
            {
                JsonData child = children[i];
                if (child != null && child["name"].ToString() == name)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定组件的数据块。
        /// </summary>
        private static JsonData GetComponentData(JsonData node, string componentType)
        {
            if (node == null || !node.ContainsKey("components"))
            {
                return null;
            }

            JsonData components = node["components"];
            for (int i = 0; i < components.Count; i++)
            {
                JsonData component = components[i];
                if (component["type"].ToString() == componentType)
                {
                    return component["data"];
                }
            }

            return null;
        }
    }
}
