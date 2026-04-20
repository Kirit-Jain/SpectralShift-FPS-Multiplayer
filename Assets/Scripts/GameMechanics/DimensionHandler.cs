using Unity.Netcode;
using UnityEngine;

public class DimensionHandler : NetworkBehaviour
{
    [SerializeField]
    private Camera playerCamera;

    private string redLayer = "RedDimension";
    private string blueLayer = "BlueDimension";
    private string redPlayerLayer = "RedPlayer";
    private string bluePlayerLayer = "BluePlayer";

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable camera and audio for other players
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                if (playerCamera.TryGetComponent<AudioListener>(out var listener))
                {
                    listener.enabled = false;
                }
            }
            SetTeamlayer();
        }
        else if (playerCamera != null)
        {
            // Setup local player camera
            playerCamera.enabled = true;
            playerCamera.gameObject.SetActive(true);
            playerCamera.tag = "MainCamera";
            
            if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = true;
            }
            
            SetTeamlayer();
            UpdateView();
        }
    }

    private void SetTeamlayer()
    {
        // This relies on your SmartLevelGenerator script being recovered too!
        string layerName = (SmartLevelGenerator.isRed(OwnerClientId) ? redPlayerLayer : bluePlayerLayer);
        gameObject.layer = LayerMask.NameToLayer(layerName);
    }

    private void UpdateView()
    {
        int redIdx = LayerMask.NameToLayer(redLayer);
        int blueIdx = LayerMask.NameToLayer(blueLayer);

        if (SmartLevelGenerator.isRed(OwnerClientId))
        {
            if (playerCamera != null)
            {
                // Culling Mask: Hide the Blue Dimension for Red players
                playerCamera.cullingMask = ~(1 << blueIdx);
            }
            SetColor(Color.red);
        }
        else
        {
            if (playerCamera != null)
            {
                // Culling Mask: Hide the Red Dimension for Blue players
                playerCamera.cullingMask = ~(1 << redIdx);
            }
            SetColor(Color.blue);
        }
    }

    private void SetColor(Color c)
    {
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = c;
        }
    }
}