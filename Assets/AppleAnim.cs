using UnityEngine;
using DG.Tweening;

public class AppleAnim : MonoBehaviour
{
    void Start()
    {
        // Starts at (100, 100, 100) → grows to (120, 120, 120) → shrinks back, forever
        transform.DOScale(new Vector3(120f, 120f, 120f), 1f)   // 1 s up-scale
            .SetEase(Ease.InOutSine)                      // smooth ease both ways
            .SetLoops(-1, LoopType.Yoyo);                 // infinite back-and-forth
    }
}
