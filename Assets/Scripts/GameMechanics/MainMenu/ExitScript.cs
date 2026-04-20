using System.Collections;
using TMPro;
using UnityEngine;

public class ExitScript : MonoBehaviour
{
	[Header("Exit Settings")]
	[SerializeField]
	private float exitDelay = 3f;

	[SerializeField]
	private TextMeshProUGUI exitText;

	private Coroutine exitCoroutine;

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			exitText.text = $"Exiting in {exitDelay} seconds...";
			Debug.Log("Player entered exit area. Starting exit countdown.");
			exitCoroutine = StartCoroutine(ExitCountDown());
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player") && exitCoroutine != null)
		{
			StopCoroutine(exitCoroutine);
			exitCoroutine = null;
			exitText.text = "";
			Debug.Log("Player exited exit area. Exit countdown stopped.");
		}
	}

	private IEnumerator ExitCountDown()
	{
		yield return new WaitForSeconds(exitDelay);
		Debug.Log("Exit countdown completed. Quitting application.");
		Application.Quit();
	}
}