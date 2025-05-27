using System;
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

    private void OnTriggerStay(Collider other)
    {
        /*
        if (other != null && other.gameObject != null)
        {
            Apple apple = other.gameObject.GetComponent<Apple>();
            if (apple.isReleased)
            {
                Debug.Log(other.gameObject.name);
                bool isCorrectBasket = other.GetComponent<Apple>().appleType == basketType;
                other.GetComponent<XRGrabInteractable>().enabled = false;
                other.GetComponent<Apple>().Pick(isCorrectBasket);
                
            }
        }*/
    }
}
