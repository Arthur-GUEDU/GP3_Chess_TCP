using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chatbox : MonoBehaviour
{
    int maxMessages = 25;
    List<GameObject> textObjects = new List<GameObject>();

    public GameObject ChatPanel;
    public GameObject TextObject;

    public TMP_InputField inputField;

    public void AddMessage(Message message)
    {
        GameObject textObject = Instantiate(TextObject, ChatPanel.transform);
        textObject.transform.SetAsFirstSibling();
        textObject.GetComponent<TextMeshProUGUI>().text = message.ToString();
        textObjects.Add(textObject);
        if(textObjects.Count > maxMessages)
        {
            Destroy(textObjects[0]);
            textObjects.RemoveAt(0);
        }
    }

    public void AddErrorMessage(string message, UnityEngine.Color color)
    {
        GameObject textObject = Instantiate(TextObject, ChatPanel.transform);
        textObject.transform.SetAsFirstSibling();
        textObject.GetComponent<TextMeshProUGUI>().text = message;
        textObject.GetComponent<TextMeshProUGUI>().color = color;
        textObjects.Add(textObject);
        if (textObjects.Count > maxMessages)
        {
            Destroy(textObjects[0]);
            textObjects.RemoveAt(0);
        }
    }

    public void ClearInputField()
    {
        inputField.text = "";
    }
}

public class Message
{
    ChessServer.ChessPlayer player;
    string text;

    public Message(ChessServer.ChessPlayer player, string text)
    {
        this.player = player;
        this.text = text;
    }

    public string Encode()
    {
        return player.id.ToString() + ":" + text;
    }

    public override string ToString()
    {
        return "Player " + player.id.ToString() + ": " + text;
    }

    public static Message Decode(string code)
    {
        ChessServer.ChessPlayer player = new ChessServer.ChessPlayer(code.Substring(0, 1));
        string text = code.Substring(1);
        return new Message(player,text);
    }
}

