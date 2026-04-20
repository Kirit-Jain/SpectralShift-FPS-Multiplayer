using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WeaponController : NetworkBehaviour
{
    [Header("Gun Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.2f;

    [Header("Visual Effects")]
    [SerializeField] private GameObject paintSplatPrefab;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private float splatLifeTime = 10f;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LineRenderer bulletTrail;

    [Header("Ammo Settings")]
    [SerializeField] private int maxAmmo = 12;
    [SerializeField] private float reloadTime = 4f;

    public NetworkVariable<int> currentAmmo = new NetworkVariable<int>(12);
    public NetworkVariable<int> totalReserve = new NetworkVariable<int>(36);
    public NetworkVariable<bool> isReloading = new NetworkVariable<bool>(false);

    private NetworkPlayerAudio playerAudio;
    private float nextFireTime;

    private void Start()
    {
        playerAudio = GetComponentInChildren<NetworkPlayerAudio>();
    }

    private void Update()
    {
        // Don't shoot if match is inactive, we don't own this player, or we are reloading
        if ((GameManager.Instance != null && !GameManager.Instance.matchActive.Value) || !IsOwner || isReloading.Value)
        {
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextFireTime && currentAmmo.Value > 0)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentAmmo.Value < maxAmmo && !isReloading.Value)
        {
            ReloadServerRpc();
        }
    }

    private void Shoot()
    {
        ReduceAmmoServerRpc();
        ProcessLocalShootEffects();

        Vector3 hitPoint;
        Vector3 hitNormal = Vector3.zero;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, range))
        {
            hitPoint = hit.point;
            hitNormal = hit.normal;

            PlayerHealth targetHealth = hit.transform.GetComponentInParent<PlayerHealth>();
            
            if (targetHealth != null && targetHealth.gameObject != gameObject)
            {
                // Deal damage to other players
                targetHealth.TakeDamageServerRpc((int)damage, OwnerClientId);
            }
            else if (targetHealth == null)
            {
                // Spawn paint splat on walls/floor
                SpawnPaintSplatServerRpc(hitPoint, hitNormal);
            }
        }
        else
        {
            hitPoint = playerCamera.transform.position + playerCamera.transform.forward * range;
        }

        StartCoroutine(ShowBulletTrail(firePoint.position, hitPoint));
        ShootServerRpc(firePoint.position, hitPoint);
    }

    private void ProcessLocalShootEffects()
    {
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Shoot");

        if (playerAudio != null) playerAudio.RequestFireServerRpc();
        if (muzzleFlash != null) muzzleFlash.Play();
    }

    [ServerRpc]
    private void ReloadServerRpc()
    {
        if (!isReloading.Value)
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    [ServerRpc]
    private void ReduceAmmoServerRpc()
    {
        if (currentAmmo.Value > 0)
        {
            currentAmmo.Value--;
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 start, Vector3 end)
    {
        ShootClientRpc(start, end);
    }

    [ClientRpc]
    private void ShootClientRpc(Vector3 start, Vector3 end)
    {
        if (!IsOwner)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) anim.SetTrigger("Shoot");
            if (muzzleFlash != null) muzzleFlash.Play();
            StartCoroutine(ShowBulletTrail(start, end));
        }
    }

    [ServerRpc]
    private void SpawnPaintSplatServerRpc(Vector3 position, Vector3 normal)
    {
        SpawnPaintSplatClientRpc(position, normal);
    }

    [ClientRpc]
    private void SpawnPaintSplatClientRpc(Vector3 position, Vector3 normal)
    {
        if (paintSplatPrefab != null)
        {
            Vector3 splatPos = position + normal * 0.01f;
            Quaternion splatRot = Quaternion.LookRotation(-normal);
            GameObject splat = Instantiate(paintSplatPrefab, splatPos, splatRot);
            Destroy(splat, splatLifeTime);
        }
    }

    private IEnumerator ShowBulletTrail(Vector3 start, Vector3 end)
    {
        if (bulletTrail == null) yield break;
        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, start);
        bulletTrail.SetPosition(1, end);
        yield return new WaitForSeconds(0.05f);
        bulletTrail.enabled = false;
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading.Value = true;
        yield return new WaitForSeconds(reloadTime);

        int needed = maxAmmo - currentAmmo.Value;
        if (totalReserve.Value >= needed)
        {
            totalReserve.Value -= needed;
            currentAmmo.Value = maxAmmo;
        }
        else
        {
            currentAmmo.Value += totalReserve.Value;
            totalReserve.Value = 0;
        }
        isReloading.Value = false;
    }

    public void ResetAmmoOnRespawn()
    {
        if (IsServer)
        {
            currentAmmo.Value = maxAmmo;
            totalReserve.Value = 36;
            isReloading.Value = false;
        }
    }
}