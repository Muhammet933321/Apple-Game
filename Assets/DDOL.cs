using System;
using UnityEngine;

public class DDOL : MonoBehaviour
{
    private void Awake()
    {
        // Don't destroy on load
        DontDestroyOnLoad(this);
    }
}
