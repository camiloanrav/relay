using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Player2DController : NetworkBehaviour
{
    private Rigidbody2D body;
    private Animator anim;
    private Animator m_NetworkAnimator;
    private float speed = 10;
    private bool grounded;

    void Awake(){
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        if(IsOwner)
        {
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
        body.velocity = new Vector2(body.velocity.x, speed);
        anim.SetTrigger("jump");
        grounded = false;
    }

    private void OnCollisionEnter2D(Collision2D collision){
        if(collision.gameObject.tag == "Ground"){
            grounded = true;
        }
    }
}
