using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryController : MonoBehaviour
{
    public static StoryController instance;

    [SerializeField] List<ContentStory> lstContent = new List<ContentStory>();

    [SerializeField] Conversation m_conversation;

    CreateData data;

    public static int numConversation;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Debug.Log("Start");
        data = GameManager.instance.GetData();
        if(data == null)
        {
            GameManager.instance.LoadScene("Continue");
            return;
        }
        lstContent = data.Data;
        Debug.Log(lstContent.Count);
        numConversation = 0;
        SetConversation();
    }

    public void SetConversation()
    {
        if(numConversation < lstContent.Count)
        {
            Debug.Log("numConversation: " + numConversation + " lstContent[numConversation]: " + lstContent[numConversation].content);
            m_conversation.Setup(lstContent[numConversation]);
            numConversation++;
        }    
        else
        {
            GameManager.Data = new DataForSceneGame(data.Player[0].Character, data.Enemy[0].Character, data.itemReward.ItemReward);
            GameManager.instance.LoadScene("Game", new DataForSceneGame(data.Player[0].Character, data.Enemy[0].Character, data.itemReward.ItemReward));
        }    
    }

}
