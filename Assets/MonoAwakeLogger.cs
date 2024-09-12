using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MonoAwakeLogger : MonoBehaviour
{

    private void Awake()
    {
        var x = GraphicsSettings.useScriptableRenderPipelineBatching;
        Debug.LogError(x);
    }
}
