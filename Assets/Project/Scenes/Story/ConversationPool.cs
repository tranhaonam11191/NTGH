using System.Collections.Generic;
using UnityEngine;

public class ConversationPool : MonoBehaviour
{
    static ConversationPool s_instance;

    readonly Dictionary<Conversation, Stack<Conversation>> m_available = new Dictionary<Conversation, Stack<Conversation>>();
    readonly Dictionary<Conversation, Conversation> m_rented = new Dictionary<Conversation, Conversation>();

    public static ConversationPool Instance
    {
        get
        {
            if (s_instance == null)
            {
                var root = new GameObject(nameof(ConversationPool));
                s_instance = root.AddComponent<ConversationPool>();
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

    public Conversation Rent(Conversation prefab, Transform parent)
    {
        if (!m_available.TryGetValue(prefab, out var available))
        {
            available = new Stack<Conversation>();
            m_available.Add(prefab, available);
        }

        Conversation conversation = null;
        while (available.Count > 0 && conversation == null)
        {
            conversation = available.Pop();
        }

        if (conversation == null)
        {
            conversation = Instantiate(prefab, parent);
        }
        else
        {
            conversation.transform.SetParent(parent, false);
        }

        conversation.transform.SetAsLastSibling();
        conversation.gameObject.SetActive(true);
        m_rented.Add(conversation, prefab);
        return conversation;
    }

    public void Return(Conversation conversation)
    {
        if (ReferenceEquals(conversation, null) || !m_rented.TryGetValue(conversation, out var prefab))
        {
            return;
        }

        m_rented.Remove(conversation);
        if (conversation == null)
        {
            return;
        }

        conversation.gameObject.SetActive(false);
        conversation.ResetForPool();
        conversation.transform.SetParent(transform, false);
        m_available[prefab].Push(conversation);
    }

    void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }
}
