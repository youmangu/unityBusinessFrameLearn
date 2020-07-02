using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuddleEditor
{

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        Debug.Log("打包");
        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
    }
}
