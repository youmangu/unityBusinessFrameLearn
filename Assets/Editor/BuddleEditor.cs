using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuddleEditor
{
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";

    // key 是AB包的名字，value是路径
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();

    // 过滤ab包
    private static List<string> m_AllFileAB = new List<string>();

    // 单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>(); 

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        m_AllFileDir.Clear();
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        foreach (ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("已经包含了相同的文件名:" + fileDir.ABName + ", 请检查");
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFileAB.Add(fileDir.ABName);
            }
        }

        // 返回的是 guid 数组
        string []str = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());

        for (int i = 0; i < str.Length; i++)  
        {
            // 根据GUID获取到路径
            string path = AssetDatabase.GUIDToAssetPath(str[i]);
            // 显示进度条
            EditorUtility.DisplayProgressBar("查找Prafab", "Prefab:" + path, i * 1.0f / str.Length);

            if (!ContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string []depencies = AssetDatabase.GetDependencies(path);
                List<string> allDepencies = new List<string>();
                for (int j = 0; j < depencies.Length; j++)
                {
                    Debug.Log(depencies[j]);
                    if (!ContainAllFileAB(depencies[i]) && depencies[i].EndsWith(".cs"))
                    {
                        m_AllFileAB.Add(depencies[i]);
                        allDepencies.Add(depencies[i]);

                    }
                }

                if (m_AllPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的prefab，名字：" + obj.name);
                }
                else { 
                    m_AllPrefabDir.Add(obj.name, allDepencies);
                }

            }



        }

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_AllPrefabDir.Keys)
        {
            SetABName(name, m_AllPrefabDir[name]);
        }

        // 非常耗时，最好不要做
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();

        string []oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB名字","名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }

        EditorUtility.ClearProgressBar();


        Debug.Log("打包");
        //BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        //AssetDatabase.Refresh();
    }

    static void SetABName(string name, string path)
    {
        AssetImporter assetImport = AssetImporter.GetAtPath(path);
        if (assetImport == null)
        {
            Debug.LogError("不存在此路径文件：" + path);
        }
        else
        {
            assetImport.assetBundleName = name;
        }

    }

    static void SetABName(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || path.Contains(m_AllFileAB[i]))
                return true;
        }

        return false;
    }
}
