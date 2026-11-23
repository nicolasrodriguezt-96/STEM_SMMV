
using UnityEngine;

public class ForcePortraitResolution : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Screen.SetResolution(
            540,
            960,
            FullScreenMode.Windowed
        );
#endif
    }
}
