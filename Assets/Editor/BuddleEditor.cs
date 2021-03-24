using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BuddleEditor
{
    public static string m_BundleTargetPath = Application.streamingAssetsPath;
    private static string ABCONFIGPATH = "Assets/Editor/ABConfig.asset";

    // key 是AB包的名字，value是路径
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();

    // 过滤ab包
    private static List<string> m_AllFileAB = new List<string>();

    // 单个prefab的ab包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();

    // 储存所有有效路径
    private static List<string> m_ConfigFile = new List<string>();

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        m_ConfigFile.Clear();
        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
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
                m_AllFileAB.Add(fileDir.Path);
                m_ConfigFile.Add(fileDir.Path);
            }
        }

        // 返回的是 guid 数组
        string []str = AssetDatabase.FindAssets("t:prefab", abConfig.m_AllPrefabPath.ToArray());

        for (int i = 0; i < str.Length; i++)  
        {
            // 根据GUID获取到路径
            string path = AssetDatabase.GUIDToAssetPath(str[i]);
            // 显示进度条
            EditorUtility.DisplayProgressBar("查找Prafab", "prefab:" + path, i * 1.0f / str.Length);
            m_ConfigFile.Add(path);
            if (!ContainAllFileAB(path))
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string []depencies = AssetDatabase.GetDependencies(path);
                List<string> allDepencies = new List<string>();
                for (int j = 0; j < depencies.Length; j++)
                {
                    // Debug.Log("depency: "+depencies[j]);
                    if (!ContainAllFileAB(depencies[i]) && !depencies[i].EndsWith(".cs"))
                    {
                        m_AllFileAB.Add(depencies[i]);
                        allDepencies.Add(depencies[i]);

                    }
                }

                if (m_AllPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的prefab，位置：" + path + " 名字： " + obj.name);
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

        BuildAssetBundle();

        string []oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB名字","名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        Debug.Log("打包完成");

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

    static void BuildAssetBundle()
    {
        string[] allBuddles = AssetDatabase.GetAllAssetBundleNames();
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allBuddles.Length; i++)
        {
            string[] allBuddlePaths = AssetDatabase.GetAssetPathsFromAssetBundle(allBuddles[i]);
            for (int j = 0; j < allBuddlePaths.Length; j++)
            {
                if (allBuddlePaths[j].EndsWith(".cs") || !ValidPath(allBuddlePaths[j]))
                    continue;
                //Debug.Log("此AB包 " + allBuddles[i] + " 下面包含的资源文件路径有：" + allBuddlePaths[j]);
                if(ValidPath(allBuddlePaths[j]))
                    resPathDic.Add(allBuddlePaths[j], allBuddles[i]);
            }
        }

        DeleteAB();
        // 打包生成自己的配置表
        WriteData(resPathDic);


        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.Refresh();
    }

    static void WriteData(Dictionary<string, string> resPathDic)
    {
        AssetBundleconfig config = new AssetBundleconfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = CRC32.GetCRC32(path);
            abBase.ABName = resPathDic[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDependence = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependce.Length; i++)
            {
                string tempPath = resDependce[i];
                if (tempPath == path || path.EndsWith(".cs"))
                    continue;
                string abName = "";
                if (resPathDic.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDic[path])
                        continue;

                    if(!abBase.ABDependence.Contains(abName))
                    {
                        abBase.ABDependence.Add(abName);
                    }
                }
            }
            config.ABList.Add(abBase);
        }

        // 写入xml
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        if (File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xmlSerilier = new XmlSerializer(config.GetType());
        xmlSerilier.Serialize(sw, config);
        sw.Close();
        fileStream.Close();

        // 写入二进制
        foreach (ABBase ab in config.ABList)  // 此处为优化，可以减少二进制文件容量大小
        {
            ab.Path = "";
        }
        string bytePath = m_BundleTargetPath + "/AssetbundleConfig.bytes";
        if (File.Exists(bytePath)) File.Delete(bytePath);
        FileStream byteStream = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(byteStream, config);
        byteStream.Close();

    }

    // 删除无用的AB包
    static void DeleteAB()
    {
        // 1. 获取所有的AB包名
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        // 2. 获取目录信息 DirectoryInfo
        DirectoryInfo directoryInfo = new DirectoryInfo(m_BundleTargetPath);
        // 3. 获取文件信息  FileInfo[]
        FileInfo[] fileInfos = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        // 4. fileInfo 中是否包含 AB 或者是否是 .meta 文件， 如果没使用，则删除
        for (int i = 0; i < fileInfos.Length; i++)
        {
            if (ConatinABName(fileInfos[i].Name, allBundleNames) || fileInfos[i].Name.EndsWith(".meta") )
                continue;
            else
            {
                if (File.Exists(fileInfos[i].FullName))
                    File.Delete(fileInfos[i].FullName);
            }
        }
    }

    /// <summary>
    /// 遍历文件夹里的文件名与设置
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strs"></param>
    /// <returns></returns>
    static bool ConatinABName(string name, string[] strs)
    {
        foreach(string str in strs)
        {
            if (str.Equals(name))
                return true;
        }

        return false;
    }


    /// <summary>
    /// 是否包含在已经有的AB包里，用来做荣誉剔除
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || path.Contains(m_AllFileAB[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 是否有效路径
    /// </summary>
    static bool ValidPath(string path)
    {
        for (int i = 0; i < m_ConfigFile.Count; i++)
        {
            if (path.Contains(m_ConfigFile[i]))
                return true;
        }

        return false;
    }
}
