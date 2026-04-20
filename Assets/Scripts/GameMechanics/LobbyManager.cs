using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
	[SerializeField]
	private TextMeshProUGUI lobbyStatusText;

	[SerializeField]
	private string arenaSceneName = "Arena Map";

	private NetworkVariable<int> playersInLobby = new NetworkVariable<int>(0);

	private bool isStarting;

	public override void OnNetworkSpawn()
	{
		if (base.IsServer)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
			playersInLobby.Value = 1;
		}
		NetworkVariable<int> networkVariable = playersInLobby;
		networkVariable.OnValueChanged = (NetworkVariable<int>.OnValueChangedDelegate)Delegate.Combine(networkVariable.OnValueChanged, new NetworkVariable<int>.OnValueChangedDelegate(UpdateLobbyUI));
		UpdateLobbyUI(0, playersInLobby.Value);
	}

	private void OnPlayerJoined(ulong clientId)
	{
		playersInLobby.Value = NetworkManager.Singleton.ConnectedClients.Count;
		if (playersInLobby.Value >= 2 && !isStarting)
		{
			isStarting = true;
			StartCountdownClientRpc();
		}
	}

	[Rpc(SendTo.Everyone)]
	private void StartCountdownClientRpc()
	{
		NetworkManager networkManager = base.NetworkManager;
		if ((object)networkManager == null || !networkManager.IsListening)
		{
			Debug.LogError("Rpc methods can only be invoked after starting the NetworkManager!");
			return;
		}
		if (__rpc_exec_stage != __RpcExecStage.Execute)
		{
			RpcAttribute.RpcAttributeParams attributeParams = default(RpcAttribute.RpcAttributeParams);
			RpcParams rpcParams = default(RpcParams);
			FastBufferWriter bufferWriter = __beginSendRpc(795531027u, rpcParams, attributeParams, SendTo.Everyone, RpcDelivery.Reliable);
			__endSendRpc(ref bufferWriter, 795531027u, rpcParams, attributeParams, SendTo.Everyone, RpcDelivery.Reliable);
		}
		if (__rpc_exec_stage == __RpcExecStage.Execute)
		{
			__rpc_exec_stage = __RpcExecStage.Send;
			StartCoroutine(CountdownCoroutine());
		}
	}

	private IEnumerator CountdownCoroutine()
	{
		for (float timer = 3f; timer > 0f; timer -= 1f)
		{
			lobbyStatusText.text = $"MATCH STARTING IN: {Mathf.Ceil(timer)}";
			lobbyStatusText.color = Color.yellow;
			yield return new WaitForSeconds(1f);
		}
		lobbyStatusText.text = "UPLOADING...";
		lobbyStatusText.color = Color.cyan;
		if (base.IsServer)
		{
			NetworkManager.Singleton.SceneManager.LoadScene(arenaSceneName, LoadSceneMode.Single);
		}
	}

	private void UpdateLobbyUI(int oldVal, int newVal)
	{
		if (!isStarting)
		{
			lobbyStatusText.text = $"WAITING FOR PLAYERS... ({newVal}/2)";
		}
	}
}