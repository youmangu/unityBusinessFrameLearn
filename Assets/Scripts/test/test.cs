using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class test : MonoBehaviour {

    public GameObject m_object;
	// Use this for initialization
	void Start () {
        // 方式一
        //GameObject obj = GameObject.Instantiate(m_object);
        // 方式二
        //GameObject obj = GameObject.Instantiate(Resources.Load("bazi_bg") as GameObject);
        // 方式三（常用， 有點：內存小，加載快）
        //AssetBundle ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/coin");
        //GameObject go = GameObject.Instantiate(ab.LoadAsset<GameObject>("CoinPrefab"));
        // 方式四(編輯器代碼或者框架)
        //GameObject go = GameObject.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/CoinPrefab.prefab"));

        //SerilizeTest();
        //XmlDeserilize();
        // BinarySerTest();
        // BinaryDeserialize();
        //ReadAssetsTest();
        TestLoadAB();
    }

    void TestLoadAB()
    {
        //AssetBundle abConfig1 = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/assetbundleconfig");
        //TextAsset testAsset = abConfig1.LoadAsset<TextAsset>("AssetbundleConfig");
        //MemoryStream ms = new MemoryStream(testAsset.bytes);
        //BinaryFormatter bf = new BinaryFormatter();
        //AssetBundleconfig abConfig = (AssetBundleconfig)bf.Deserialize(ms);
        //ms.Close();

        //string path = "Assets/GameData/Prefabs/Attack.prefab";
        //ABBase ab = null;
        //for (int i = 0; i < abConfig.ABList.Count; i++)
        //{
        //    uint crc = CRC32.GetCRC32(path);
        //    if (abConfig.ABList[i].ABName == "attack")
        //        Debug.Log(crc.ToString());
        //    if (crc == abConfig.ABList[i].Crc)
        //    {
        //        ab = abConfig.ABList[i];
        //    }
        //}

        //AssetBundle abBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + ab.ABName);
        //GameObject go = GameObject.Instantiate(abBundle.LoadAsset<GameObject>(ab.ABName));

    }

    void ReadAssetsTest()
    {

        //AssetsSerilize ta = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetsSerilize>("Assets/assetsTest.asset");
        //Debug.Log(ta.id);
        //Debug.Log(ta.name);
        //foreach (string str in ta.testList)
        //{
        //    Debug.Log(str);
        //}

    }

    public void SerilizeTest()
    {
        XmlSerilier serilize = new XmlSerilier();
        serilize.Id = 1;
        serilize.Name = "测试";
        serilize.List = new List<int>();
        serilize.List.Add(0);
        serilize.List.Add(1);
        serilize.List.Add(2);
        XmlSerilize(serilize);
    }

    void XmlSerilize(XmlSerilier serilize)
    {
        FileStream fileStream = new FileStream(Application.dataPath + "/test.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xml = new XmlSerializer(serilize.GetType());
        xml.Serialize(sw, serilize);
        sw.Close();
        fileStream.Close();
    }

    void XmlDeserilize()
    {
        FileStream fs = new  FileStream(Application.dataPath + "/test.xml", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        XmlSerializer xml = new XmlSerializer(typeof(XmlSerilier));
        XmlSerilier serilier = (XmlSerilier)xml.Deserialize(fs);
        fs.Close();
    }

    void BinarySerTest()
    {
        XmlSerilier serilize = new XmlSerilier();
        serilize.Id = 1;
        serilize.Name = "二进制测试";
        serilize.List = new List<int>();
        serilize.List.Add(0);
        serilize.List.Add(1);
        serilize.List.Add(2);
        BinarySerialize(serilize);
    }

    void BinarySerialize(XmlSerilier serilize)
    {
        FileStream fs = new FileStream(Application.dataPath + "/test.bytes", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bfm = new BinaryFormatter();
        bfm.Serialize(fs, serilize);
        fs.Close();
    }

    void BinaryDeserialize()
    {
        //TextAsset ta = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/test.bytes");
        //MemoryStream ms = new MemoryStream(ta.bytes);
        //BinaryFormatter bf = new BinaryFormatter();
        //XmlSerilier serilize = (XmlSerilier)bf.Deserialize(ms);
        //ms.Close();
        //Debug.Log(serilize.Id);
        //Debug.Log(serilize.Name);
    }
}
