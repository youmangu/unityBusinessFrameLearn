using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "assetsTest", menuName = "CreateAssets", order = 0)]
public class AssetsSerilize : ScriptableObject
{
    public int id;
    public string assetName;
    public List<string> testList;

}
