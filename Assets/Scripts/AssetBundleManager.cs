using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    // 资源关系依赖配置表， 可以根据crc来找到对应资源块
    protected static Dictionary<uint, ResourceItem> m_ResourceItemDic = new Dictionary<uint, ResourceItem>();
    // 储存AssetBundleItem， key为路径的crc
    private static Dictionary<uint, AssetBundleItem> m_AssetBundleDic = new Dictionary<uint, AssetBundleItem>();
    // AssetBundleItem 对象池
    private static ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = new ClassObjectPool<AssetBundleItem>(500);

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
            ResourceItem abItem = new ResourceItem();
            abItem.m_Crc = abBase.Crc;
            abItem.m_AssetBundleName = abBase.ABName;
            abItem.m_AssetName = abBase.AssetName;
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

    /// <summary>
    /// 根据路径Crc加载中间类 ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem LoadResourceAssetBundle(uint crc)
    {
        ResourceItem item = null;
        if (!m_ResourceItemDic.TryGetValue(crc, out item) || item == null)
        {
            Debug.LogError("LoadResourceAssetBundle error: cannot find crc "+ crc + " in AssetBundleConfig");
            return item;
        }

        if (item.m_AssetBundle != null)
        {
            return item;
        }

        item.m_AssetBundle = LoadAssetBundle(item.m_AssetBundleName);

        // 加载依赖
        if (item.m_DependAssetBundle != null)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                LoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }

        return item;
    }

    private AssetBundle LoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);
        if (!m_AssetBundleDic.TryGetValue(crc, out item))
        {
            AssetBundle assetBundle = null;
            string fullPath = Application.streamingAssetsPath + "/" + name;
            if (File.Exists(fullPath))
            {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }

            if (assetBundle == null)
            {
                Debug.LogError("Load AssetBundle error: " + fullPath);
            }

            item = m_AssetBundleItemPool.Spawn(true);
            item.assetBundle = assetBundle;
            item.RefCount++;
            m_AssetBundleDic.Add(crc, item);
        }
        else
        {
            item.RefCount++;
        }

        return item.assetBundle;
;    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResourceItem item)
    {
        if (item == null)
            return;

        if (item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                UnLoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        UnLoadAssetBundle(item.m_AssetBundleName);
    }

    private void UnLoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);
        if(m_AssetBundleDic.TryGetValue(crc, out item) && (item != null))
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.assetBundle != null)
            {
                item.assetBundle.Unload(true);
                item.Reset();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleDic.Remove(crc);
            }
        }
    }

    /// <summary>
    /// 根据crc 查找 ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem FindResourceItem(uint crc)
    {
        return m_ResourceItemDic[crc];
    }
}

public class AssetBundleItem
{
    public AssetBundle assetBundle = null;
    public int RefCount;

    public void Reset()
    {
        assetBundle = null;
        RefCount = 0;
    }
}


public class ResourceItem
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


    // 资源对象
    public Object m_Obj = null;
    // 资源唯一标识
    public int m_Guid = 0;
    // 资源最后使用的时间
    public float m_LastUseTime = 0.0f;
    // 是否跳场景清掉
    public bool m_Clear = true;

    // 引用计数
    protected int m_RefCount = 0;
    public int RefCount
    {
        get {
            return m_RefCount;
        }
        set {
            m_RefCount = value;
            if (m_RefCount < 0)
                Debug.LogError("refcount < 0 " + m_RefCount + " , " + m_Obj != null ? m_Obj.name : " name is null" );
        }
    }
}
