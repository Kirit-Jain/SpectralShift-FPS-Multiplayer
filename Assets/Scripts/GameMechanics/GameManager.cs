using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [Header("Match Setting")]
    [SerializeField] public float matchDuration = 30f;
    [SerializeField] private int playersRequired = 2;

    [Header("End Game UI")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Image panelBackground;

    // Network Variables stay in sync across all players automatically
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(0f);
    public NetworkVariable<int> redKills = new NetworkVariable<int>(0);
    public NetworkVariable<int> blueKills = new NetworkVariable<int>(0);
    public NetworkVariable<bool> matchActive = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> waitingForPlayers = new NetworkVariable<bool>(true);

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = matchDuration;
            redKills.Value = 0;
            blueKills.Value = 0;
            matchActive.Value = false;
            waitingForPlayers.Value = true;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            StartCoroutine(WaitForPlayersCoroutine());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Update()
    {
        if (IsServer && matchActive.Value)
        {
            timeRemaining.Value -= Time.deltaTime;
            if (timeRemaining.Value <= 0f)
            {
                timeRemaining.Value = 0f;
                matchActive.Value = false;
                EndMatchClientRpc(); // Notify all clients the match is over
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        int count = NetworkManager.Singleton.ConnectedClientsList.Count;
        Debug.Log($"Client {clientId} connected. Total: {count}/{playersRequired}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (matchActive.Value)
        {
            matchActive.Value = false;
            waitingForPlayers.Value = true;
            StopAllCoroutines();
            StartCoroutine(WaitForPlayersCoroutine());
            Debug.Log("Player left — match paused.");
        }
    }

    private IEnumerator WaitForPlayersCoroutine()
    {
        while (!matchActive.Value)
        {
            yield return new WaitForSeconds(0.5f);
            if (NetworkManager.Singleton == null) break;

            if (NetworkManager.Singleton.ConnectedClientsList.Count >= playersRequired)
            {
                StartMatch();
                break;
            }
        }
    }

    private void StartMatch()
    {
        timeRemaining.Value = matchDuration;
        redKills.Value = 0;
        blueKills.Value = 0;
        matchActive.Value = true;
        waitingForPlayers.Value = false;
    }

    public void RegisterKill(ulong killerClientId)
    {
        if (!IsServer) return;

        if (SmartLevelGenerator.isRed(killerClientId))
            redKills.Value++;
        else
            blueKills.Value++;
    }

    [ClientRpc]
    private void EndMatchClientRpc()
    {
        if (endGamePanel == null) return;

        endGamePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (redKills.Value > blueKills.Value)
        {
            winnerText.text = "Red Team Wins!";
            panelBackground.color = new Color(1f, 0f, 0f, 0.8f);
        }
        else if (blueKills.Value > redKills.Value)
        {
            winnerText.text = "Blue Team Wins!";
            panelBackground.color = new Color(0f, 0f, 1f, 0.8f);
        }
        else
        {
            winnerText.text = "It's a Tie!";
            panelBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        scoreText.text = $"RED: {redKills.Value}  |  BLUE: {blueKills.Value}";
        StartCoroutine(CleanupAndReturnToLobby());
    }

    private IEnumerator CleanupAndReturnToLobby()
    {
        yield return new WaitForSeconds(5f);
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("Lobby");
    }
}