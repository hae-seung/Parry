using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class VerifyPassThrough {
static void Check(bool p,string s){if(!p)throw new Exception(s);}
static GameObject Box(Scene scene,string name,int layer,Vector2 position,Vector2 size,bool dynamic){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);go.layer=layer;go.transform.position=position;go.AddComponent<BoxCollider2D>().size=size;if(dynamic){var body=go.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;}return go;}
public static async Task<string> Run(){
var scene=SceneManager.CreateScene("TemporaryPassThrough",new CreateSceneParameters(LocalPhysicsMode.Physics2D));
try {
int playerLayer=LayerMask.NameToLayer("Player"),monsterLayer=LayerMask.NameToLayer("Monster");
Check(!Physics2D.GetIgnoreLayerCollision(playerLayer,monsterLayer),"Project pair still ignored");Check(!Physics2D.GetIgnoreLayerCollision(playerLayer,0),"Player/world disabled");Check(!Physics2D.GetIgnoreLayerCollision(monsterLayer,0),"Monster/world disabled");
var physics=scene.GetPhysicsScene2D();var player=Box(scene,"Player",playerLayer,new Vector2(-2,0),Vector2.one,true);var monster=Box(scene,"Monster",monsterLayer,Vector2.zero,Vector2.one,true);var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"); var exclusion=prefab.GetComponentInChildren<Collider2D>().excludeLayers; Check((exclusion.value & (1<<monsterLayer))!=0,"Prefab exclusion missing"); player.GetComponent<Collider2D>().excludeLayers=exclusion; var pb=player.GetComponent<Rigidbody2D>();var mb=monster.GetComponent<Rigidbody2D>();pb.linearVelocity=new Vector2(2,0);
for(int i=0;i<100;i++)physics.Simulate(.02f);
Check(pb.position.x>1.8f,"Player blocked by Monster");Check(Mathf.Abs(mb.position.x)<.01f,"Monster pushed");
Box(scene,"Floor",0,new Vector2(0,-2),new Vector2(20,1),false);pb.position=new Vector2(0,3);pb.linearVelocity=Vector2.zero;pb.gravityScale=1;
for(int i=0;i<150;i++)physics.Simulate(.02f);
Check(pb.position.y<-.8f && pb.position.y>-1.1f,"Player stood on Monster or fell through floor");Check(Mathf.Abs(mb.position.y)<.01f,"Monster pushed vertically");
var filter=new ContactFilter2D();filter.SetLayerMask(1<<monsterLayer);filter.useTriggers=true;var results=new Collider2D[4];int count=physics.OverlapBox(Vector2.zero,new Vector2(2,2),0,filter,results);Check(count>0 && results[0].gameObject==monster,"Attack overlap cannot find Monster");
return "PASS: horizontal pass-through, no pushing, fall through Monster onto floor, world contacts retained, Monster attack overlap retained";
}finally{await SceneManager.UnloadSceneAsync(scene);}
}}

