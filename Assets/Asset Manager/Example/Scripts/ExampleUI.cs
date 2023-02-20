using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Skywatch.AssetManagement;
using UnityEngine;
using UnityEngine.UI;

/*
 * Addressable 窗口创建group 可以添加组和设置默认组
 */

public class ExampleUI : MonoBehaviour
{
    [SerializeField] bool _preLoadAssets;
    [SerializeField] string _preLoadLabel = "preload";
    //bool _loadFinished;
    [SerializeField] bool _usePooling;

    [Space(10)]

    [SerializeField] DataContainer _dataContainer;
    [SerializeField] AudioSource _audioSource;

    [Space(10)]
    [SerializeField] InputField _labelInput;
    [SerializeField] Text _infoText;
    [SerializeField] GameObject _buttonsRoot;
    [SerializeField] GameObject _loadingRoot;

    void Awake()
    {
        _loadingRoot.SetActive(true);
        _buttonsRoot.SetActive(false);

        if (_preLoadAssets)
        {
            var handle = AssetManager.LoadAssetsByLabelAsync(_preLoadLabel);
            handle.Completed += op =>
            {
                LoadFinished();
            };
        }
        else
        {
            LoadFinished();
        }
    }

    void Update()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Loading Assets: {AssetManager.loadingAssetsCount}");
        sb.AppendLine($"Loaded Assets: {AssetManager.loadedAssetsCount}");
        sb.AppendLine($"Instantiated Assets: {AssetManager.instantiatedAssetsCount}");
        _infoText.text = sb.ToString();
    }

    void LoadFinished()
    {
        //_loadFinished = true;
        _loadingRoot.SetActive(false);
        _buttonsRoot.SetActive(true);
    }

    /// <summary>
    /// 播放随机音频按钮点击事件
    /// </summary>
    public void PlayRandomAudioClip()
    {
        _dataContainer.PlayRandomAudioClip(_audioSource);
    }

    /// <summary>
    /// 生成随机例子系统按钮点击事件
    /// </summary>
    public void InstantiateRandomParticleSystem()
    {
        _dataContainer.InstantiateRandomParticleSystem(_usePooling);
    }

    /// <summary>
    /// 销毁所有的实例按钮点击事件
    /// </summary>
    public void DestroyAllInstances()
    {
        _dataContainer.DestroyAllInstances();
    }

    /// <summary>
    /// 卸载所有的资源按钮点击事件
    /// </summary>
    public void UnloadAllAssets()
    {
        _dataContainer.UnloadAllAssets();
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    /// <summary>
    /// 根据标签卸载按钮点击事件
    /// </summary>
    public void UnloadByLabel()
    {
        AssetManager.UnloadByLabel(_labelInput.text);
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    /// <summary>
    /// 打印加载的资源按钮点击事件
    /// </summary>
    public void LogLoadedAssets()
    {
        foreach (var loadedAsset in AssetManager.LoadedAssets)
        {
            Debug.Log(loadedAsset);
        }
    }
}
