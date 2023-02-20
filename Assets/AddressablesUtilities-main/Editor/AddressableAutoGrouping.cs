/***********************************************************************************************************
 * CreateSharedAssetsGroup.cs
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
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.U2D;

namespace UTJ
{
    internal class AddressablesAutoGrouping : EditorWindow {

        //#region DEFINE
        public const string SHARED_GROUP_NAME = "Shared-";
        public const string SHADER_GROUP_NAME = "Shared-Shader";
        public const string SINGLE_GROUP_NAME = "Shared-Single";
        //#endregion


        //#region MAIN LAYOUT
        [MenuItem("UTJ/ADDR Auto-Grouping Window")]
        private static void OpenWindow() {
            var window = GetWindow<AddressablesAutoGrouping>();
            window.titleContent = new GUIContent("ADDR Auto-Grouping");
            var rect = window.position;
            rect.size = new Vector2(400f, 400f);
            window.position = rect;
        }

        private void OnEnable() {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var HELPBOX_HEIGHT = 50f;
            var BUTTON_HEIGHT = 50f;

            // Space
            var box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.rootVisualElement.Add(box);

            {
                // Info
                var helpbox = new HelpBox(
                        "批量删除自动生成的组\n" +
                        "在开发和测试期间使用,",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(helpbox);

                // Remove Button
                var removeGroupButton = new Button();
                removeGroupButton.text = "Remove Shared Group";
                removeGroupButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(removeGroupButton);

                removeGroupButton.clicked += () => {
                    var deletedGroupList = new List<AddressableAssetGroup>();
                    foreach (var group in settings.groups) {
                        //if (group.ReadOnly && group.GetSchema<PlayerDataGroupSchema>() == null)
                        if (group.name.Contains(SHARED_GROUP_NAME) ||
                            group.name.Contains(SHADER_GROUP_NAME) ||
                            group.name.Contains(SINGLE_GROUP_NAME))
                            deletedGroupList.Add(group);
                    }
                    foreach (var group in deletedGroupList)
                        settings.RemoveGroup(group);
                };
            }

            // Space
            box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.rootVisualElement.Add(box);

            {
                // Info
                var helpbox = new HelpBox(
                        "创建共享资产组以解决重复资产\n" +
                        "已经输入的资产不会改变",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(helpbox);

                // Config
                var shaderGroupToggle = new Toggle("Shader Group");
                shaderGroupToggle.name = "ShaderGroup";
                shaderGroupToggle.value = true;
                this.rootVisualElement.Add(shaderGroupToggle);

                var thresholdField = new IntegerField("Threshold (KiB)");
                thresholdField.name = "Threshold";
                thresholdField.value = 0;
                this.rootVisualElement.Add(thresholdField);

                var createGroupButton = new Button();
                createGroupButton.text = "Create Shared Assets Group";
                createGroupButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(createGroupButton);

                createGroupButton.clicked += () => {
                    var instance = new CreateSharedAssetsGroup();
                    instance.Execute(shaderGroupToggle.value, thresholdField.value);
                };
            }

            // Space
            box = new Box();
            box.style.height = new Length(10f, LengthUnit.Pixel);
            this.rootVisualElement.Add(box);

            {
                // Info
                var helpbox = new HelpBox(
                        "将所有依赖资产打包成单独的Bundle",
                        HelpBoxMessageType.Info
                    );
                helpbox.style.height = new Length(HELPBOX_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(helpbox);

                var implicitGroupButton = new Button();
                implicitGroupButton.text = "Create Implicit Group (All single)";
                implicitGroupButton.style.height = new Length(BUTTON_HEIGHT, LengthUnit.Pixel);
                this.rootVisualElement.Add(implicitGroupButton);

                implicitGroupButton.clicked += () => {
                    var instance = new CreateSharedAssetsGroup();
                    instance.ExecuteSingle();
                };
            }
        }
        //#endregion


        /// <summary>
        /// Addressables自动分组以获得最佳重复分辨率
        /// 检测隐式依赖Assets（ImplicitAssets）并分组拥有相同依赖关系的Assets和groups（=Assetbundle）
        /// </summary>
        class CreateSharedAssetsGroup : BundleRuleBase {
            delegate bool IsPathCallback(string path);
            IsPathCallback IsPathValidForEntry = null;
            delegate long GetMemorySizeLongCallback(Texture tex);
            GetMemorySizeLongCallback GetStorageMemorySizeLong = null;
            ExtractDataTask ExtractData = null;

            public CreateSharedAssetsGroup() {
                // Utilityの取得
                var aagAssembly = typeof(AddressableAssetGroup).Assembly;
                var aauType = aagAssembly.GetType("UnityEditor.AddressableAssets.Settings.AddressableAssetUtility");
                var validMethod = aauType.GetMethod("IsPathValidForEntry", BindingFlags.Static | BindingFlags.NonPublic, null, new System.Type[] { typeof(string) }, null);
                this.IsPathValidForEntry = System.Delegate.CreateDelegate(typeof(IsPathCallback), validMethod) as IsPathCallback;

                var editorAssembly = typeof(TextureImporter).Assembly;
                var utilType = editorAssembly.GetType("UnityEditor.TextureUtil");
                var utilMethod = utilType.GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public, null, new System.Type[] { typeof(Texture) }, null);
                this.GetStorageMemorySizeLong = System.Delegate.CreateDelegate(typeof(GetMemorySizeLongCallback), utilMethod) as GetMemorySizeLongCallback;
            }

            /// <summary>
            /// SharedAsset 组信息
            /// </summary>
            class SharedGroupParam {
                public string name = SHARED_GROUP_NAME + "{0}";
                public List<string> bundles;                // 依赖包
                public List<ImplicitParam> implicitParams;  // 包含的隐式资产
            }

            /// <summary>
            /// 为隐式依赖资产收集的信息
            /// </summary>
            private class ImplicitParam {
                public string guid;
                public string path;
                public bool isSubAsset;             // 是否为子资产
                public List<System.Type> usedType;  // 使用的子资产类型（用于 fbx ）
                public List<string> bundles;        // 引用包
                public long fileSize;               // 资产文件大小
            }

            public void ExecuteSingle() {
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                // 分析常用处理
                ClearAnalysis();
                if (!BuildUtility.CheckModifiedScenesAndAskToSave()) {
                    Debug.LogError("Cannot run Analyze with unsaved scenes");
                    return;
                }
                CalculateInputDefinitions(settings);
                var context = GetBuildContext(settings);
                var exitCode = RefreshBuild(context);
                if (exitCode < ReturnCode.Success) {
                    Debug.LogError($"Analyze build failed. {exitCode}");
                    return;
                }
                // NOTE: 1.20 后不需要反射
                //this.extractData = this.ExtractData;
                var extractDataField = this.GetType().GetField("m_ExtractData", BindingFlags.Instance | BindingFlags.NonPublic);
                this.ExtractData = (ExtractDataTask)extractDataField.GetValue(this);

                // 提取隐式依赖资产信息
                var implicitParams = new List<ImplicitParam>();
                this.GetImplicitAssetsParam(context, implicitParams, null);

                // Group分布
                var singleGroup = settings.groups.Find(group => { return (group.name.Contains(SINGLE_GROUP_NAME)); });
                if (singleGroup == null) {
                    singleGroup = CreateSharedGroup(settings, SINGLE_GROUP_NAME);
                    var schema = singleGroup.GetSchema<BundledAssetGroupSchema>();
                    schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                }
                foreach (var implicitParam in implicitParams) {
                    var entry = settings.CreateOrMoveEntry(implicitParam.guid, singleGroup, readOnly: false, postEvent: false);
                    var addr = System.IO.Path.GetFileNameWithoutExtension(implicitParam.path);
                    entry.SetAddress(addr, postEvent: false);
                }
                // 如果为空则不需要
                if (singleGroup.entries.Count == 0)
                    settings.RemoveGroup(singleGroup);

                // 字母数字排序
                settings.groups.Sort(CompareGroup);

                // 反映
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, eventData: null, postEvent: true, settingsModified: true);
            }

            /// <summary>
            /// 执行
            /// </summary>
            public void Execute(bool collectShader, int thresholdSingleAsset) {
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                // 单个Bundle的资产文件大小阈值
                var SEPARATE_ASSET_SIZE = (long)thresholdSingleAsset * 1024L;

                // 分析常用处理
                ClearAnalysis();
                if (!BuildUtility.CheckModifiedScenesAndAskToSave()) {
                    Debug.LogError("Cannot run Analyze with unsaved scenes");
                    return;
                }
                CalculateInputDefinitions(settings);
                var context = GetBuildContext(settings);
                var exitCode = RefreshBuild(context);
                if (exitCode < ReturnCode.Success) {
                    Debug.LogError($"Analyze build failed. {exitCode}");
                    return;
                }
                // 1.20 后不需要反射
                //this.extractData = this.ExtractData;
                var extractDataField = this.GetType().GetField("m_ExtractData", BindingFlags.Instance | BindingFlags.NonPublic);
                this.ExtractData = (ExtractDataTask)extractDataField.GetValue(this);

                // 提取隐式依赖资产信息
                var implicitParams = new List<ImplicitParam>();
                var atlases = new List<SpriteAtlas>();
                this.GetImplicitAssetsParam(context, implicitParams, atlases);

                // 已放置的 SharedAsset 组数
                var sharedGroupCount = settings.groups.FindAll(group => { return (group.name.Contains(SHARED_GROUP_NAME)); }).Count;

                var sharedGroupParams = new List<SharedGroupParam>();
                var collectionGroupParams = new List<SharedGroupParam>();
                var shaderGroupParam = new SharedGroupParam() {
                    name = SHADER_GROUP_NAME,
                    implicitParams = new List<ImplicitParam>(),
                };
                var singleGroupParam = new SharedGroupParam() {
                    name = SINGLE_GROUP_NAME,
                    implicitParams = new List<ImplicitParam>(),
                };

                foreach (var implicitParam in implicitParams) {
                    if (collectShader) {
                        // 在着色器组中收集
                        var assetType = implicitParam.usedType[0];
                        if (assetType == typeof(Shader)) {
                            shaderGroupParam.implicitParams.Add(implicitParam);
                            continue;
                        }
                    }

                    // 如果它大于指定的大小，则使其成为单个捆绑包。
                    if (SEPARATE_ASSET_SIZE > 0 && implicitParam.fileSize > SEPARATE_ASSET_SIZE) {
                        var single = true;
                        if (implicitParam.isSubAsset && implicitParam.usedType[0] == typeof(Sprite)) {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(implicitParam.path);
                            foreach (var atlas in atlases) {
                                if (atlas.CanBindTo(sprite)) {
                                    Debug.LogWarning($"Skip sprite in atlas : {implicitParam.path}");
                                    single = false;
                                    break;
                                }
                            }
                        }
                        if (single)
                            singleGroupParam.implicitParams.Add(implicitParam);
                        continue;
                    }

                    // 非重叠资产什么都不做
                    if (implicitParam.bundles.Count == 1)
                        continue;

                    // 现有搜索
                    var hit = sharedGroupParams.Count > 0; // 第一时间回复
                    foreach (var groupParam in sharedGroupParams) {
                        //一、依赖的数量（重叠的数量）不同
                        if (groupParam.bundles.Count != implicitParam.bundles.Count) {
                            hit = false;
                            continue;
                        }
                        // 依赖（重复源）是同一个包吗
                        hit = true;
                        foreach (var bundle in implicitParam.bundles) {
                            if (!groupParam.bundles.Contains(bundle)) {
                                hit = false;
                                break;
                            }
                        }
                        if (hit) {
                            groupParam.implicitParams.Add(implicitParam);
                            break;
                        }
                    }
                    // 新规Group
                    if (!hit) {
                        sharedGroupParams.Add(
                            new SharedGroupParam() {
                                bundles = implicitParam.bundles,
                                implicitParams = new List<ImplicitParam>() { implicitParam },
                            });
                    }
                }

                // 着色器组
                if (collectShader)
                    sharedGroupParams.Add(shaderGroupParam);

                //单组分布
                var singleGroup = CreateSharedGroup(settings, SINGLE_GROUP_NAME);
                var schema = singleGroup.GetSchema<BundledAssetGroupSchema>();
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                CreateOrMoveEntry(settings, singleGroup, singleGroupParam);

                // Group分组
                foreach (var groupParam in sharedGroupParams) {
                    // 只有一个资产的组被分组到一个组中。
                    var group = singleGroup;

                    if (groupParam.implicitParams.Count > 1) {
                        var name = string.Format(groupParam.name, sharedGroupCount);
                        group = CreateSharedGroup(settings, name);
                        sharedGroupCount++;
                    }

                    CreateOrMoveEntry(settings, group, groupParam);
                }
                // 如果为空则不需要
                if (singleGroup.entries.Count == 0)
                    settings.RemoveGroup(singleGroup);

                // 字母数字排序
                settings.groups.Sort(CompareGroup);

                // 反映
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, eventData: null, postEvent: true, settingsModified: true);
            }

            /// <summary>
            /// 为 SharedAsset 创建组
            /// </summary>
            static AddressableAssetGroup CreateSharedGroup(AddressableAssetSettings settings, string groupName) {
                var groupTemplate = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
                var group = settings.CreateGroup(groupName, setAsDefaultGroup: false, readOnly: false, postEvent: false, groupTemplate.SchemaObjects);
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                // NOTE: 由于是依赖资产，因此省略了在目录中的注册（减少了catalog.json）
                schema.IncludeAddressInCatalog = false;
                schema.IncludeGUIDInCatalog = false;
                schema.IncludeLabelsInCatalog = false;

                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                schema.InternalBundleIdMode = BundledAssetGroupSchema.BundleInternalIdMode.GroupGuid;
                schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.Dynamic;
                schema.UseAssetBundleCrc = schema.UseAssetBundleCache = false;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;

                return group;
            }

            /// <summary>
            /// 进入指定组
            /// </summary>
            static void CreateOrMoveEntry(AddressableAssetSettings settings, AddressableAssetGroup group, SharedGroupParam groupParam) {
                foreach (var implicitParam in groupParam.implicitParams) {
                    var entry = settings.CreateOrMoveEntry(implicitParam.guid, group, readOnly: false, postEvent: false);
                    var addr = System.IO.Path.GetFileNameWithoutExtension(implicitParam.path);
                    entry.SetAddress(addr, postEvent: false);
                }
            }

            /// <summary>
            /// 提取隐式相关资产并收集信息
            /// </summary>
            private void GetImplicitAssetsParam(AddressableAssetsBuildContext context, List<ImplicitParam> implicitParams, List<SpriteAtlas> atlases) {
                var validImplicitGuids = new Dictionary<GUID, ImplicitParam>();

                foreach (var fileToBundle in this.ExtractData.WriteData.FileToBundle) {
                    if (this.ExtractData.WriteData.FileToObjects.TryGetValue(fileToBundle.Key, out var objects)) {
                        // NOTE: 所有引用都来了，所以多个引用来自同一个文件
                        foreach (var objectId in objects) {
                            var guid = objectId.guid;
                            var instance = ObjectIdentifier.ToObject(objectId);
                            var type = instance.GetType();

                            // 收集 SpriteAtlas 使纹理不会被单项检查卡住
                            if (atlases != null && type == typeof(SpriteAtlas))
                                atlases.Add(instance as SpriteAtlas);

                            // 是否是尚未进入的隐式资产
                            if (this.ExtractData.WriteData.AssetToFiles.ContainsKey(guid))
                                continue;

                            // Group
                            var bundle = fileToBundle.Value;
                            // 内置着色器找不到组
                            if (!context.bundleToAssetGroup.TryGetValue(bundle, out var groupGUID))
                                continue;
                            var selectedGroup = context.Settings.FindGroup(findGroup => findGroup.Guid == groupGUID);
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var isSubAsset = AssetDatabase.IsSubAsset(instance);

                            // Resources警告但允许重复资源
                            // NOTE: TextMeshPro在很多项目中都有用到，但是TextMeshPro是在Resources的前提下设计的，所以我们要允许它
                            if (!this.IsPathValidForEntry(path))
                                continue;
                            if (path.Contains("/Resources/"))
                                Debug.LogWarning($"Resources is duplicated. - {path} / Group : {selectedGroup.name}");

                            if (validImplicitGuids.TryGetValue(guid, out var param)) {
                                if (!param.usedType.Contains(type))
                                    param.usedType.Add(type);
                                if (!param.bundles.Contains(bundle))
                                    param.bundles.Add(bundle);
                                param.isSubAsset &= isSubAsset;
                            } else {
                                var fullPath = Application.dataPath.Replace("/Assets", "");
                                if (path.Contains("Packages/"))
                                    fullPath = System.IO.Path.GetFullPath(path);
                                else
                                    fullPath = System.IO.Path.Combine(fullPath, path);

                                // 根据压缩格式，纹理大小会发生显着变化，因此支持
                                // NOTE:AssetBundle LZ4 压缩后的结果会根据内容而变化，所以在构建之前无法检查
                                var fileSize = 0L;
                                if (instance is Texture)
                                    fileSize = this.GetStorageMemorySizeLong(instance as Texture);
                                if (fileSize == 0L)
                                    fileSize = new System.IO.FileInfo(fullPath).Length;

                                param = new ImplicitParam() {
                                    guid = guid.ToString(),
                                    path = path,
                                    isSubAsset = isSubAsset,
                                    usedType = new List<System.Type>() { type },
                                    bundles = new List<string>() { bundle },
                                    fileSize = fileSize,
                                };
                                validImplicitGuids.Add(guid, param);
                            }

                            // 确认用
                            //Debug.Log($"{implicitPath} / Entry : {explicitPath} / Group : {selectedGroup.name}");
                        }
                    }
                }

                implicitParams.AddRange(validImplicitGuids.Values);
            }

            static System.Text.RegularExpressions.Regex NUM_REGEX = new System.Text.RegularExpressions.Regex(@"[^0-9]");
            /// <summary>
            /// Addressables Group字母数字排序
            /// </summary>
            private static int CompareGroup(AddressableAssetGroup a, AddressableAssetGroup b) {
                if (a.name == "Built In Data")
                    return -1;
                if (b.name == "Built In Data")
                    return 1;
                if (a.IsDefaultGroup())
                    return -1;
                if (b.IsDefaultGroup())
                    return 1;
                //if (a.ReadOnly && !b.ReadOnly)
                //    return 1;
                //if (!a.ReadOnly && b.ReadOnly)
                //    return -1;
                if (a.name.Contains(SHARED_GROUP_NAME) && !b.name.Contains(SHARED_GROUP_NAME))
                    return 1;
                if (!a.name.Contains(SHARED_GROUP_NAME) && b.name.Contains(SHARED_GROUP_NAME))
                    return -1;

                var ret = string.CompareOrdinal(a.name, b.name);
                // 将数字与不同的位数对齐
                var regA = NUM_REGEX.Replace(a.name, "");
                var regB = NUM_REGEX.Replace(b.name, "");
                if ((regA.Length > 0 && regB.Length > 0) && regA.Length != regB.Length) {
                    if (ret > 0 && regA.Length < regB.Length)
                        return -1;
                    else if (ret < 0 && regA.Length > regB.Length)
                        return 1;
                }

                return ret;
            }
        }
    }
}
