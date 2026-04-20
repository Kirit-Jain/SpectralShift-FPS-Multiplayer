using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;

    [Header("Timing")]
    [SerializeField] private float deathAnimDuration = 3f;
    [SerializeField] private float hiddenDuration = 1f;

    [Header("Prefab reference")]
    [SerializeField] private GameObject sparkPrefab;

    private bool isInvincible;
    private bool isRespawning;
    private ulong lastAttackerClientId;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);

    public override void OnNetworkSpawn()
    {
        // Hook into health changes to play effects
        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        StopAllCoroutines();
        isRespawning = false;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        if (newVal < oldVal)
        {
            PlayDamageEffectClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong attackerClientId)
    {
        if (currentHealth.Value > 0 && !isInvincible)
        {
            lastAttackerClientId = attackerClientId;
            currentHealth.Value -= damage;

            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0;
                Die();
            }
        }
    }

    private void Die()
    {
        isRespawning = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterKill(lastAttackerClientId);
        }

        float totalWait = deathAnimDuration + hiddenDuration;
        HandleDeathClientRpc(totalWait);
        StartCoroutine(RespawnCoroutine(totalWait));
    }

    [ClientRpc]
    private void HandleDeathClientRpc(float totalWait)
    {
        GetComponent<Collider>().enabled = false;

        if (IsOwner)
        {
            GetComponent<PlayerController>().enabled = false;
            if (GetComponent<WeaponController>()) GetComponent<WeaponController>().enabled = false;
        }

        StartCoroutine(DeathVisualCoroutine());
    }

    [ClientRpc]
    private void PlayDamageEffectClientRpc()
    {
        if (sparkPrefab != null)
        {
            Vector3 position = transform.position + Vector3.up * 1f;
            GameObject spark = Instantiate(sparkPrefab, position, Quaternion.identity, transform);
            Destroy(spark, 1f);
        }
    }

    private IEnumerator DeathVisualCoroutine()
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsDead", true);
        }

        yield return new WaitForSeconds(deathAnimDuration);
        SetRenderersVisible(false);
    }

    private IEnumerator RespawnCoroutine(float totalWait)
    {
        yield return new WaitForSeconds(totalWait);

        if (IsSpawned)
        {
            currentHealth.Value = maxHealth;
            
            // Reset Ammo
            WeaponController weapon = GetComponent<WeaponController>();
            if (weapon != null) weapon.ResetAmmoOnRespawn();

            // Find spawn position based on Team logic
            Vector3 spawnPos = new Vector3(0, 2, 0);
            SmartLevelGenerator levelGen = UnityEngine.Object.FindFirstObjectByType<SmartLevelGenerator>();
            if (levelGen != null)
            {
                spawnPos = levelGen.GetSpawnPosition(OwnerClientId);
            }

            RespawnClientRpc(spawnPos);
            isRespawning = false;
        }
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 position)
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetBool("IsDead", false);
            anim.transform.localPosition = Vector3.zero;
            anim.transform.localRotation = Quaternion.identity;
        }

        GetComponent<PlayerController>().TeleportTo(position);
        transform.rotation = Quaternion.identity;
        
        RestoreRenderers();
        GetComponent<Collider>().enabled = true;

        if (IsOwner)
        {
            GetComponent<PlayerController>().enabled = true;
            if (GetComponent<WeaponController>()) GetComponent<WeaponController>().enabled = true;
        }

        StartCoroutine(InvincibilityTimer());
    }

    private IEnumerator InvincibilityTimer()
    {
        isInvincible = true;
        SetRenderersAlpha(0.5f);
        yield return new WaitForSeconds(3f);
        isInvincible = false;
        SetRenderersAlpha(1f);
    }

    private void SetRenderersAlpha(float alpha)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;
    }

    private void RestoreRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
            if (IsOwner && r.gameObject.name == "Body_Mesh")
            {
                r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }
    }
}