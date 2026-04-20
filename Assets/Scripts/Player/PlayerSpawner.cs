using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : NetworkBehaviour
{
	[Header("Player Prefab")]
	[SerializeField]
	private GameObject redPlayerPrefab;

	[SerializeField]
	private GameObject bluePlayerPrefab;

	public override void OnNetworkSpawn()
	{
		if (base.IsServer)
		{
			NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
			NetworkManager.Singleton.OnClientConnectedCallback += SpawnSinglePlayer;
		}
	}

	private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadMode, List<ulong> clientsCompleted, List<ulong> clientTimeOut)
	{
		if (!(sceneName == "MainMap"))
		{
			return;
		}
		Debug.Log("Arena Loaded. Respawning all connected players...");
		foreach (ulong connectedClientsId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			SpawnSinglePlayer(connectedClientsId);
		}
	}

	private void SpawnSinglePlayer(ulong clientId)
	{
		if (base.IsServer)
		{
			StartCoroutine(SpawnWithDelay(clientId));
		}
	}

	private IEnumerator SpawnWithDelay(ulong clientId)
	{
		yield return new WaitForSeconds(0.2f);
		if (!(NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null))
		{
			GameObject original = (SmartLevelGenerator.isRed(clientId) ? redPlayerPrefab : bluePlayerPrefab);
			SmartLevelGenerator smartLevelGenerator = Object.FindFirstObjectByType<SmartLevelGenerator>();
			Vector3 vector = ((smartLevelGenerator != null) ? smartLevelGenerator.GetSpawnPosition(clientId) : new Vector3(0f, 10f, 0f));
			Debug.Log($"Spawning player for client {clientId} at {vector}");
			GameObject obj = Object.Instantiate(original, vector, Quaternion.identity);
			obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, destroyWithScene: true);
			PlayerController component = obj.GetComponent<PlayerController>();
			if (component != null)
			{
				component.ForceTeleportClientRpc(vector);
			}
			yield return null;
		}
	}

	public override void OnNetworkDespawn()
	{
		if (base.IsServer && NetworkManager.Singleton != null)
		{
			if (NetworkManager.Singleton.SceneManager != null)
			{
				NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
			}
			NetworkManager.Singleton.OnClientConnectedCallback -= SpawnSinglePlayer;
		}
	}
}