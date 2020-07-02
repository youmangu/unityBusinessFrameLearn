using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        XmlDeserilize();
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
        Debug.Log(serilier.Id);
        Debug.Log(serilier.Name);
        foreach(int i in serilier.List)
            Debug.Log(i);
    }
}
