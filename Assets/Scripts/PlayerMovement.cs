using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPunCallbacks, IPunObservable
{

    CharacterController characterController;
    float speed = 5f;
    Vector3 move = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    bool jump = false;
    bool jumping = false;
    bool grounded = false;
    float pitch = 0f;
    float gravity = -15f;
    public Transform cameraPivot;
    public GameObject playerCamera;

    // Start is called before the first frame update
    public override void OnEnable()
    {
        if (photonView.IsMine)
        {
            //Debug.Log("We are the local Player");
            characterController = GetComponent<CharacterController>();
        }
        else
        {
            playerCamera.SetActive(false);
            //Debug.Log("This is a clone loading in");
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            //check grounded
            grounded = characterController.isGrounded;
            if(grounded && velocity.y < 0)
            {
                velocity.y = gravity * Time.deltaTime;
            }
            
            //get input
            transform.Rotate(0, Input.GetAxis("Mouse X") * 3f, 0);
            pitch -= Input.GetAxis("Mouse Y") * 3f;
            pitch = Mathf.Clamp(pitch, -60f, 60f);

            move.x = Input.GetAxisRaw("Horizontal");
            move.z = Input.GetAxisRaw("Vertical");
            move = Vector3.ClampMagnitude(move, 1f);

            var new_velocity = transform.TransformVector(move) * speed;
            velocity.x = new_velocity.x;
            velocity.z = new_velocity.z;

            //actually move
            characterController.Move(velocity * Time.deltaTime);
            
            //if we are grounded
            if (grounded && Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("Jump");
                velocity.y = 10f;
            }
            //apply gravity
            velocity.y += gravity * Time.deltaTime;


        }
        //this will get done regardless of whether a client or a clone
        cameraPivot.localEulerAngles = new Vector3(pitch, 0, 0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //this is the local client
            stream.SendNext(pitch);
        }
        else
        {
            //this is the clone
            pitch = (float)stream.ReceiveNext();
        }
    }
}
