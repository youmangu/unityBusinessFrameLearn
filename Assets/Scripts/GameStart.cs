using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource audioSource;
    AudioClip clip;
    private void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
    }
    // Start is called before the first frame update
    void Start()
    {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
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
            ResourceManager.Instance.ReleaseResource(clip, true);
            clip = null;
        }
    }
}
