using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
public static class VerifyMonsterPlatform {
public static async Task<string> Run(){
var root=new GameObject("TemporaryPlatformRepro");var log=new StringBuilder();
try {
var p=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"),root.transform);p.transform.position=new Vector3(4,1000,0);p.GetComponent<PlayerMovement>().enabled=false;p.GetComponent<Rigidbody2D>().gravityScale=0;p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;p.GetComponent<Collider2D>().isTrigger=true;
var go=new GameObject("GoblinProbe");go.tag="Monster";go.transform.SetParent(root.transform);go.SetActive(false);var body=go.AddComponent<Rigidbody2D>();body.freezeRotation=true;body.gravityScale=0;go.AddComponent<BoxCollider2D>();go.AddComponent<SpriteRenderer>();var anim=go.AddComponent<Animator>();anim.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Monster/MonsterController.controller");go.AddComponent<Monster>();var move=go.AddComponent<MonsterMovement>();var sensor=go.AddComponent<MonsterSensor>();var so=new SerializedObject(sensor);so.FindProperty("target").objectReferenceValue=p.GetComponent<PlayerCharacter>();so.ApplyModifiedPropertiesWithoutUndo();var brain=go.AddComponent<MonsterBrain>();var bso=new SerializedObject(brain);bso.FindProperty("data").objectReferenceValue=AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Monster/Goblin.asset");bso.ApplyModifiedPropertiesWithoutUndo();go.transform.position=new Vector3(0,1000,0);go.SetActive(true);Physics2D.SyncTransforms();var feet=p.GetComponent<Collider2D>().bounds.min.y;go.transform.position=new Vector3(0,feet+.5f,0);Physics2D.SyncTransforms();
await Task.Delay(250);
int frame=Time.frameCount; p.transform.position=new Vector3(body.position.x-.5f,1000,0);Physics2D.SyncTransforms();
while(Time.frameCount<=frame+1)await Task.Delay(1);
if(!sensor.IsTargetVisible || move.FacingDirection!=-1)throw new Exception("Did not face left within two frames");
log.AppendLine("[PlatformProbe] direction flipped left within two frames, including inside stop distance");
p.transform.position=new Vector3(4,1000,0);Physics2D.SyncTransforms(); await Task.Delay(150);
var platform=new GameObject("RaisedPlatform");platform.transform.SetParent(root.transform);platform.transform.position=new Vector3(2,feet+1.5f,0);platform.AddComponent<BoxCollider2D>().size=new Vector2(1,3);p.transform.position+=new Vector3(-2,3,0);Physics2D.SyncTransforms();
var flags=BindingFlags.Instance|BindingFlags.NonPublic;
for(int i=0;i<6;i++){await Task.Delay(200);log.AppendLine("[PlatformProbe] visible="+sensor.IsTargetVisible+" lastSeen="+sensor.LastSeenPosition+" target="+p.transform.position+" position="+body.position+" velocity="+body.linearVelocity+" destination="+typeof(MonsterMovement).GetField("destinationX",flags).GetValue(move)+" animSpeed="+anim.GetFloat("Speed"));}
float x=body.position.x;await Task.Delay(150);float displacement=Mathf.Abs(body.position.x-x);float speed=anim.GetFloat("Speed");
if(displacement<.01f && speed>.01f)throw new Exception("Stationary monster still in Walk.\n"+log);
await Task.Delay(3100); if(!(brain.State is MonsterWanderState))throw new Exception("Raised-platform sight loss never ended combat");
return "PASS: stationary monster leaves Walk; prolonged sight loss ends combat and enters Wander.\n"+log;
}finally{UnityEngine.Object.DestroyImmediate(root);}
}}



