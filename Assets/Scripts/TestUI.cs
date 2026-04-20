using Unity.Netcode;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    [SerializeField] private RelayManager relayManager;
    
    private string joinCodeInput = "";

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 300, 200));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // HOST BUTTON
            if (GUILayout.Button("Create Game (Host)"))
            {
                relayManager.CreateRelay();
            }

            GUILayout.Space(10);

            // JOIN SECTION
            GUILayout.Label("Join Code:");
            joinCodeInput = GUILayout.TextField(joinCodeInput);

            if (GUILayout.Button("Join Game"))
            {
                relayManager.JoinRelay(joinCodeInput); 
            }
        }
        else
        {
            GUILayout.Label("Status: Connected");
            if (relayManager != null) GUILayout.Label("Code: " + relayManager.JoinCode);
        }

        GUILayout.EndArea();
    }
}