using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GunSpawner : MonoBehaviourPunCallbacks
{
    public static GunSpawner instance;
    public List<Transform> guns;
    public List<Transform> items;
    public List<AmmoPickup> ammo;
    public List<AmmoPickup> deathAmmo;
    public List<PhotonRoyaleChest> chests;
    public List<Transform> spawnPositions;

    public List<Transform> randomSpawn;
    public Mesh gizmoMesh;
    public Vector3 gizmoScale = new Vector3(200, 100, 100);

    public int minGuns = 40;
    public int maxGuns = 60;
    public int minAmmo = 40;
    public int maxAmmo = 40;
    public int minAmmoVal = 10;
    public int maxAmmoVal = 40;
    public float itemChance = .25f;
    public float spawnSpread = 1.0f;
    public float minWait = 10.0f;
    float nextWait = 0.0f;

    public bool shouldSetup = true;

    public void Start()
    {
        nextWait = Time.time + minWait;

        PhotonExplodingBullet.bullets.Clear();
        for (int i = 0; i < guns.Count; i++)
        {
            PhotonGun gun = guns[i].GetComponent<PhotonGun>();
            if (gun.bulletPool[0].GetComponent<PhotonExplodingBullet>() != null)
            {
                for (int j = 0; j < gun.bulletPool.Count; j++)
                {
                    PhotonExplodingBullet.bullets.Add((PhotonExplodingBullet)gun.bulletPool[j]);
                    ((PhotonExplodingBullet)gun.bulletPool[j]).id = PhotonExplodingBullet.bullets.Count - 1;
                }
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        instance = this;
    }

    public void HandleGameSetup()
    {
        if (photonView.IsMine)
        {
            photonView.RPC("ResetGuns", RpcTarget.All);
            photonView.RPC("ResetChests", RpcTarget.All);
        }
    }

    [PunRPC]
    public void ResetChests()
    {
        shouldSetup = false;
        for (int i = 0; i < chests.Count; i++)
        {
            chests[i].ResetChest();
        }

        if (photonView.IsMine)
        {
            List<Transform> tempSpawn = new List<Transform>();
            for (int i = 0; i < spawnPositions.Count; i++)
            {
                tempSpawn.Add(spawnPositions[i]);
            }
            randomSpawn.Clear();

            int rand;
            for (int i = 0; i < chests.Count; i++)
            {
                if (!chests[i].photonView.IsMine)
                {
                    chests[i].photonView.RequestOwnership();
                }
                rand = Random.Range(0, tempSpawn.Count);

                randomSpawn.Add(tempSpawn[rand]);
                tempSpawn.RemoveAt(rand);

                chests[i].transform.position = randomSpawn[randomSpawn.Count - 1].position;
                chests[i].transform.rotation = randomSpawn[randomSpawn.Count - 1].rotation;
            }
        }
    }

    [PunRPC]
    public void ResetGuns()
    {
        for (int i = 0; i < guns.Count; i++)
        {
            PhotonGun gun = guns[i].gameObject.GetComponent<PhotonGun>();
            gun.gameObject.SetActive(true);
            gun.ResetGun();

            if (!gun.photonView.IsMine && photonView.IsMine)
            {
                gun.photonView.RequestOwnership();
            }
            guns[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < ammo.Count; i++)
        {
            if (!ammo[i].photonView.IsMine && photonView.IsMine)
            {
                ammo[i].photonView.RequestOwnership();
            }
            ammo[i].ammoInside = Random.Range(minAmmoVal, maxAmmoVal);
            ammo[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < deathAmmo.Count; i++)
        {
            if (!deathAmmo[i].photonView.IsMine && photonView.IsMine)
            {
                deathAmmo[i].photonView.RequestOwnership();
            }
            deathAmmo[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (!items[i].GetComponent<PhotonView>().IsMine && photonView.IsMine)
            {
                items[i].GetComponent<PhotonView>().RequestOwnership();
            }

            Grenade grenade = items[i].gameObject.GetComponent<Grenade>();
            PhotonIceShield iceShield = items[i].gameObject.GetComponent<PhotonIceShield>();
            PhotonTorch torch = items[i].gameObject.GetComponent<PhotonTorch>();

            if (grenade != null)
            {
                grenade.gameObject.SetActive(true);
                grenade.ResetGrenade();
            }
            else if (iceShield != null)
            {
                iceShield.gameObject.SetActive(true);
                iceShield.ResetShield();
            }
            else if (torch != null)
            {
                torch.gameObject.SetActive(true);
                torch.ResetTorch();
            }
            items[i].gameObject.SetActive(false);
        }
    }

    [PunRPC]
    public void SyncGunState(bool[] gunsOn, bool[] ammoOn, int[] ammoVal, bool[] deathAmmoOn, bool[] itemsOn)
    {
        Debug.Log("SyncedUp");
        shouldSetup = false;
        for (int i = 0; i < guns.Count; i++)
        {
            guns[i].gameObject.SetActive(gunsOn[i]);
        }

        for (int i = 0; i < ammo.Count; i++)
        {
            ammo[i].ammoInside = ammoVal[i];
            ammo[i].gameObject.SetActive(ammoOn[i]);
        }

        for (int i = 0; i < deathAmmo.Count; i++)
        {
            deathAmmo[i].gameObject.SetActive(deathAmmoOn[i]);
        }

        for (int i = 0; i < items.Count; i++)
        {
            items[i].gameObject.SetActive(itemsOn[i]);
        }
    }
    
    [PunRPC]
    public void MoveDeathAmmo(Vector3 deathAmmoPos, int ammoVal, int ammoCount)
    {
        deathAmmo[ammoVal].gameObject.SetActive(true);
        deathAmmo[ammoVal].ammoInside = ammoCount;
        deathAmmo[ammoVal].transform.position = deathAmmoPos;
    }

    [PunRPC]
    public void SetupGame()
    {
        if (photonView.IsMine)
        {
            HandleGameSetup();
        }
    }

    public int GetOpenGun()
    {
        int rand = Random.Range(0, guns.Count);
        int numTries = 1000;
        while (guns[rand].gameObject.activeSelf || guns[rand].GetComponent<PhotonGun>().held)
        {
            rand = Random.Range(0, guns.Count);
            numTries--;
            if (numTries <= 0)
            {
                rand = -1;
                break;
            }
        }

        if (rand > 1)
        {
            guns[rand].gameObject.SetActive(true);
        }
        return rand;
    }

    public int GetOpenAmmo()
    {
        for (int i = 0; i < ammo.Count; i++)
        {
            if (!ammo[i].gameObject.activeSelf)
            {
                return i;
            }
        }
        return -1;
    }

    public int GetOpenItem()
    {
        if (Random.Range(0f, 1f) > itemChance || NoItemsAvailable())
        {
            return -1;
        }

        int rand = Random.Range(0, items.Count);
        Transform item = items[rand];
        Grenade grenade = item.gameObject.GetComponent<Grenade>();
        PhotonIceShield iceShield = item.gameObject.GetComponent<PhotonIceShield>();
        PhotonTorch torch = item.gameObject.GetComponent<PhotonTorch>();
        while (item.gameObject.activeSelf || (grenade != null && grenade.held) || (iceShield != null && iceShield.held) || (torch != null && torch.held))
        {
            rand = Random.Range(0, items.Count);
            item = items[rand];
            grenade = item.gameObject.GetComponent<Grenade>();
            iceShield = item.gameObject.GetComponent<PhotonIceShield>();
            torch = item.gameObject.GetComponent<PhotonTorch>();
        }

        return rand;
    }

    public bool NoItemsAvailable()
    {
        Grenade grenade;
        PhotonIceShield iceShield;
        PhotonTorch torch;
        for (int i = 0; i < items.Count; i++)
        {
            grenade = items[i].gameObject.GetComponent<Grenade>();
            iceShield = items[i].gameObject.GetComponent<PhotonIceShield>();
            torch = items[i].gameObject.GetComponent<PhotonTorch>();
            if (grenade != null)
            {
                if (!items[i].gameObject.activeSelf && !grenade.held)
                {
                    return false;
                }
            }

            if (iceShield != null)
            {
                if (!items[i].gameObject.activeSelf && !iceShield.held)
                {
                    return false;
                }
            }

            if (torch != null)
            {
                if (!items[i].gameObject.activeSelf && !torch.held)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void SpawnGuns()
    {
        List<Transform> tempSpawn = new List<Transform>();
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            tempSpawn.Add(spawnPositions[i]);
        }
        randomSpawn.Clear();

        int rand;
        int numGuns = Random.Range(minGuns, maxGuns + 1);
        for (int i = 0; i < numGuns; i++)
        {
            rand = Random.Range(0, tempSpawn.Count);
            randomSpawn.Add(tempSpawn[rand]);
            tempSpawn.RemoveAt(rand);
        }

        for (int i = 0; i < guns.Count; i++)
        {
            if (!guns[i].gameObject.GetComponent<PhotonView>().IsMine)
            {
                guns[i].gameObject.GetComponent<PhotonView>().RequestOwnership();
            }
            guns[i].gameObject.SetActive(false);
        }

        while (numGuns > 0)
        {
            guns[numGuns - 1].position = randomSpawn[numGuns - 1].position;
            guns[numGuns - 1].gameObject.SetActive(true);
            numGuns--;
        }

        SpawnAmmo(numGuns);
    }

    public void SpawnAmmo(int startNum)
    {
        int numAmmo = Random.Range(minAmmo, maxAmmo + 1);
        for (int i = 0; i < ammo.Count; i++)
        {
            if (!ammo[i].photonView.IsMine)
            {
                ammo[i].photonView.RequestOwnership();
            }
            ammo[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < deathAmmo.Count; i++)
        {
            if (!deathAmmo[i].photonView.IsMine)
            {
                deathAmmo[i].photonView.RequestOwnership();
            }
            deathAmmo[i].gameObject.SetActive(false);
        }

        while (numAmmo > 0)
        {
            ammo[numAmmo - 1].transform.position = randomSpawn[numAmmo + startNum - 1 ].position;
            ammo[numAmmo - 1].gameObject.SetActive(true);
            ammo[numAmmo - 1].ammoInside = Random.Range(minAmmoVal, maxAmmoVal);
            numAmmo--;
        }
    }

    public Vector3 GetStartSpot()
    {
        return spawnPositions[Random.Range(0, spawnPositions.Count)].position;
    }

    public void Update()
    {
        if (shouldSetup && Time.time > nextWait)
        {
            shouldSetup = false;
            if (photonView.IsMine)
            {
                HandleGameSetup();
            }
        }
    }

    public void OnDrawGizmos()
    {
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Gizmos.DrawWireMesh(gizmoMesh, spawnPositions[i].position, Quaternion.Euler(spawnPositions[i].eulerAngles + Vector3.right * -90.0f), gizmoScale);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        ResetGuns();
        ResetChests();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (photonView.IsMine)
        {
            bool[] gunState = new bool[guns.Count];
            bool[] ammoState = new bool[ammo.Count];
            bool[] deathAmmoState = new bool[deathAmmo.Count];
            bool[] itemState = new bool[items.Count];
            int[] ammoVal = new int[ammo.Count];

            for (int i = 0; i < guns.Count; i++)
            {
                gunState[i] = guns[i].gameObject.activeSelf;
            }

            for (int i = 0; i < ammo.Count; i++)
            {
                ammoVal[i] = ammo[i].ammoInside;
                ammoState[i] = ammo[i].gameObject.activeSelf;
            }

            for (int i = 0; i < deathAmmo.Count; i++)
            {
                deathAmmoState[i] = deathAmmo[i].gameObject.activeSelf;
            }

            photonView.RPC("SyncGunState", newPlayer, gunState, ammoState, ammoVal, deathAmmoState, itemState);
        }
    }
}
