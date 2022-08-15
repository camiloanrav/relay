using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerAttack : NetworkBehaviour
{
    private float attackCoolDown = 1f;
    public Transform firePoint;
    private List<GameObject> fireBalls = new List<GameObject>();
    public GameObject fireBall;
    private Animator anim;
    private Player2DController player2DController;
    private float coolDownTimer = Mathf.Infinity;
    // Start is called before the first frame update
    void Awake()
    {
        anim = GetComponent<Animator>();
        player2DController = GetComponent<Player2DController>();
        var fbs = new GameObject("fireBalls" + NetworkObjectId);
        for(int i = 0; 10 > i; i++){
            GameObject projectile = Instantiate(fireBall, firePoint.position, Quaternion.identity);
            projectile.transform.SetParent(fbs.transform);
            fireBalls.Add(projectile);
            projectile.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(IsOwner && Input.GetMouseButton(0) && coolDownTimer > attackCoolDown){
            RequestFireServerRpc();
        }

        coolDownTimer += Time.deltaTime;
    }

    [ServerRpc]
    private void RequestFireServerRpc() {
        FireClientRpc();
    }

    [ClientRpc]
    private void FireClientRpc() {
        Attack();
    }

    private void Attack(){
        coolDownTimer = 0;
        anim.SetTrigger("attack");

        // Pool fireBalls
        /* var projectile = Instantiate(fireBall, firePoint.position, Quaternion.identity);
        projectile.GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x)); */
        int index = FindFireball();
        fireBalls[index].transform.position = firePoint.position;
        fireBalls[index].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
    }

    private int FindFireball(){
        for(int i = 0; fireBalls.Count > i; i++){
            if(!fireBalls[i].activeInHierarchy)
                return i;
        }
        return 0;
    }
}
