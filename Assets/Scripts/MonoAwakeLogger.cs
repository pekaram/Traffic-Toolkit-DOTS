using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MonoAwakeLogger : MonoBehaviour
{
    private void Awake()
    {
        Application.targetFrameRate = 1000000;
    }
}
