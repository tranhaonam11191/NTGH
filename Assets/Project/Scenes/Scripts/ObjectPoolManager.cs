using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    static ObjectPoolManager s_instance;

    readonly Dictionary<GameObject, Stack<GameObject>> m_available = new Dictionary<GameObject, Stack<GameObject>>();
    readonly Dictionary<GameObject, GameObject> m_rented = new Dictionary<GameObject, GameObject>();

    public static bool HasInstance => s_instance != null;

    public static ObjectPoolManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                var root = new GameObject(nameof(ObjectPoolManager));
                s_instance = root.AddComponent<ObjectPoolManager>();
            }

            return s_instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetInstance()
    {
        s_instance = null;
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Prewarm<T>(T template, int count) where T : Component
    {
        if (template == null || count <= 0) return;

        GameObject key = template.gameObject;
        Stack<GameObject> pool = GetOrCreatePool(key);
        int toCreate = count - pool.Count;
        for (int i = 0; i < toCreate; i++)
        {
            GameObject instance = Instantiate(key, transform);
            instance.SetActive(false);
            pool.Push(instance);
        }
    }

    public T Rent<T>(T template, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        if (template == null) return null;

        GameObject key = template.gameObject;
        Stack<GameObject> pool = GetOrCreatePool(key);

        GameObject instance = null;
        while (pool.Count > 0 && instance == null)
        {
            instance = pool.Pop();
        }

        if (instance == null)
        {
            instance = Instantiate(key, position, rotation, parent);
        }
        else
        {
            Transform tr = instance.transform;
            tr.SetParent(parent, false);
            tr.SetPositionAndRotation(position, rotation);
            tr.localScale = key.transform.localScale;
        }

        instance.SetActive(true);
        m_rented[instance] = key;
        return instance.GetComponent<T>();
    }

    public void Return(Component instance)
    {
        if (instance == null) return;
        Return(instance.gameObject);
    }

    public void Return(GameObject instance)
    {
        if (ReferenceEquals(instance, null) || !m_rented.TryGetValue(instance, out GameObject key))
        {
            return;
        }

        m_rented.Remove(instance);
        if (instance == null) return;

        if (key == null)
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);
        GetOrCreatePool(key).Push(instance);
    }

    public void ClearPool(GameObject template)
    {
        if (ReferenceEquals(template, null)) return;

        if (m_available.TryGetValue(template, out Stack<GameObject> pool))
        {
            while (pool.Count > 0)
            {
                GameObject idle = pool.Pop();
                if (idle != null) Destroy(idle);
            }
            m_available.Remove(template);
        }

        List<GameObject> rentedForTemplate = null;
        foreach (var pair in m_rented)
        {
            if (pair.Value == template)
            {
                if (rentedForTemplate == null) rentedForTemplate = new List<GameObject>();
                rentedForTemplate.Add(pair.Key);
            }
        }

        if (rentedForTemplate != null)
        {
            for (int i = 0; i < rentedForTemplate.Count; i++)
            {
                GameObject active = rentedForTemplate[i];
                m_rented.Remove(active);
                if (active != null) Destroy(active);
            }
        }
    }

    Stack<GameObject> GetOrCreatePool(GameObject key)
    {
        if (!m_available.TryGetValue(key, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            m_available.Add(key, pool);
        }

        return pool;
    }

    void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }
}
