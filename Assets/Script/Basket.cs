using DG.Tweening;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public enum AppleType
{
    Healthy,
    Rotten
}
public class Basket : MonoBehaviour
{
    public AppleType basketType;
    private AppleSpawner _appleSpawner = null;
    
    private void Start()
    {
        _appleSpawner = FindAnyObjectByType<AppleSpawner>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Apple>().appleType == basketType)
        {
            other.GetComponent<XRGrabInteractable>().enabled = false;
            other.transform.DOScale(Vector3.zero, 0.5f);
            other.transform.DOMove(transform.position, 0.5f).OnComplete(()=>other.GetComponent<Apple>().Pick());
            
        }
           
    }
}
