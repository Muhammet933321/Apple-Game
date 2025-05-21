using UnityEngine;

public class GrabEffect : MonoBehaviour
{
    public void FireEffect(Vector3 position)
    {
        transform.position = position;
        GetComponent<ParticleSystem>().Play();
        GetComponent<AudioSource>().Play();
    }
}
