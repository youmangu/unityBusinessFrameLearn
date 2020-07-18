using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuddleEditor
{
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        foreach (var str in abConfig.m_AllPrefabPath)
            Debug.Log(str);

        foreach (var str in abConfig.m_AllFileDirAB)
        {
            Debug.Log(str.ABName + "    " + str.Path);
        }

        Debug.Log("打包");
        //BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        //AssetDatabase.Refresh();
    }
}
