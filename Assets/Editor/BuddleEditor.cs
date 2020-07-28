using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuddleEditor
{
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";

    // key 是AB包的名字，value是路径
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();

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

        }

        EditorUtility.ClearProgressBar();




        Debug.Log("打包");
        //BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        //AssetDatabase.Refresh();
    }
}
