/***********************************************************************************************************
 * AddressablesDependenciesGraph.cs
 * Copyright (c) Yugo Fujioka - Unity Technologies Japan K.K.
 * 
 * Licensed under the Unity Companion License for Unity-dependent projects--see Unity Companion License.
 * https://unity.com/legal/licenses/unity-companion-license
 * Unless expressly provided otherwise, the Software under this license is made available strictly
 * on an "AS IS" BASIS WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.
 * Please review the license for details on these and other terms and conditions.
***********************************************************************************************************/

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UTJ {
    /// <summary>
    /// Addressables检查节点图中自动解析的依赖关系
    /// </summary>
    internal class AddressableDependenciesGraph : EditorWindow {

        [MenuItem("UTJ/ADDR Dependencies Graph")]
        public static void Open() {
            var window = GetWindow<AddressableDependenciesGraph>();
            window.titleContent = new GUIContent("ADDR Dependencies Graph");
        }

        private Node mainNode = null;
        private GraphView graphView = null;
        private DependenciesRule bundleRule = new DependenciesRule();


        #region MAIN LAYOUT
        private void OnEnable() {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var HELPBOX_HEIGHT = 50f;
            var BUTTON_HEIGHT = 50f;

            this.mainNode = new Node();
            this.mainNode.mainContainer.Clear();
            this.rootVisualElement.Add(mainNode);

            // Space
            var box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.mainNode.mainContainer.Add(box);

            // Select Group
            var groupList = settings.groups.FindAll(group => {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null && schema.IncludeInBuild)
                    return (schema.IncludeAddressInCatalog || schema.IncludeGUIDInCatalog || schema.IncludeLabelsInCatalog);
                return false;
            });
            var selectedGroupFiled = new PopupField<AddressableAssetGroup>("Selected Group", groupList, 0,
                value => value.name,
                value => value.name);
            selectedGroupFiled.name = "SelectedGroup";
            this.mainNode.mainContainer.Add(selectedGroupFiled);

            // Select Entry
            var entryList = new List<AddressableAssetEntry>() { null };
            entryList.AddRange(selectedGroupFiled.value.entries);
            var selectedEntryField = new PopupField<AddressableAssetEntry>("Selected Entry", entryList, 0,
                value => value == null ? "all" : value.address,
                value => value == null ? "all" : value.address);
            selectedEntryField.name = "SelectedEntry";
            this.mainNode.mainContainer.Add(selectedEntryField);

            var enabledShaderToggle = new Toggle("Visible Shader Nodes");
            enabledShaderToggle.name = "VisibleShaders";
            enabledShaderToggle.value = true;
            this.mainNode.mainContainer.Add(enabledShaderToggle);

            // 更改组时更新条目列表
            selectedGroupFiled.RegisterCallback<ChangeEvent<AddressableAssetGroup>>((ev) => {
                entryList.Clear();
                entryList.Add(null);
                foreach (var entry in ev.newValue.entries)
                    entryList.Add(entry);
                selectedEntryField.index = 0;
            });

            // Space
            box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.mainNode.mainContainer.Add(box);

            // Clear Analysis Button
            {
                var helpbox = new HelpBox(
                        "除分析结果\n" +
                        "反映组设置更改时需要。",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(helpbox);

                var viewBundleButton = new Button();
                viewBundleButton.text = "Clear Addressables Analysis";
                viewBundleButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(viewBundleButton);

                viewBundleButton.clicked += () => {
                    this.bundleRule.Clear();
                    if (this.graphView != null) {
                        this.rootVisualElement.Remove(this.graphView);
                        this.graphView = null;
                    }
                };
            }

            // Space
            box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.mainNode.mainContainer.Add(box);

            // Bundle-Dependencies Button
            {
                var helpbox = new HelpBox(
                        "显示 AssetBundle 依赖项\n" +
                        "您可以在加载Bundle包时看到哪些Bundle包被隐式加载。",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(helpbox);

                var viewBundleButton = new Button();
                viewBundleButton.text = "View Bundle-Dependencies";
                viewBundleButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(viewBundleButton);

                viewBundleButton.clicked += () => {
                    this.rootVisualElement.Remove(this.mainNode);
                    if (this.graphView != null)
                        this.rootVisualElement.Remove(this.graphView);
                    this.bundleRule.Execute();

                    var entry = selectedEntryField.index > 0 ? selectedEntryField.value : null;
                    this.graphView = new BundlesGraph(BundlesGraph.TYPE.BUNDLE_DEPENDENCE, selectedGroupFiled.value, entry, enabledShaderToggle.value, this.bundleRule);
                    this.rootVisualElement.Add(this.graphView);
                    this.rootVisualElement.Add(this.mainNode);
                };
            }

            // Space
            box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.mainNode.mainContainer.Add(box);

            // Asset-Dependencies Button
            {
                var helpbox = new HelpBox(
                        "显示 AssetBundle 中包含的资产的依赖关系\n" +
                        "可以找到意外的引用。",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(helpbox);

                var viewAssetButton = new Button();
                viewAssetButton.text = "View Asset-Dependencies";
                viewAssetButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.mainNode.mainContainer.Add(viewAssetButton);

                viewAssetButton.clicked += () => {
                    this.rootVisualElement.Remove(this.mainNode);
                    if (this.graphView != null)
                        this.rootVisualElement.Remove(this.graphView);
                    this.bundleRule.Execute();

                    var entry = selectedEntryField.index > 0 ? selectedEntryField.value : null;
                    this.graphView = new BundlesGraph(BundlesGraph.TYPE.ASSET_DEPENDENCE, selectedGroupFiled.value, entry, enabledShaderToggle.value, this.bundleRule);
                    this.rootVisualElement.Add(this.graphView);
                    this.rootVisualElement.Add(this.mainNode);
                };
            }
        }
        #endregion


        #region BUNDLE RULE
        /// <summary>
        ///  Analyze
        /// </summary>
        internal class DependenciesRule : BundleRuleBase {
            public AddressableAssetsBuildContext context { get; private set; }
            public ExtractDataTask extractData { get; private set; }
            public List<AssetBundleBuild> bunldeInfos { get; private set; }

            public delegate bool IsPathCallback(string path);
            public IsPathCallback IsPathValidForEntry { get; private set; }

            public DependenciesRule() {
                // Utility
                var aagAssembly = typeof(AddressableAssetGroup).Assembly;
                var aauType = aagAssembly.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetUtility");
                var validMethod = aauType.GetMethod("IsPathValidForEntry", BindingFlags.Static | BindingFlags.NonPublic, null, new System.Type[] { typeof(string) }, null);
                this.IsPathValidForEntry = System.Delegate.CreateDelegate(typeof(IsPathCallback), validMethod) as IsPathCallback;
            }
            public void Execute() {
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                if (this.context == null) {
                    CalculateInputDefinitions(settings);
                    this.context = GetBuildContext(settings);

                    var exitCode = RefreshBuild(this.context);
                    if (exitCode < ReturnCode.Success) {
                        Debug.LogError($"Analyze build failed. {exitCode}");
                        return;
                    }

                    // Addressables 1.20 及更高版本不再需要反射
                    //this.extractData = this.ExtractData;
                    var extractDataField = this.GetType().GetField("m_ExtractData", BindingFlags.Instance | BindingFlags.NonPublic);
                    this.extractData = (ExtractDataTask)extractDataField.GetValue(this);

                    var bundleInfoField = this.GetType().GetField("m_AllBundleInputDefs", BindingFlags.Instance | BindingFlags.NonPublic);
                    this.bunldeInfos = (List<AssetBundleBuild>)bundleInfoField.GetValue(this);
                }
            }

            public void Clear() {
                this.context = null;
                this.extractData = null;
                this.bunldeInfos = null;

                // 分析常用处理
                ClearAnalysis();
                if (!BuildUtility.CheckModifiedScenesAndAskToSave()) {
                    Debug.LogError("Cannot run Analyze with unsaved scenes");
                    return;
                }
            }
        }
        #endregion


        #region NODE
        /// <summary>
        /// 节点扩展
        /// </summary>
        internal class BundleNode : Node {
            public string bundleName = string.Empty;
            public Dictionary<string, Port> input = new Dictionary<string, Port>();     // InputContainer 中注册的端口
            public Dictionary<string, Port> output = new Dictionary<string, Port>();    // 在 OutputContainer 中注册的端口
            public Dictionary<string, GUID> assetGuid = new Dictionary<string, GUID>(); // 在端口注册的资产的 GUID（*缓存，因为它在资产依赖中使用）
            public Dictionary<Port, HashSet<Port>> connectTo = new Dictionary<Port, HashSet<Port>>();       // 连接端口
            public Dictionary<Port, List<FlowingEdge>> edgeTo = new Dictionary<Port, List<FlowingEdge>>();  // 连接边缘
            public Dictionary<Port, List<FlowingEdge>> edgeFrom = new Dictionary<Port, List<FlowingEdge>>();// 连接边缘

            public BundleNode() {
                this.capabilities &= ~Capabilities.Deletable; // 禁止删除
            }

            public override void OnSelected() {
                base.OnSelected();

                foreach (var pair in edgeTo) {
                    foreach (var edge in pair.Value)
                        edge.activeFlow = true;
                }
                foreach (var pair in edgeFrom) {
                    foreach (var edge in pair.Value)
                        edge.selected = true; // 从输出更改颜色
                }
            }
            public override void OnUnselected() {
                base.OnUnselected();

                foreach (var pair in edgeTo) {
                    foreach (var edge in pair.Value)
                        edge.activeFlow = false;
                }
                foreach (var pair in edgeFrom) {
                    foreach (var edge in pair.Value)
                        edge.selected = false;
                }
            }
        }
        #endregion


        #region GraphView
        /// <summary>
        /// Graph表示
        /// </summary>
        internal class BundlesGraph : GraphView {

            public enum TYPE {
                BUNDLE_DEPENDENCE, // 捆绑包之间的依赖关系
                ASSET_DEPENDENCE,  // bundle中资产之间的依赖关系
            }

            private const string UNITY_BUILTIN_SHADERS = "unitybuiltinshaders";
            private GUID UNITY_BUILTIN_SHADERS_GUID = new GUID("0000000000000000e000000000000000"); // 来自 SBP 的 CommonSettings.cs

            private const float NODE_OFFSET_H = 140f;
            private const float NODE_OFFSET_V = 20f;

            private BundleNode shaderNode, builtinNode;
            private List<FlowingEdge> allEdges = new List<FlowingEdge>();
            private Dictionary<string, BundleNode> bundleNodes = new Dictionary<string, BundleNode>();
            private List<string> explicitNodes = new List<string>();

            /// <summary>
            /// 所有 Bundle 依赖项
            /// </summary>
            public BundlesGraph(TYPE type, AddressableAssetGroup group, AddressableAssetEntry entry, bool enabledShaderNode, DependenciesRule rule) {
                switch (type) {
                    case TYPE.BUNDLE_DEPENDENCE:
                        this.ViewBundles(rule, group, entry);
                        break;
                    case TYPE.ASSET_DEPENDENCE:
                        this.ViewEntries(rule, group, entry);
                        break;
                }
                // NOTE:如果一次布局没有完成，就无法获取节点大小，所以在View的预渲染回调中设置。
                var position = new Vector2(0f, 50f);
                this.RegisterCallback<GeometryChangedEvent>((ev) => {
                    var position = new Vector2(400f, 50f);
                    var parentStack = new HashSet<string>(); // 父节点
                    var placedNames = new HashSet<string>(); // 对齐节点
                    foreach (var bundleName in this.explicitNodes)
                        position = this.AlignNode(rule.context, bundleName, parentStack, placedNames, enabledShaderNode, position);
                });

                this.StretchToParentSize(); // 设置 GraphView 大小以匹配父级的大小
                this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale); // 滚动缩放
                this.AddManipulator(new ContentDragger());      // 绘制区域拖动
                this.AddManipulator(new SelectionDragger());    // 选择节点拖动
                this.AddManipulator(new RectangleSelector());   // 范围节点选择
            }


            #region PRIVATE FUNCTION
            /// <summary>
            /// 节点对齐
            /// </summary>
            private Vector2 AlignNode(AddressableAssetsBuildContext context, string bundleName, HashSet<string> parentStack, HashSet<string> placedNodes, bool enabledShaderNode, Vector2 position) {
                if (this.bundleNodes.TryGetValue(bundleName, out var node)) {
                    if (!enabledShaderNode) {
                        if (node == this.shaderNode || node == this.builtinNode) {
                            node.visible = false;
                            foreach (var edgeList in node.edgeFrom.Values) {
                                foreach (var edge in edgeList)
                                    edge.visible = false;
                            }
                            return position;
                        }
                    }

                    var rect = node.GetPosition();

                    if (!placedNodes.Contains(bundleName)) {
                        rect.x = position.x;
                        rect.y = position.y;
                        node.SetPosition(rect);
                        placedNodes.Add(bundleName);
                    }

                    parentStack.Add(bundleName);

                    var addChild = false;
                    if (context.bundleToImmediateBundleDependencies.TryGetValue(bundleName, out var depBundleNames)) {
                        // 按字母顺序排序
                        depBundleNames.Sort(CompareGroup);

                        if (depBundleNames.Count > 1) {
                            var pos = position;
                            pos.x += rect.width + NODE_OFFSET_H;
                            foreach (var depBundleName in depBundleNames) {
                                // 忽略自己
                                if (depBundleName == bundleName)
                                    continue;
                                // 循环参照
                                if (parentStack.Contains(depBundleName)) {
                                    if (this.bundleNodes.TryGetValue(depBundleName, out var depNode))
                                        Debug.LogWarning($"循环参照 : {node.title} <-> {depNode.title}");
                                    continue;
                                }
                                if (placedNodes.Contains(depBundleName))
                                    continue;

                                pos = this.AlignNode(context, depBundleName, parentStack, placedNodes, enabledShaderNode, pos);
                                position.y = pos.y;
                                addChild = true;
                            }
                        }
                    }

                    parentStack.Remove(bundleName);

                    if (!addChild)
                        position.y += rect.height + NODE_OFFSET_V;

                    position.y = Mathf.Max(position.y, rect.y + rect.height + NODE_OFFSET_V);
                }

                return position;
            }

            /// <summary>
            /// 新节点
            /// </summary>
            /// <param name="bundleName">AssetBundle名</param>
            /// <param name="isExplicit">组是显式调用的（添加到目录中）还是</param>
            private BundleNode CreateBundleNode(string bundleName, bool isExplicit, AddressableAssetsBuildContext context) {
                if (this.bundleNodes.TryGetValue(bundleName, out var node)) {
                    Debug.LogWarning($"Exist the same bundleName for Nodes : {bundleName}");
                    return node;
                }

                node = new BundleNode();
                node.name = node.bundleName = bundleName;

                // 节点名称
                if (bundleName.Contains(UNITY_BUILTIN_SHADERS)) {
                    node.title = "Unity Built-in Shaders";
                    this.builtinNode = node;
                } else {
                    // 删除哈希并与组名结合
                    var temp = System.IO.Path.GetFileName(bundleName).Split(new string[] { "_assets_", "_scenes_" }, System.StringSplitOptions.None);
                    node.title = temp[temp.Length - 1];
                    if (context.bundleToAssetGroup.TryGetValue(bundleName, out var groupGUID)) {
                        var groupName = context.Settings.FindGroup(findGroup => findGroup != null && findGroup.Guid == groupGUID).name;
                        node.title = $"{groupName}/{node.title}";

                        if (this.shaderNode == null && groupName == AddressablesAutoGrouping.SHADER_GROUP_NAME)
                            this.shaderNode = node;
                    }
                }

                // 显式节点为黄色
                if (isExplicit)
                    node.titleContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0f, 1f);

                this.AddElement(node);
                this.bundleNodes.Add(bundleName, node);

                // 标题旁边的删除按钮
                node.titleButtonContainer.Clear();
                node.titleButtonContainer.Add(new Label(string.Empty)); // NOTE: 不填空的话会被打包，看起来很糟糕

                return node;
            }

            /// <summary>
            /// 创建输入端口
            /// </summary>
            private Port CreateInputPort(BundleNode node, string portName, string keyName) {
                var input = Port.Create<FlowingEdge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
                input.portName = portName;
                input.capabilities = 0;
                node.inputContainer.Add(input);
                node.input.Add(keyName, input);
                return input;
            }

            /// <summary>
            /// 边缘连接
            /// </summary>
            private void AddEdge(BundleNode fromNode, Port from, BundleNode toNode, Port to) {
                var addEdge = false;
                if (fromNode.connectTo.TryGetValue(from, out var ports)) {
                    // 第二次和后续连接
                    // NOTE: 当多个 SubAsset 对同一个 Asset 有依赖关系时发生，所以要避免，主要发生在 Scene
                    if (!ports.Contains(to))
                        addEdge = true;
                } else {
                    // 初始连接
                    addEdge = true;
                    ports = new HashSet<Port>();
                    fromNode.connectTo.Add(from, ports);
                }
                if (addEdge) {
                    // 创建新边缘
                    var edge = from.ConnectTo<FlowingEdge>(to);
                    this.AddElement(edge);
                    if (!fromNode.edgeTo.TryGetValue(from, out var toEdges)) {
                        toEdges = new List<FlowingEdge>();
                        fromNode.edgeTo.Add(from, toEdges);
                    }
                    if (!toNode.edgeFrom.TryGetValue(to, out var fromEdges)) {
                        fromEdges = new List<FlowingEdge>();
                        toNode.edgeFrom.Add(to, fromEdges);
                    }
                    toEdges.Add(edge);
                    fromEdges.Add(edge);
                    ports.Add(to);
                    this.allEdges.Add(edge);
                }
            }

            static System.Text.RegularExpressions.Regex NUM_REGEX = new System.Text.RegularExpressions.Regex(@"[^0-9]");
            private static int CompareGroup(string a, string b) {
                if (a.Contains(UNITY_BUILTIN_SHADERS))
                    return -1;
                if (b.Contains(UNITY_BUILTIN_SHADERS))
                    return 1;

                var ret = string.CompareOrdinal(a, b);
                // 将数字与不同的位数对齐
                var regA = NUM_REGEX.Replace(a, string.Empty);
                var regB = NUM_REGEX.Replace(b, string.Empty);
                if ((regA.Length > 0 && regB.Length > 0) && regA.Length != regB.Length) {
                    if (ret > 0 && regA.Length < regB.Length)
                        return -1;
                    else if (ret < 0 && regA.Length > regB.Length)
                        return 1;
                }

                return ret;
            }
            private static int CompareGroup(VisualElement a, VisualElement b) {
                return CompareGroup(a.name, b.name);
            }
            #endregion


            #region BUNDLES
            /// <summary>
            /// 将内容注册到输出端口
            /// </summary>
            private void CreateOutputPort(BundleNode node) {
                if (node.bundleName.Contains(UNITY_BUILTIN_SHADERS))
                    return;

                var output = Port.Create<FlowingEdge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                output.portName = string.Empty;
                output.capabilities = 0;
                node.outputContainer.Add(output);
                node.output.Add(node.bundleName, output);
            }
            
            void AddBundleNode(DependenciesRule rule, BundleNode parentNode, string bundleName) {
                var onlyConnnect = false;
                if (this.bundleNodes.TryGetValue(bundleName, out var node)) {
                    onlyConnnect = true;
                } else {
                    // 报名
                    node = this.CreateBundleNode(bundleName, false, rule.context);

                    // 连接端口
                    this.CreateInputPort(node, string.Empty, parentNode.bundleName);
                }

                // 联系
                var input = node.inputContainer.ElementAt(0) as Port;
                var parentPort = parentNode.outputContainer.ElementAt(0) as Port;
                if (rule.context.bundleToImmediateBundleDependencies.TryGetValue(parentNode.bundleName, out var depNames)) {
                    if (depNames.Contains(bundleName))
                        this.AddEdge(parentNode, parentPort, node, input);
                }

                if (onlyConnnect)
                    return;

                // 创建任何依赖项
                if (rule.context.bundleToImmediateBundleDependencies.TryGetValue(bundleName, out var depBundleNames)) {
                    // 按字母顺序排序
                    depBundleNames.Sort(CompareGroup);

                    if (depBundleNames.Count > 1) {
                        var output = Port.Create<FlowingEdge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                        output.portName = name;
                        output.capabilities = 0;
                        node.outputContainer.Add(output);

                        // 递归遍历
                        foreach (var depBundleName in depBundleNames) {
                            // 跳过，因为你被包括在内
                            if (bundleName == depBundleName)
                                continue;

                            this.AddBundleNode(rule, node, depBundleName);
                        }
                    }
                }

                node.RefreshPorts();
                node.RefreshExpandedState();
            }

            void ViewBundles(DependenciesRule rule, AddressableAssetGroup group, AddressableAssetEntry entry) {
                this.bundleNodes.Clear();
                var context = rule.context;

                if (context.assetGroupToBundles.TryGetValue(group, out var bundleNames)) {
                    foreach (var bundleName in bundleNames) {
                        if (bundleName.Contains(UNITY_BUILTIN_SHADERS))
                            continue;

                        // 按指定条目名称过滤
                        if (entry != null) {
                            var hit = false;
                            var bundleInfo = rule.bunldeInfos.Find(val => val.assetBundleName == bundleName);
                            foreach (var assetName in bundleInfo.assetNames) {
                                if (assetName == entry.AssetPath) {
                                    hit = true;
                                    break;
                                }
                            }
                            if (!hit)
                                continue;
                        }

                        var node = this.CreateBundleNode(bundleName, true, context);
                        this.explicitNodes.Add(bundleName);

                        //创建隐式节点
                        if (context.bundleToImmediateBundleDependencies.TryGetValue(bundleName, out var depBundleNames)) {
                            if (depBundleNames.Count > 1) {
                                this.CreateOutputPort(node);

                                // 递归遍历
                                foreach (var depBundleName in depBundleNames) {
                                    // 跳过，因为你被包括在内
                                    if (bundleName == depBundleName)
                                        continue;

                                    this.AddBundleNode(rule, node, depBundleName);
                                }
                            }
                        }
                        node.RefreshPorts();
                        node.RefreshExpandedState();
                    }
                }
            }
            #endregion


            #region ENTRIES
            /// <summary>
            /// 将内容注册到输出端口
            /// </summary>
            private void CreateOutputPortsWithAssets(DependenciesRule rule, BundleNode node) {
                if (node.bundleName.Contains(UNITY_BUILTIN_SHADERS)) {
                    node.assetGuid.Add(UNITY_BUILTIN_SHADERS, UNITY_BUILTIN_SHADERS_GUID);
                    return;
                }

                // 内容登记
                var info = rule.bunldeInfos.Find((info) => {
                    return info.assetBundleName == node.bundleName;
                });

                foreach (var assetName in info.assetNames) {
                    var output = Port.Create<FlowingEdge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                    output.portName = assetName;
                    output.capabilities = 0;
                    node.outputContainer.Add(output);
                    node.output.Add(assetName, output);
                    node.assetGuid.Add(assetName, new GUID(AssetDatabase.AssetPathToGUID(assetName)));
                }

                // 按字母顺序
                node.outputContainer.Sort(CompareGroup);
            }

            int AddEntriesNode(DependenciesRule rule, BundleNode parentNode, string bundleName) {
                var onlyConnect = false;

                if (this.bundleNodes.TryGetValue(bundleName, out var node)) {
                    onlyConnect = true;
                } else {
                    // 报名
                    node = this.CreateBundleNode(bundleName, false, rule.context);

                    // 内容物表示
                    this.CreateOutputPortsWithAssets(rule, node);
                }

                // 遍历父条目资产
                foreach (var pair in parentNode.output) {
                    // ExtractData.DependencyData从中获取资产依赖项
                    // NOTE: AssetDatabase.GetDependencies但是我可以得到它，但是它很慢，所以我拒绝了它
                    var parentAsset = pair.Key;
                    var parentPort = pair.Value;
                    var parentGuid = new GUID(AssetDatabase.AssetPathToGUID(parentAsset));
                    ICollection<ObjectIdentifier> parentObjects = null;

                    // 场景特殊，所以分开
                    if (parentAsset.Contains(".unity")) {
                        if (rule.extractData.DependencyData.SceneInfo.TryGetValue(parentGuid, out var parentDependencies))
                            parentObjects = parentDependencies.referencedObjects;
                    } else {
                        if (rule.extractData.DependencyData.AssetInfo.TryGetValue(parentGuid, out var parentDependencies))
                            parentObjects = parentDependencies.referencedObjects;
                    }
                    if (parentObjects == null)
                        continue;

                    foreach (var parentObj in parentObjects) {
                        // 兼容内置着色器
                        if (parentObj.guid == UNITY_BUILTIN_SHADERS_GUID) {
                            if (node.assetGuid.ContainsKey(UNITY_BUILTIN_SHADERS)) {
                                Port input = null;
                                if (node.inputContainer.childCount == 0)
                                    input = this.CreateInputPort(node, string.Empty, UNITY_BUILTIN_SHADERS); // 新規Port作成
                                else
                                    input = node.inputContainer.ElementAt(0) as Port;

                                // Port间接続
                                this.AddEdge(parentNode, parentPort, node, input);
                            }
                            continue;
                        }

                        // 排除脚本
                        var parentPath = AssetDatabase.GUIDToAssetPath(parentObj.guid);
                        if (!rule.IsPathValidForEntry(parentPath))
                            continue;
                        // Prefab 被排除在外，因为它不是依赖项
                        if (parentPath.Contains(".prefab"))
                            continue;

                        // 节点连接
                        foreach (var myAssetName in node.output.Keys) {
                            if (node.assetGuid.TryGetValue(myAssetName, out var myGuid)) {
                                // MainAsset判定
                                var hit = (parentObj.guid == myGuid);
                                // 子资产搜索
                                if (!hit) {
                                    if (rule.extractData.DependencyData.AssetInfo.TryGetValue(myGuid, out var myAllAssets)) {
                                        foreach (var refAsset in myAllAssets.referencedObjects) {
                                            // NOTE: 由于创建实例存在内存开销，但简单判断 SubAsset 更安全
                                            var instance = ObjectIdentifier.ToObject(refAsset);
                                            if (!AssetDatabase.IsSubAsset(instance))
                                                continue;
                                            if (parentObj == refAsset) {
                                                hit = true;
                                                break;
                                            }
                                            //if (parentObj == refAsset) {
                                            //    // 如果引用但 GUID 是独立的，则不考虑 SubAsset
                                            //    // e.g. fbx包含的材质/纹理/着色器不是子资产，因为分配了 GUID
                                            //    if (parentObj.guid != refAsset.guid)
                                            //        hit = true;
                                            //    break;
                                            //}
                                        }
                                    }
                                }

                                // 发现依赖关系
                                if (hit) {
                                    if (!node.input.TryGetValue(myAssetName, out var input))
                                        input = this.CreateInputPort(node, myAssetName, myAssetName); //新规Port作成

                                    // Port間接続
                                    this.AddEdge(parentNode, parentPort, node, input);
                                }
                            }
                        }
                    }
                }

                if (onlyConnect)
                    return 0;

                var totalDepCount = 0;
                // 创建任何依赖项
                if (rule.context.bundleToImmediateBundleDependencies.TryGetValue(bundleName, out var depBundleNames)) {
                    if (depBundleNames.Count > 1) {
                        // 递归遍历
                        foreach (var depBundleName in depBundleNames) {
                            // 跳过，因为你被包括在内
                            if (bundleName == depBundleName)
                                continue;

                            totalDepCount += this.AddEntriesNode(rule, node, depBundleName);
                        }
                    }
                }
                totalDepCount = Mathf.Max(totalDepCount, 1);

                return totalDepCount;
            }

            void ViewEntries(DependenciesRule rule, AddressableAssetGroup group, AddressableAssetEntry entry) {
                this.bundleNodes.Clear();
                var context = rule.context;

                if (context.assetGroupToBundles.TryGetValue(group, out var bundleNames)) {
                    foreach (var bundleName in bundleNames) {
                        if (bundleName.Contains(UNITY_BUILTIN_SHADERS))
                            continue;

                        // 按指定条目名称过滤
                        if (entry != null) {
                            var hit = false;
                            var bundleInfo = rule.bunldeInfos.Find(val => val.assetBundleName == bundleName);
                            foreach (var assetName in bundleInfo.assetNames) {
                                if (assetName == entry.AssetPath) {
                                    hit = true;
                                    break;
                                }
                            }
                            if (!hit)
                                continue;
                        }

                        // 创建显式节点
                        var node = this.CreateBundleNode(bundleName, true, rule.context);
                        this.explicitNodes.Add(bundleName);

                        // 内容物表示
                        this.CreateOutputPortsWithAssets(rule, node);

                        // 创建隐式节点
                        var totalDepth = 0;
                        if (context.bundleToImmediateBundleDependencies.TryGetValue(bundleName, out var depBundleNames)) {
                            if (depBundleNames.Count > 1) {
                                // 递归遍历
                                foreach (var depBundleName in depBundleNames) {
                                    // 跳过，因为你被包括在内
                                    if (bundleName == depBundleName)
                                        continue;

                                    var depCount = this.AddEntriesNode(rule, node, depBundleName);
                                    totalDepth += depCount;
                                }
                            }
                        }
                        node.RefreshPorts();
                        node.RefreshExpandedState();
                    }
                }
            }
            #endregion

            // MEMO: 右键单击上下文菜单
            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
                // 这次是只确认视图，所以留空（无输出）
            }
        }
        #endregion
    }
}