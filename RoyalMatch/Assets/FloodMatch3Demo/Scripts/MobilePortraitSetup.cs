using UnityEngine;

public class MobilePortraitSetup : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Portrait;
        Application.targetFrameRate = 60;
#endif
    }
}
