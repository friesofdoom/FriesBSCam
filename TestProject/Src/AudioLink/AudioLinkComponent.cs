using UnityEngine;
using System.Collections;

public class AudioLinkComponent : MonoBehaviour
{
    private static AudioLink.Scripts.AudioLink? _audioLink = null;

    // Use this for initialization
    void Start()
    {
        if (_audioLink == null)
        {
            Logger.Log("Starting AudioLink");
            _audioLink = new AudioLink.Scripts.AudioLink();
            Logger.Log("AudioLink Started");
        }
    }

    // Update is called once per frame
    void Update()
    {
        _audioLink?.Tick();
    }
}
