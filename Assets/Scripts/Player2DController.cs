using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class Player2DController : NetworkBehaviour
{
    public Rigidbody2D _projectile;
    public Transform _spawner;
    private Text LifeLabel;
    public NetworkVariable<int> _netLife = new NetworkVariable<int>(
        default,
        NetworkVariableBase.DefaultReadPerm, // Everyone
        NetworkVariableWritePermission.Owner);
    private Rigidbody2D body;
    private Animator anim;
    private Animator m_NetworkAnimator;
    public float speed = 10;
    public float jumpSpeed = 10;
    public int velocityEffectCtr = 0;
    private bool grounded;
    private GameObject playerInfo;

    public override void OnNetworkSpawn(){
        _netLife.OnValueChanged += OnLifeChanged;

        if (IsOwner){
            _netLife.Value = 100;
            GameObject.Find("Main Camera").GetComponent<CameraController>().player = transform;
        }

    }

    public override void OnNetworkDespawn()
    {
        _netLife.OnValueChanged -= OnLifeChanged;
    }

    void Awake(){
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        LifeLabel = GameObject.Find("LifeLabel").GetComponent<Text>();

        playerInfo = new GameObject("Child");
        playerInfo.transform.SetParent(GameObject.Find("Players").transform);
        playerInfo.AddComponent<Text>().text = "";
        playerInfo.GetComponent<Text>().font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
    }

    void FixedUpdate(){
        if(!IsOwner)
            return;

        if(IsOwner){
            float horizontalInput = Input.GetAxis("Horizontal");
            body.velocity = new Vector2(horizontalInput * speed, body.velocity.y);

            if(horizontalInput > 0.01f){
                transform.localScale = Vector3.one;
            }else if(horizontalInput < -0.01f){
                transform.localScale = new Vector3(-1, 1, 1);
            }

            if(Input.GetKey(KeyCode.Space) && grounded)
                Jump();


            // Set animator parameters
            anim.SetBool("run", horizontalInput != 0);
            anim.SetBool("grounded", grounded);
        }
    }

    private void Jump(){
        body.velocity = new Vector2(body.velocity.x, jumpSpeed);
        anim.SetTrigger("jump");
        grounded = false;
    }

    private void OnTriggerEnter2D(Collider2D collision){
        if(collision.gameObject.tag == "Fire"){
            if(IsOwner){
                Debug.Log("Fire IsOwner");
                _netLife.Value -= 10;
            } 
        }
    }

    private void OnCollisionEnter2D(Collision2D collision){
        if(collision.gameObject.tag == "Ground"){
            grounded = true;
        }
    }

    public void OnLifeChanged(int previous, int current)
    {
        if(current != 100){
            Debug.Log("Player " + NetworkObjectId + " was damaged by " + (previous - current));
        }
        Text t = playerInfo.GetComponent<Text>();
        t.text = "Life Player " + NetworkObjectId + ": " + current.ToString();
        if(IsOwner){
            LifeLabel.text = "Your life :" + (_netLife.Value).ToString();
        } 
    }
}
