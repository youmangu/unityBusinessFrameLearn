using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource audioSource;
    AudioClip clip;
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
        ResourceManager.Instance.AsyncLoadResource("Assets/GameData/Sounds/menusound.mp3", OnLoadFinished, LoadResPriority.RES_MIDDLE);
    }

    private void OnLoadFinished(string path, Object obj, object param1 = null, object param2 = null, object param3 = null)
    {
        clip = obj as AudioClip;
        audioSource.clip = clip;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            audioSource.Stop();
            audioSource.clip = null;
            ResourceManager.Instance.ReleaseResource(clip);
            clip = null;
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
