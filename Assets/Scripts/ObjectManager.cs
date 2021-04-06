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
    // ResourceObject 类对象池
    protected ClassObjectPool<ResourceObj> m_ResourceObjectClassPool = ObjectManager.Instance.GetOrCreateClassPool<ResourceObj>(1000);

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="rcycleTrs">回收节点</param>
    /// <param name="sceneTrs">场景默认节点</param>
    public void Init(Transform rcycleTrs, Transform sceneTrs)
    {
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
            ResourceObj resObj = st[0];
            st.RemoveAt(0);
            GameObject obj = resObj.m_CloneObj;
            if (!System.Object.ReferenceEquals(obj, null)) // 效率更高
            {
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
    public GameObject InstantiateObject(string path, bool setSceneObj,  bool bClear = true)
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
        }

        return resourceObj.m_CloneObj;
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




