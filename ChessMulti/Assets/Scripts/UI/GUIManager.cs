using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using NUnit.Framework;

/// <summary>
/// Manages the main menu, as well as the game UI display (scores + player turn)
/// </summary>
public class GUIManager : MonoBehaviour
{

    #region Singleton
    static GUIManager instance = null;
    public static GUIManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<GUIManager>();
            return instance;
        }
    }
    #endregion

    public Image mainMenu = null;

    public Button hostButton = null;
    public Button joinButton = null;
    public Button playButton = null;

    public TMP_InputField ipAddressInputField = null;
    public Button ipAddressEnterButton = null;

    public Text teamToMoveText = null;
    public Text whiteScoreText = null;
    public Text blackScoreText = null;

    public Text serverIPAdressText = null;
    public Toggle isLocalGame = null;

    public Chatbox chatbox = null;

    public Button returnButton = null;
    public Button quitButton = null;

    public Button spectateButton = null;
    public Button joinWhiteButton = null;
    public Button joinBlackButton = null;

    public UnityEvent<ChessGameManager.EChessTeam> onChangeTeam = null;

    void Awake()
    {
        hostButton.onClick.AddListener(LaunchHost);
        joinButton.onClick.AddListener(LaunchJoin);
        playButton.onClick.AddListener(ChessGameManager.Instance.PrepareAIGame);
        playButton.onClick.AddListener(LaunchPlay);
        returnButton.onClick.AddListener(ChessGameManager.Instance.ResetGame);
        quitButton.onClick.AddListener(QuitGame);

        ipAddressEnterButton.onClick.AddListener(EnterIPAdress);

        onChangeTeam = new UnityEvent<ChessGameManager.EChessTeam>();

        spectateButton.onClick.AddListener(OnSpectate);
        joinWhiteButton.onClick.AddListener(OnJoinWhite);
        joinBlackButton.onClick.AddListener(OnJoinBlack);

        SetMenu();

        ChessGameManager.Instance.OnPlayerTurn += DisplayTurn;
        ChessGameManager.Instance.OnScoreUpdated += UpdateScore;

    }

    void DisplayTurn(bool isWhiteMove)
    {
        if (isWhiteMove)
            teamToMoveText.text = "White To Move";
        else
            teamToMoveText.text = "Black To Move";
    }

    void UpdateScore(uint whiteScore, uint blackScore)
    {
        whiteScoreText.text = string.Format("White : {0}", whiteScore);
        blackScoreText.text = string.Format("Black : {0}", blackScore);
    }

    void LaunchHost()
    {
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);

        if (isLocalGame.isOn)
            serverIPAdressText.text = NetworkingManager.Instance.CreateLocalServer();
        else
            serverIPAdressText.text = NetworkingManager.Instance.CreateServer();

        returnButton.gameObject.SetActive(true);
    }

    void LaunchJoin()
    {
        hostButton.gameObject.SetActive(false);
        joinButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);

        ipAddressInputField.gameObject.SetActive(true);
        ipAddressEnterButton.gameObject.SetActive(true);

        returnButton.gameObject.SetActive(true);
    }

    public void LaunchPlay()
    {
        mainMenu.gameObject.SetActive(false);

        teamToMoveText.gameObject.SetActive(true);

        whiteScoreText.gameObject.SetActive(true);
        blackScoreText.gameObject.SetActive(true);

        returnButton.gameObject.SetActive(true);
    }

    void EnterIPAdress()
    {
        string ipAddress = ipAddressInputField.text;

        if (isLocalGame.isOn)
        {
            ipAddress = IPAddress.Loopback.ToString();
        }
        else if (ipAddress == "")
            return;
        if (NetworkingManager.Instance.TryJoinServer(ipAddress))
        {
            mainMenu.gameObject.SetActive(false);
            serverIPAdressText.text = ipAddress;
        }
        else
        {
            chatbox.AddErrorMessage("Connection attempt failed !", Color.red);
        }
    }

    public void SetTeam(ChessGameManager.EChessTeam _team)
    {
        ChessGameManager.Instance.SetLocalTeam(_team);
        LaunchPlay();
    }

    public void SetMenu()
    {
        mainMenu.gameObject.SetActive(true);

        hostButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        playButton.gameObject.SetActive(true);

        ipAddressInputField.gameObject.SetActive(false);
        ipAddressEnterButton.gameObject.SetActive(false);

        teamToMoveText.gameObject.SetActive(false);

        whiteScoreText.gameObject.SetActive(false);
        blackScoreText.gameObject.SetActive(false);

        returnButton.gameObject.SetActive(false);

        spectateButton.gameObject.SetActive(false);
        joinWhiteButton.gameObject.SetActive(false);
        joinBlackButton.gameObject.SetActive(false);
    }

    void QuitGame()
    {
        Application.Quit();
    }

    void OnSpectate()
    {
        onChangeTeam.Invoke(ChessGameManager.EChessTeam.None);
    }

    void OnJoinWhite()
    {
        onChangeTeam.Invoke(ChessGameManager.EChessTeam.White);
    }

    void OnJoinBlack()
    {
        onChangeTeam.Invoke(ChessGameManager.EChessTeam.Black);
    }

    public void UpdateTeamButtons(List<ChessGameManager.EChessTeam> teamList)
    {
        spectateButton.gameObject.SetActive(false);
        joinWhiteButton.gameObject.SetActive(false);
        joinBlackButton.gameObject.SetActive(false);

        if (teamList.Contains(ChessGameManager.EChessTeam.None))
            spectateButton.gameObject.SetActive(true);
        if (teamList.Contains(ChessGameManager.EChessTeam.White))
            joinWhiteButton.gameObject.SetActive(true);
        if (teamList.Contains(ChessGameManager.EChessTeam.Black))
            joinBlackButton.gameObject.SetActive(true);
    }

}
