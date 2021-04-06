using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LoadResPriority
{
    RES_HIGHT = 0, // 最高优先级
    RES_MIDDLE,    // 一般优先级
    RES_LOW,       // 低优先级
    RES_NUM,
}

public class ResourceObj
{
    // 路径对应的crc
    public uint m_Crc = 0;
    // 存ResourceItem
    public ResourceItem m_ResourceItem = null;
    // 实例化出来的Gameobject
    public GameObject m_CloneObj = null;
    // 是否跳场景清楚
    public bool m_bClear = true;
    // 出错GuId
    public long m_Guid = 0;

    public void Reset()
    {
        m_Crc = 0;
        m_RestItem = null;
        m_CloneObj = null;
        m_bClear = true;
        m_Guid = 0;
    }
}

public class AsyncLoadResParam
{
    public List<AsyncCallBack> m_CallbackList = new List<AsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public bool m_Sprite = false;
    public LoadResPriority m_Priority = LoadResPriority.RES_LOW;

    public void Reset()
    {
        m_CallbackList.Clear();
        m_Crc = 0;
        m_Path = string.Empty;
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_LOW;
    }
}

public class AsyncCallBack
{
    // 加载完成的回调
    public OnAsyncObjFinish m_DealFinish = null;
    // 回调参数
    public object param1 = null, param2 = null, param3 = null;

    public void Reset()
    {
        m_DealFinish = null;
        param1 = null;
        param2 = null;
        param3 = null;
    }
}

// 后三个参数为保留参数，备用
public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);


public class ResourceManager : Singleton<ResourceManager>
{
    public bool m_LoadFromAssetBundle = true;
    // 缓存使用的资源列表
    public Dictionary<uint, ResourceItem> AssetDic { get; set; } = new Dictionary<uint, ResourceItem>();
    // 缓存引用计数为0的资源列表， 达到缓存最大的时候释放这个列表里最早没用的资源
    protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();

    // 中间类， 回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallbackPool = new ClassObjectPool<AsyncCallBack>(100);

    //Mono脚本, 用于开启携程
    protected MonoBehaviour m_Startmono;
    // 正在加载的资源列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    // 正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    // 连续加载资源的最长时间， 单位微妙
    private const long MAXLOADRESTIME = 200000;

    public void Init(MonoBehaviour mono)
    {
        for (int i= 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        m_Startmono = mono;
        m_Startmono.StartCoroutine(AsyncLoadCor());
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache()
    {
        List<ResourceItem> tempList = new List<ResourceItem>();
        foreach (ResourceItem item in AssetDic.Values)
        {
            if (item.m_Clear)
                tempList.Add(item);
        }

        foreach (ResourceItem item in tempList)
        {
            DestroyResourceItem(item, item.m_Clear);
        }

        tempList.Clear();

        //while (m_NoRefrenceAssetMapList.Size() > 0)
        //{
        //    ResourceItem item = m_NoRefrenceAssetMapList.Back();
        //    DestroyResourceItem(item, true);
        //    m_NoRefrenceAssetMapList.Pop();
        //}
    }

    /// <summary>
    /// 预加载资源
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = GetCacheResourceItem(crc, 0);
        if (item != null)
        {
            return;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj;
            }
            else
                obj = LoadAssetByEditor<Object>(path);

        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj;
                }
                else
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
            }
        }

        CacheResource(path, ref item, crc, obj);
        // 条场景不清空缓存
        item.m_Clear = false;
        ReleaseResource(obj, false);

    }

    /// <summary>
    /// 同步加载资源， 针对给Objectmanager的接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <returns></returns>
    public ResourceObj LoadResource(string path, ResourceObj resObj)
    {
        if (resObj == null)
        {
            return null;
        }

        uint crc = resObj.m_Crc == 0 ? CRC32.GetCRC32(path) : resObj.m_Crc;

        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            resObj.m_ResourceItem = item;
            return resObj;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
                obj = LoadAssetByEditor<Object>(path) as Object;
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName) as Object;
            }
        }

        CacheResource(path, ref item, crc, obj);
        resObj.m_ResourceItem = item;
        item.m_Clear = resObj.m_bClear;

        return resObj;

    }


    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源，例如 Texture, 音频等等
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
            return null;

        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }

        T obj = null;
#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as T;
            }
            else
                obj = LoadAssetByEditor<T>(path);
            
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
            }
        }

        CacheResource(path, ref item, crc, obj);

        return obj;
    }

    /// <summary>
    /// 不需要的实例化的资源释放， 根据对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destroy"></param>
    /// <returns></returns>
    public bool ReleaseResource(Object obj, bool destroy = false)
    {
        if (obj == null)
            return false;

        ResourceItem item = null;
        foreach (ResourceItem res in AssetDic.Values)
        {
            if (res.m_Guid == obj.GetInstanceID())
            {
                item = res;
                break;
                    
            }
        }
        if (item == null)
        {
            Debug.LogError("AssetDic 不存在该资源：" + obj.name + " 可能释放了多长");
            return false;
        }

        item.RefCount--;

        DestroyResourceItem(item, destroy);
        return true;
    }

    /// <summary>
    /// 不需要的实例化的资源释放, 根据路径
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destroy"></param>
    /// <returns></returns>
    public bool ReleaseResource(string path, bool destroy = false)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = null;
        if (!AssetDic.TryGetValue(crc, out item) || null == item)
        {
            Debug.LogError("AssetDic 不存在该资源：" + path + " 可能释放了多长");
            return false;
        }

        item.RefCount--;

        DestroyResourceItem(item, destroy);
        return true;
    }


    /// <summary>
    /// 缓存加载的资源
    /// </summary>
    /// <param name="name"></param>
    /// <param name="item"></param>
    /// <param name="crc"></param>
    /// <param name="obj"></param>
    /// <param name="addRefcount"></param>
    void CacheResource(string name, ref ResourceItem item, uint crc, Object obj, int addRefcount = 1)
    {
        // 缓存太多，清除最早没有使用的资源
        WashOut();

        if (item == null)
            Debug.LogError("");
        if (obj == null)
            Debug.LogError("");

        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addRefcount;
        ResourceItem oldItem = null;
        if (AssetDic.TryGetValue(item.m_Crc, out oldItem))
        {
            AssetDic[item.m_Crc] = item;
        }
        else {
            AssetDic.Add(item.m_Crc, item);
        }
    }

    /// <summary>
    /// 缓存太多，清除最早没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        // 当当前内存使用大于80%，我们来进行清除最早没用的资源
        {
            if (m_NoRefrenceAssetMapList.Size() <= 0)
                return;

            ResourceItem item = m_NoRefrenceAssetMapList.Back();
            DestroyResourceItem(item, true);
            m_NoRefrenceAssetMapList.Pop();
        }
    }

    /// <summary>
    /// 回收同一个资源
    /// </summary>
    /// <param name="item"></param>
    /// <param name="destroy"></param>
    protected void DestroyResourceItem(ResourceItem item, bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
            return;

        if (!AssetDic.Remove(item.m_Crc))
            return;

        if (!destroyCache)
        {
            //m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }

        // 释放AssetBundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        if (item.m_Obj != null)
        {
            item.m_Obj = null;
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }
    }

#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

    ResourceItem GetCacheResourceItem(uint crc, int addrefcount = 1)
    {
        ResourceItem item = null;
        if (AssetDic.TryGetValue(crc, out item))
        {
            if (item != null)
            {
                item.RefCount += addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;

                if (item.RefCount <= 1)
                {
                    m_NoRefrenceAssetMapList.RemoveNode(item);
                }
            }
        }

        return item;
    }

    /// <summary>
    /// 异步加载资源（仅仅是不需要实例化的资源，例如音乐， 图片等）
    /// </summary>
    public void AsyncLoadResource(string path, OnAsyncObjFinish deleFinish,  LoadResPriority priority, object param1 = null, object param2 = null, object param3 = null, uint crc = 0 )
    {
        if (crc == 0)
        {
            crc = CRC32.GetCRC32(path);
        }

        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            if (deleFinish != null)
            {
                deleFinish(path, item.m_Obj, param1, param2, param3);
            }
            return;
        }

        // 判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(crc, out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }

        // 回调列表里加回调
        AsyncCallBack callBack = m_AsyncCallbackPool.Spawn(true);
        callBack.m_DealFinish = deleFinish;
        callBack.param1 = param1;
        callBack.param2 = param2;
        callBack.param3 = param3;
        para.m_CallbackList.Add(callBack);

    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadCor()
    {
        List<AsyncCallBack> callBackList = null;
        // 上一次yield的时间
        long lastYiledTime = System.DateTime.Now.Ticks;
        while (true)
        {
            bool haveYield = false;
            // 根据优先级加载
            for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
            {
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                    continue;

                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);

                callBackList = loadingItem.m_CallbackList;

                Object obj = null;
                ResourceItem item = null;
#if UNITY_EDITOR
                if (!m_LoadFromAssetBundle)
                {
                    obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    // 模拟异步加载
                    yield return new WaitForSeconds(0.5f);

                    item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);
                }
#endif

                if (obj == null)
                {
                    item = AssetBundleManager.Instance.LoadResourceAssetBundle(loadingItem.m_Crc);
                    if (item != null && item.m_AssetBundle != null)
                    {
                        AssetBundleRequest abRequest = null;
                        if (loadingItem.m_Sprite)
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                        }
                        else
                            abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                        yield return abRequest;
                        if (abRequest.isDone)
                        {
                            obj = abRequest.asset;
                        }
                        lastYiledTime = System.DateTime.Now.Ticks;
                    }
                }

                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBackList.Count);

                foreach (AsyncCallBack callBack in callBackList)
                {
                    if (callBack != null && callBack.m_DealFinish != null)
                    {
                        callBack.m_DealFinish(loadingItem.m_Path, obj, callBack.param1, callBack.param2, callBack.param3);
                        callBack.m_DealFinish = null;

                    }

                    callBack.Reset();
                    m_AsyncCallbackPool.Recycle(callBack);
                }

                obj = null;
                callBackList.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            if (!haveYield || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                yield return null;
                lastYiledTime = System.DateTime.Now.Ticks;
            }

            yield return null;
        }
    }

}

//双向链表结构节点
public class DoubleLinkedListNode<T> where T : class, new()
{
    // 前一个节点
    public DoubleLinkedListNode<T> prev = null;
    // 后一个节点
    public DoubleLinkedListNode<T> next = null;
    // 当前节点
    public T t = null;
}

//双向链表结构
public class DoubleLinkedList<T> where T : class, new()
{
    // 表头
    public DoubleLinkedListNode<T> Head = null;
    // 表尾
    public DoubleLinkedListNode<T> Tail = null;
    // 双向链表结构类对象池
    protected ClassObjectPool<DoubleLinkedListNode<T>> m_DoubleLinkedNodePool = ObjectManager.Instance.GetOrCreateClassPool<DoubleLinkedListNode<T>>(500);
    // 个数
    protected int m_Count = 0;
    public int Count
    {
        get { return m_Count; }
    }

    /// <summary>
    /// 添加到头部节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(T t)
    {
        DoubleLinkedListNode<T> plist = m_DoubleLinkedNodePool.Spawn(true);
        plist.prev = null;
        plist.next = null;
        plist.t = t;
        return AddToHeader(plist);
    }

    /// <summary>
    /// 添加到头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToHeader(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return null;
        pNode.prev = null;
        if (Head == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }

        m_Count++;
        return Head;
            
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(T t)
    {
        DoubleLinkedListNode<T> pNode = m_DoubleLinkedNodePool.Spawn(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return AddToTail(pNode);
    }

    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null) return null;

        pNode.next = null;
        if (Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }

        m_Count++;
        return Tail;
    }

    public void RemoveNode(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null)
            return;
        if (Head == pNode)
            Head = pNode.next;
        if (Tail == pNode)
            Tail = pNode.prev;
        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkedNodePool.Recycle(pNode);
        m_Count--;
    }

    public void MoveToHead(DoubleLinkedListNode<T> pNode)
    {
        if (pNode == null || pNode == Head)
            return;

        if (pNode.prev == null && pNode.next == null)
            return;

        if (pNode == Tail)
            Tail = pNode.prev;

        if (pNode.prev != null)
            pNode.prev.next = pNode.next;

        if (pNode.next != null)
            pNode.next.prev = pNode.prev;

        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;
        if (Tail == null)
        {
            Tail = Head;
        }
    }
}


public class CMapList<T> where T : class, new()
{
    public DoubleLinkedList<T> m_DLink = new DoubleLinkedList<T>();
    public Dictionary<T, DoubleLinkedListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkedListNode<T>>();

    ~CMapList()
    {
        Clear();
    }
    /// <summary>
    /// 插入到头部
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (m_FindMap.TryGetValue(t, out node) && node != null)
        {
            m_DLink.MoveToHead(node);
            return;
        }

        m_DLink.AddToHeader(t);
        m_FindMap.Add(t, node);
    }

    /// <summary>
    /// 弹出尾部
    /// </summary>
    public void Pop()
    {
        if (m_DLink.Tail != null)
            RemoveNode(m_DLink.Tail.t);
    }

    /// <summary>
    /// 删除某个节点
    /// </summary>
    /// <param name="t"></param>
    public void RemoveNode(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t,out node) || node == null)
            return;
        m_DLink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 获取尾部节点
    /// </summary>
    /// <returns></returns>
    public T Back()
    {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }

    /// <summary>
    /// 节点个数
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return m_FindMap.Count;
    }

    /// <summary>
    /// 节点是否存在
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 刷新节点，把节点移到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Refresh(T t)
    {
        DoubleLinkedListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
            return false;
        }

        m_DLink.MoveToHead(node);
        return true;
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear()
    {
        while (m_DLink.Tail != null)
        {
            RemoveNode(m_DLink.Tail.t);
        }
    }



}