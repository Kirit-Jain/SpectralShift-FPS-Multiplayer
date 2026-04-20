using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
	public string JoinCode { get; private set; }

	public event Action<string> OnRelayCreated;

	private async Task EnsureAuthenticated()
	{
		if (UnityServices.State != ServicesInitializationState.Initialized)
		{
			InitializationOptions options = new InitializationOptions();
			options.SetProfile("Player_" + UnityEngine.Random.Range(0, 10000));
			await UnityServices.InitializeAsync(options);
		}
		if (!AuthenticationService.Instance.IsSignedIn)
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
			Debug.Log("Signed in! ID: " + AuthenticationService.Instance.PlayerId);
		}
	}

	public async Task<string> CreateRelay()
	{
		_ = 3;
		try
		{
			await EnsureAuthenticated();
			List<Region> obj = await RelayService.Instance.ListRegionsAsync();
			string id = obj[0].Id;
			foreach (Region item in obj)
			{
				if (item.Id.Contains("asia"))
				{
					id = item.Id;
					break;
				}
			}
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3, id);
			JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
			if (NetworkManager.Singleton.StartHost())
			{
				Debug.Log("Host Started! Code: " + JoinCode);
				this.OnRelayCreated?.Invoke(JoinCode);
				return JoinCode;
			}
			return null;
		}
		catch (RelayServiceException ex)
		{
			Debug.LogError("Relay Error: " + ex.Message);
			return null;
		}
	}

	public async void JoinRelay(string joinCode)
	{
		if (string.IsNullOrEmpty(joinCode))
		{
			return;
		}
		joinCode = joinCode.Trim().ToUpper();
		try
		{
			await EnsureAuthenticated();
			RelayServerData relayServerData = new RelayServerData(await RelayService.Instance.JoinAllocationAsync(joinCode), "dtls");
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
			NetworkManager.Singleton.StartClient();
		}
		catch (RelayServiceException ex)
		{
			Debug.LogError("Join Error: " + ex.Message);
		}
	}
}