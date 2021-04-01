using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource audioSource;
    private void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
    }
    // Start is called before the first frame update
    void Start()
    {
        AudioClip clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
        audioSource.clip = clip;
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
