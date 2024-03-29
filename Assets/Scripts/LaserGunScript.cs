using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LaserGunScript : MonoBehaviourPunCallbacks
{
    public TrailRenderer laserTrailPrefab;
    public ParticleSystem hitParticleSystem;
    public Transform originTransform;
    public Animator animator;
    [Tooltip("The amount of damage in one hit")]
    public int damage = 35;
    public AudioSource audioSource;
    public AudioClip damageHitMarkerAudioClip;
    public AudioClip laserSoundAudioClip;
    private float fireRate = 0.2f;
    private float fireTimer = 0f;

    //the layer to ignore when casting rays- ie what team are we on!
    [Tooltip("The layer to ignore when casting rays- ie what team are we on!")]
    public LayerMask layerMask;

    void Start()
    {
        //set the layermask to ignore the layer we are on
        layerMask = ~(1 << gameObject.layer);
    }



    // Update is called once per frame
    void Update()
    {
        fireTimer += Time.deltaTime;
        if(Input.GetButton("Fire1") && photonView.IsMine && fireTimer > fireRate)
        {
            //update timer
            fireTimer = 0f;
            //wo am I?
            int userid = PhotonNetwork.LocalPlayer.ActorNumber;
            //run the rpc so even the clone shoots
            //photonView.RPC("Shoot", RpcTarget.All, userid);
            photonView.RPC("ShootVisuals", RpcTarget.All);
            //now check hit- client side hit detection
            ShootCheckHit(userid);
        }
    }



    [PunRPC]
    public void ShootVisuals()
    {
        //play the shoot animation
        animator.SetTrigger("Shoot");
        //playe the sound
        audioSource.PlayOneShot(laserSoundAudioClip);
        var trail = Instantiate(laserTrailPrefab, originTransform.position, originTransform.rotation);
        trail.AddPosition(originTransform.position);
        //cast forward to see where we hit
        var ray = new Ray(originTransform.position, originTransform.forward);
        Vector3 hitPoint = originTransform.position + originTransform.forward * 1000f;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f,layerMask))
        {
            hitPoint = hit.point;
            Instantiate(hitParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));
        }
        trail.transform.position = hitPoint;
    }


    //local hit detection- see if a ray from the local player hits a clone
    //if it does, make the clone run an RPC for taking damage/dieing etc.
    public void ShootCheckHit(int shooterActorNumber)
    {
        //cast forward to see where we hit
        var ray = new Ray(originTransform.position, originTransform.forward);
        Vector3 hitPoint = originTransform.position + originTransform.forward * 1000f;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
        {
            hitPoint = hit.point;
            Instantiate(hitParticleSystem, hit.point, Quaternion.LookRotation(hit.normal));
            //check what we hit and if we are we are the local player do damage to the player we hit
            var playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null && photonView.IsMine)
            {
                Debug.Log($"Player {photonView} is damaging player {hit.collider.GetComponent<PhotonView>()}");
                //call the rpc on the clone's photon view. It gets synced across the network.
                //Client side hit detection!! Very dodgy and easy to hack. ust like roblox!
                playerHealth.photonView.RPC("TakeDamageRPC", RpcTarget.All, damage, shooterActorNumber);
                audioSource.PlayOneShot(damageHitMarkerAudioClip);
            }
        }
    }


}
