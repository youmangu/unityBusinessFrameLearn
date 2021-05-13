using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectManager : Singleton<ObjectManager>
{
    // 对象池节点
    public Transform RecyclePoolTrs;
    // 场景节点
    public Transform SceneTrs;
    // 对象池
    protected Dictionary<uint, List<ResourceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResourceObj>>();
    // 暂存ResObjde Dic
    protected Dictionary<int, ResourceObj> m_ResourceObjDic = new Dictionary<int, ResourceObj>();
    // ResourceObject 类对象池
    protected ClassObjectPool<ResourceObj> m_ResourceObjectClassPool = null;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="rcycleTrs">回收节点</param>
    /// <param name="sceneTrs">场景默认节点</param>
    public void Init(Transform rcycleTrs, Transform sceneTrs)
    {
        m_ResourceObjectClassPool = ObjectManager.Instance.GetOrCreateClassPool<ResourceObj>(1000); 
        RecyclePoolTrs = rcycleTrs;
        SceneTrs = sceneTrs;
    }

   
    /// <summary>
    /// 从对象池取对象
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    protected ResourceObj GetObjectFromPool(uint crc)
    {
        List<ResourceObj> st = null;
        if (m_ObjectPoolDic.TryGetValue(crc, out st) && st != null && st.Count > 0)
        {
            ResourceManager.Instance.IncreaseResourceRef(crc);
            ResourceObj resObj = st[0];
            st.RemoveAt(0);
            GameObject obj = resObj.m_CloneObj;
            if (!System.Object.ReferenceEquals(obj, null)) // 效率更高
            {
                // Resourcemanager de 引用计数
                resObj.m_Already = false;
                if (obj.name.EndsWith("(Recycle)"))
                {
                    obj.name = obj.name.Replace("(Recycle)", "");
                }
            }

            return resObj;
        }

        return null;
    }

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="bClear"></param>
    /// <returns></returns>
    public GameObject InstantiateObject(string path, bool setSceneObj = false,  bool bClear = true)
    {
        uint crc = CRC32.GetCRC32(path);
        ResourceObj resourceObj = GetObjectFromPool(crc);
        if (resourceObj == null)
        {
            resourceObj = m_ResourceObjectClassPool.Spawn(true);
            resourceObj.m_Crc = crc;
            resourceObj.m_bClear = bClear;
            // ResourceManager提供加载方法
            resourceObj = ResourceManager.Instance.LoadResource(path, resourceObj);

            if (resourceObj.m_ResourceItem.m_Obj != null)
            {
                resourceObj.m_CloneObj = GameObject.Instantiate(resourceObj.m_ResourceItem.m_Obj) as GameObject;
            }

        }

        if (setSceneObj)
        {
            resourceObj.m_CloneObj.transform.SetParent(SceneTrs, false);
            resourceObj.m_CloneObj.SetActive(true);
        }

        int tempID = resourceObj.m_CloneObj.GetInstanceID();
        if (!m_ResourceObjDic.ContainsKey(tempID))
        {
            m_ResourceObjDic.Add(tempID, resourceObj);
        }

        return resourceObj.m_CloneObj;
    }

    /// <summary>
    /// 回收资源
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="maxCacheCount"></param>
    /// <param name="destroyCache"></param>
    /// <param name="recycleParent"></param>
    public void ReleaseObject(GameObject obj, int maxCacheCount = -1, bool destroyCache = false, bool recycleParent = true)
    {
        if (obj == null)
            return;

        ResourceObj resObj = null;
        int tempID = obj.GetInstanceID();
        if (!m_ResourceObjDic.TryGetValue(tempID, out resObj))
        {
            Debug.LogError(obj.name + " 这个对象不是objectManager创建的！");
            return;
        }

        if (resObj == null)
        {
            Debug.LogError("缓存的object为空");
            return;
        }

        if (resObj.m_Already)
        {
            Debug.LogError("该对象已经放回对象池，检测自己是否清空引用");
            return;
        }

#if UNITY_EDITOR
        obj.name += "(Recycle)";
#endif
        List<ResourceObj> st = null;
        if (maxCacheCount == 0)
        {
            m_ResourceObjDic.Remove(tempID);
            ResourceManager.Instance.ReleaseResource(resObj, destroyCache);
            resObj.Reset();
            m_ResourceObjectClassPool.Recycle(resObj);
        }
        else  // 回收到对象池
        {
            if (!m_ObjectPoolDic.TryGetValue(resObj.m_Crc, out st) || st == null)
            {
                st = new List<ResourceObj>();
                m_ObjectPoolDic.Add(resObj.m_Crc, st);
            }

            if (resObj.m_CloneObj)
            {
                if (recycleParent)
                {
                    resObj.m_CloneObj.transform.SetParent(RecyclePoolTrs);
                    resObj.m_CloneObj.SetActive(false);
                }
                else
                {
                    resObj.m_CloneObj.SetActive(false);
                }
            }

            if (maxCacheCount < 0 || st.Count < maxCacheCount)
            {
                st.Add(resObj);
                resObj.m_Already = true;
                // ResourceManager做一个引用计数
                ResourceManager.Instance.DecreaseResourceRef(resObj);
            }
            else
            {
                m_ResourceObjDic.Remove(tempID);
                ResourceManager.Instance.ReleaseResource(resObj, destroyCache);
                resObj.Reset();
                m_ResourceObjectClassPool.Recycle(resObj);
            }

        }

    }


    #region 类对象池的使用
    protected Dictionary<Type, object> m_classPoolDic = new Dictionary<Type, object>();

    /// <summary>
    /// 创建对象池的方法，创建完成以后外面可以保存classObjectPool<T>, 然后在调用Spawn和Rycycle来创建和回收对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxCount"></param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxCount) where T : class, new()
    {
        Type type = typeof(T);
        object outObj = null;
        if (!m_classPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxCount);
            m_classPoolDic.Add(type, newPool);
            return newPool;
        }

        return outObj as ClassObjectPool<T>;
    }

    /// <summary>
    /// 从对象池取对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public T NewClassObjectFromPool<T>(int maxcount) where T : class, new()
    {
        ClassObjectPool<T> pool =  GetOrCreateClassPool<T>(maxcount);
        if (pool == null)
            return null;
        return pool.Spawn(true);
    }

    #endregion
}




