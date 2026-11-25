using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class PooledParticleAutoReturn : MonoBehaviour
{
    void OnParticleSystemStopped()
    {
        if (ObjectPoolManager.HasInstance)
        {
            ObjectPoolManager.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
