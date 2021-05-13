using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private GameObject obj;
    private void Awake()
    {
        GameObject.DontDestroyOnLoad(gameObject);
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"), transform.Find("SceneTrs")); 
    }
    // Start is called before the first frame update
    void Start()
    {
        //ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Sounds/menusound.mp3", OnLoadFinished, LoadResPriority.RES_MIDDLE);
        obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true);
    }

    private void OnLoadFinished(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ObjectManager.Instance.ReleaseObject(obj);
            obj = null;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            obj = ObjectManager.Instance.InstantiateObject("Assets/GameData/Prefabs/Attack.prefab", true);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            ObjectManager.Instance.ReleaseObject(obj, 0, true);
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
#endif
    }
}
