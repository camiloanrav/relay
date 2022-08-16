using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fast : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision){
        if(collision.gameObject.tag == "Player"){
            collision.gameObject.GetComponent<Player2DController>().speed = 25;
            collision.gameObject.GetComponent<Player2DController>().velocityEffectCtr ++;
            StartCoroutine(ResetVelocity(3, collision.gameObject));
        }
    }

    IEnumerator ResetVelocity(float waitTime, GameObject player){
        yield return new WaitForSeconds(waitTime);
        player.GetComponent<Player2DController>().velocityEffectCtr --;
        if(player.GetComponent<Player2DController>().velocityEffectCtr == 0){
            player.GetComponent<Player2DController>().speed = 10;
        }
    }
}
