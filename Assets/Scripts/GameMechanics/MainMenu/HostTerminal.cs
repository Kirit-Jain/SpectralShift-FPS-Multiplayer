using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;


public class HostTerminal : MonoBehaviour, IInteractable
{
	[SerializeField]
	private RelayManager relayManager;

	[SerializeField]
	private TextMeshProUGUI displayCodeText;

	public string GetPrompt()
	{
		return "Press [E] to Host Game";
	}

	public void OnInteract()
	{
		StartCoroutine(DisplayCodeFlow());
	}

	private IEnumerator DisplayCodeFlow()
	{
		displayCodeText.text = "GENERATING CODE...";
		Task<string> relayTask = relayManager.CreateRelay();
		while (!relayTask.IsCompleted)
		{
			yield return null;
		}
		if (!string.IsNullOrEmpty(relayManager.JoinCode))
		{
			displayCodeText.text = "ID: " + relayManager.JoinCode;
		}
		else
		{
			displayCodeText.text = "FAILED TO GENERATE CODE";
		}
	}
}