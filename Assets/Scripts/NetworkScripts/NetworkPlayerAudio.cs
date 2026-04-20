using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerAudio : MonoBehaviour
{
	public AudioSource audioSource;

	public AudioClip fireAudioClip;

	public AudioClip footstepAudioClip;

	[Rpc(SendTo.Server)]
	public void RequestFireServerRpc()
	{
		PlayFireClientRpc();
	}

	[Rpc(SendTo.NotMe)]
	public void PlayFireClientRpc()
	{
		audioSource.PlayOneShot(fireAudioClip);
	}

	public void PlayFootstep()
	{
		audioSource.PlayOneShot(footstepAudioClip);
	}
}