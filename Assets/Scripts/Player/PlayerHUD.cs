using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : NetworkBehaviour
{
    [Header("Health And Ammo")]
    [SerializeField] private Image healthbarFill;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI ammoStatus;
    [SerializeField] private PlayerHealth healthScript;
    [SerializeField] private WeaponController weaponScript;

    [Header("Scoreboard")]
    [SerializeField] private TextMeshProUGUI redKillsText;
    [SerializeField] private TextMeshProUGUI blueKillsText;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText;

    public override void OnNetworkSpawn()
    {
        // Only show the HUD for the local player
        if (!IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        // Subscribe to Health Changes
        if (healthScript != null)
        {
            healthScript.currentHealth.OnValueChanged += UpdateHealthBar;
            // Initialize the bar immediately
            UpdateHealthBar(0, healthScript.currentHealth.Value);
        }

        // Subscribe to GameManager data (Score and Match State)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.redKills.OnValueChanged += UpdateRedKills;
            GameManager.Instance.blueKills.OnValueChanged += UpdateBlueKills;
            GameManager.Instance.waitingForPlayers.OnValueChanged += OnWaitingChanged;

            // Initial UI Sync
            UpdateRedKills(0, GameManager.Instance.redKills.Value);
            UpdateBlueKills(0, GameManager.Instance.blueKills.Value);
        }

        if (ammoStatus != null)
        {
            ammoStatus.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Clean up subscriptions to prevent memory leaks or null errors
        if (healthScript != null)
        {
            healthScript.currentHealth.OnValueChanged -= UpdateHealthBar;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.redKills.OnValueChanged -= UpdateRedKills;
            GameManager.Instance.blueKills.OnValueChanged -= UpdateBlueKills;
            GameManager.Instance.waitingForPlayers.OnValueChanged -= OnWaitingChanged;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            UpdateTimer();
            UpdateAmmoUI();
        }
    }

    private void UpdateHealthBar(int oldVal, int newVal)
    {
        float ratio = (float)newVal / 100f;
        healthbarFill.fillAmount = ratio;
        // Changes color from Red (low) to Green (high)
        healthbarFill.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    private void UpdateAmmoUI()
    {
        if (weaponScript == null) return;

        if (ammoText != null)
        {
            int current = weaponScript.currentAmmo.Value;
            int total = weaponScript.totalReserve.Value;
            ammoText.text = $"{current} / {total}";

            // Color coding for ammo levels
            if (current == 0) ammoText.color = Color.red;
            else if (current <= 3) ammoText.color = new Color(1f, 0.5f, 0f); // Orange
            else ammoText.color = Color.white;
        }

        if (ammoStatus != null)
        {
            if (weaponScript.isReloading.Value)
            {
                ammoStatus.gameObject.SetActive(true);
                ammoStatus.text = "RELOADING...";
                ammoStatus.color = Color.white;
            }
            else if (weaponScript.currentAmmo.Value <= 0)
            {
                ammoStatus.gameObject.SetActive(true);
                ammoStatus.text = "RELOAD";
                // Flashing red effect for "RELOAD" prompt
                float t = (Mathf.Sin(Time.time * 12f) + 1f) / 2f;
                ammoStatus.color = Color.Lerp(new Color(0.3f, 0f, 0f), Color.red, t);
            }
            else
            {
                ammoStatus.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateRedKills(int oldVal, int newVal)
    {
        if (redKillsText != null) redKillsText.text = newVal.ToString();
    }

    private void UpdateBlueKills(int oldVal, int newVal)
    {
        if (blueKillsText != null) blueKillsText.text = newVal.ToString();
    }

    private void OnWaitingChanged(bool oldVal, bool newVal)
    {
        // Greys out the score while waiting for players
        Color color = newVal ? Color.grey : Color.white;
        if (redKillsText != null) redKillsText.color = color;
        if (blueKillsText != null) blueKillsText.color = color;
    }

    private void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;

        if (GameManager.Instance.waitingForPlayers.Value)
        {
            timerText.text = "Waiting for players...";
            timerText.color = Color.yellow;
            return;
        }

        float time = Mathf.Max(0f, GameManager.Instance.timeRemaining.Value);
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText.color = (time <= 30f) ? Color.red : Color.white;
    }
}