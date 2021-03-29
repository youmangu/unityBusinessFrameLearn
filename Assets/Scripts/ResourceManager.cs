using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{

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
            m_DLink.MoveToHead(t);
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