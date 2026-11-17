using System.Collections;
using UnityEngine;
using Parry.PlayerClass;
using Parry.MonsterClass;

public static class VerifyStage01Traversal
{
    public static object Run()
    {
        if(!Application.isPlaying) throw new System.Exception("Play Stage01 first");
        var player=Object.FindAnyObjectByType<PlayerCharacter>();
        player.transform.position=new Vector3(3,0,0);
        player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        foreach(var monster in Object.FindObjectsByType<Monster>()) monster.gameObject.SetActive(false);
        player.StartCoroutine(Walk(player));
        return new {started=true,targetX=108};
    }

    private static IEnumerator Walk(PlayerCharacter player)
    {
        var movement=player.GetComponent<PlayerMovement>();
        var collider=player.GetComponent<Collider2D>();
        float end=Time.realtimeSinceStartup+35;
        while(player.transform.position.x<108 && Time.realtimeSinceStartup<end)
        {
            movement.SendMessage("Move",1f);
            var bounds=collider.bounds;
            var low=new Vector2(bounds.max.x+.02f,bounds.min.y+.1f);
            var high=new Vector2(bounds.max.x+.02f,bounds.max.y-.1f);
            if(player.IsGrounded && (Physics2D.Raycast(low,Vector2.right,.9f,1)||Physics2D.Raycast(high,Vector2.right,.9f,1)))
                movement.SendMessage("Jump");
            yield return null;
        }
        movement.SendMessage("Move",(object)0f);
        if(player.transform.position.x>=108)
            Debug.Log("[Stage01Verification] PASS: walked/jumped to arena at "+player.transform.position);
        else
            Debug.LogError("[Stage01Verification] FAILED: blocked at "+player.transform.position);
    }
}
