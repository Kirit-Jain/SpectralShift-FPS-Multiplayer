using TMPro;
using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
	[SerializeField]
	private float interactRange = 3f;

	[SerializeField]
	private LayerMask interactableLayer;

	[SerializeField]
	private TextMeshProUGUI promptText;

	private void Update()
	{
		if (Physics.Raycast(new Ray(base.transform.position, base.transform.forward), out var hitInfo, interactRange, interactableLayer))
		{
			if (hitInfo.collider.TryGetComponent<IInteractable>(out var component))
			{
				promptText.text = component.GetPrompt();
				if (Input.GetKeyDown(KeyCode.E))
				{
					component.OnInteract();
				}
			}
		}
		else
		{
			promptText.text = "";
		}
	}
}