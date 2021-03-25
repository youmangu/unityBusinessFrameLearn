using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    // 资源关系依赖配置表， 可以根据crc来找到对应资源块
    protected static Dictionary<uint, AssetBundleItem> m_ResourceItemDic = new Dictionary<uint, AssetBundleItem>();

    /// <summary>
    /// 加载AB配置表
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        // 1. 读取配置
        string path = Application.streamingAssetsPath + "/assetbundleconfig";
        AssetBundle ab = AssetBundle.LoadFromFile(path);
        TextAsset textAsset = ab.LoadAsset<TextAsset>("assetbundleconfig");
        if (textAsset == null)
        {
            Debug.LogError("Assetbundleconfig is no exist!");
            return false;
        }

        // 解析出来
        MemoryStream ms = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleconfig config = (AssetBundleconfig)bf.Deserialize(ms);
        ms.Close();

        m_ResourceItemDic.Clear();
        foreach (ABBase abBase in config.ABList)
        {
            AssetBundleItem abItem = new AssetBundleItem();
            abItem.m_Crc = abBase.Crc;
            abItem.m_AssetName = abBase.ABName;
            abItem.m_AssetBundleName = abBase.AssetName;
            abItem.m_DependAssetBundle = abBase.ABDependence;

            if (m_ResourceItemDic.ContainsKey(abItem.m_Crc))
            {
                Debug.LogError("重复的crc， 资源名：" + abItem.m_AssetName + " ab 包名：" + abItem.m_AssetBundleName);
            }
            else
            {
                m_ResourceItemDic.Add(abItem.m_Crc, abItem);
            }
        }

        return true;
    }
}


public class AssetBundleItem
{
    // 资源路径的CRC
    public uint m_Crc = 0;
    // 该资源的文件名
    public string m_AssetName = string.Empty;
    // 该资源所在的AssetBundle
    public string m_AssetBundleName = string.Empty;
    // 该资源所依赖的AssetBundle
    public List<string> m_DependAssetBundle = null;
    // 该资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
}
