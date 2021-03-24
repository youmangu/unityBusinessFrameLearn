using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class AssetBundleconfig
{
    [XmlElement("ABList")]
    public List<ABBase> ABList { set; get; }
}

[System.Serializable]
public class ABBase
{
    [XmlAttribute("Path")]
    public string Path { set; get; }   // 方便在xml文件里查看
    [XmlAttribute("Crc")]
    public uint Crc { set; get; }
    [XmlAttribute("ABName")]
    public string ABName { set; get; }
    [XmlAttribute("AssetName")]
    public string AssetName{set; get;}
    [XmlElement("ABList")]
    public List<string> ABDependence { set; get; }
}
