using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Parry.PlayerClass;
using Parry.MonsterClass;
public static class VerifyMonsterBrain {
private sealed class InjectedState : IMonsterState {
public int Enters, Ticks, Exits;
public void Enter(){Enters++;}
public void Tick(){Ticks++;}
public void Exit(){Exits++;}
}
static void Assert(bool pass,string message){if(!pass)throw new Exception(message);}
public static async Task<string> Run(){
var machine=new MonsterStateMachine(); var first=new InjectedState(); var second=new InjectedState();
machine.ChangeState(first); machine.Tick(); machine.ChangeState(first);
Assert(first.Enters==1 && first.Ticks==1 && first.Exits==0,"Same-state transition restarted state");
machine.ChangeState(second); Assert(first.Exits==1 && second.Enters==1,"Enter/Exit order failed");
machine.Clear(); Assert(second.Exits==1 && machine.CurrentState==null,"Clear did not exit state");
var root=new GameObject("TemporaryMonsterAITest"); MonsterData runtimeData=null;
try {
var p=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"),root.transform); p.transform.position=new Vector3(3,1000,0); p.GetComponent<PlayerMovement>().enabled=false; p.GetComponent<Rigidbody2D>().gravityScale=0; p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; p.GetComponent<Collider2D>().isTrigger=true;
var go=new GameObject("TestGoblin"); go.tag="Monster"; go.layer=LayerMask.NameToLayer("Monster"); go.transform.SetParent(root.transform); go.SetActive(false); go.transform.position=new Vector3(0,1000,0);
var body=go.AddComponent<Rigidbody2D>();body.gravityScale=0;body.freezeRotation=true;
var collider=go.AddComponent<BoxCollider2D>(); go.AddComponent<SpriteRenderer>(); var anim=go.AddComponent<Animator>(); anim.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Monster/MonsterController.controller");
go.AddComponent<Monster>(); var movement=go.AddComponent<MonsterMovement>();var sensor=go.AddComponent<MonsterSensor>();
var data=new SerializedObject(sensor);data.FindProperty("target").objectReferenceValue=p.GetComponent<PlayerCharacter>();data.ApplyModifiedPropertiesWithoutUndo();
var brain=go.AddComponent<MonsterBrain>(); var brainData=new SerializedObject(brain); runtimeData=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Monster/Goblin.asset")); var config=new SerializedObject(runtimeData); config.FindProperty("combat").FindPropertyRelative("sightLossDuration").floatValue=.6f; config.FindProperty("wander").FindPropertyRelative("waitDuration").floatValue=.05f; config.ApplyModifiedPropertiesWithoutUndo(); brainData.FindProperty("data").objectReferenceValue=runtimeData; brainData.ApplyModifiedPropertiesWithoutUndo(); go.SetActive(true);Physics2D.SyncTransforms();
float feet=p.GetComponent<Collider2D>().bounds.min.y; go.transform.position=new Vector3(0,feet+.5f,0); Physics2D.SyncTransforms();
var floor=new GameObject("Floor"); floor.transform.SetParent(root.transform); floor.transform.position=new Vector3(20,feet-.5f,0); floor.AddComponent<BoxCollider2D>().size=new Vector2(100,1); Physics2D.SyncTransforms();
brain.enabled=false; await Task.Delay(150); Assert(sensor.IsTargetVisible,"Sensor did not update independently of Brain"); Assert(sensor.LastSeenTime>float.NegativeInfinity,"Last seen time not recorded"); brain.enabled=true;
float lastSeenBeforeWall=sensor.LastSeenTime; var wall=new GameObject("Wall");wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(1.5f,1000,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,10);Physics2D.SyncTransforms();await Task.Delay(150); Assert(!sensor.IsTargetVisible,"Wall did not block sight"); Assert(sensor.LastSeenTime==lastSeenBeforeWall,"Occluded target updated last seen time");wall.SetActive(false);
p.transform.position+=Vector3.up*3;Physics2D.SyncTransforms();await Task.Delay(150); Assert(!sensor.IsTargetVisible,"Different platform height detected");p.transform.position-=Vector3.up*3;Physics2D.SyncTransforms();
await Task.Delay(300);Assert(brain.State is MonsterChaseState,"Did not enter Chase"); Assert(body.position.x>.1f,"Did not move toward target");
var savedPosition=body.position;body.position=new Vector2(200,body.position.y);Physics2D.SyncTransforms();Assert(!movement.CanWalkTowards(201),"Wander did not reject a ledge");body.position=savedPosition;
var blocker=new GameObject("WanderWall");blocker.transform.SetParent(root.transform);blocker.transform.position=new Vector3(body.position.x+.65f,body.position.y,0);blocker.AddComponent<BoxCollider2D>().size=new Vector2(.2f,2);Physics2D.SyncTransforms();Assert(!movement.CanWalkTowards(body.position.x+2),"Wander did not reject wall");var wallCollider=blocker.GetComponent<BoxCollider2D>();wallCollider.size=new Vector2(.2f,.1f);blocker.transform.position=new Vector3(body.position.x+.65f,body.position.y+.4f,0);Physics2D.SyncTransforms();Assert(!movement.CanWalkTowards(body.position.x+2),"Upper obstacle was missed");
blocker.transform.position=new Vector3(body.position.x+.65f,body.position.y-.4f,0);Physics2D.SyncTransforms();Assert(!movement.CanWalkTowards(body.position.x+2),"Low obstacle was missed");wallCollider.isTrigger=true;Physics2D.SyncTransforms();Assert(movement.CanWalkTowards(body.position.x+2),"Trigger blocked walking");Assert(movement.CanWalkTowards(body.position.x-2),"Clear left path rejected");UnityEngine.Object.DestroyImmediate(blocker);
var originalY=p.transform.position.y;
// Combat continues far from spawn if the player remains nearby.
body.position=new Vector2(30,body.position.y);p.transform.position=new Vector3(33,originalY,0);Physics2D.SyncTransforms();await Task.Delay(150);Assert(brain.State is MonsterChaseState,"Spawn distance still ends combat");
p.transform.position=new Vector3(50,originalY,0);Physics2D.SyncTransforms();await Task.Delay(50);Assert(brain.State is MonsterWanderState,"Distance exit did not enter Wander");
float combatEndX=body.position.x;float firstWanderX=combatEndX;await Task.Delay(700); Assert(brain.State is MonsterWanderState && Mathf.Abs(body.position.x-combatEndX)<=2.1f && body.position.x>25,"Wander returned to spawn or escaped local area");Assert(Mathf.Abs(body.position.x-firstWanderX)>.01f,"Wander never moved on clear floor");
p.transform.position=new Vector3(body.position.x+3,originalY,0);Physics2D.SyncTransforms();await Task.Delay(100);Assert(brain.State is MonsterChaseState,"Wander did not reacquire");
p.transform.position+=Vector3.up*3;Physics2D.SyncTransforms();await Task.Delay(150);Assert(brain.State is MonsterChaseState,"Short sight loss ended combat immediately");
await Task.Delay(600);Assert(brain.State is MonsterWanderState,"Sustained height-based sight loss did not end combat");Assert(anim.GetFloat("Speed")<.01f || Mathf.Abs(body.linearVelocity.x)>.01f,"Stationary Wander still shows Walk");
p.transform.position=new Vector3(body.position.x+3,originalY,0);Physics2D.SyncTransforms();await Task.Delay(100);Assert(brain.State is MonsterChaseState,"Did not reacquire after sight-loss combat exit");var injected=new InjectedState(); brain.ChangeState(injected); await Task.Delay(180); Assert(ReferenceEquals(brain.State,injected) && injected.Enters==1 && injected.Ticks>0,"Injected state not executed");
go.SetActive(false); Assert(injected.Exits==1 && brain.State==null,"Disable did not exit injected state"); go.SetActive(true); Assert(brain.State is MonsterWanderState,"Reenable did not enter Wander");
var monster=go.GetComponent<Monster>(); var beforeDeath=new InjectedState(); brain.ChangeState(beforeDeath);
monster.TakeDamage(1); Assert(ReferenceEquals(brain.State,beforeDeath),"Nonlethal hit changed state");
monster.TakeDamage(99999); Assert(!go.activeSelf && beforeDeath.Exits==1 && brain.State==null,"Death did not disable and clear state");
var idleAfterDeath=new InjectedState(); brain.ChangeState(idleAfterDeath);
typeof(MonsterBrain).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(brain,null);
Assert(idleAfterDeath.Ticks==0,"Dead Brain ticked state");return "PASS: autonomous sensor with Brain disabled, cached last-seen retention, injected state lifecycle, same-state no-op, disable/reenable, same-floor detection, wall occlusion, height exclusion, Chase movement, no spawn tether, distance combat exit, local wander movement, wall/ledge avoidance, timed sight-loss combat exit, reacquisition, death disable/cleanup, dead Brain skips state Tick";
}finally{UnityEngine.Object.DestroyImmediate(root); if(runtimeData!=null)UnityEngine.Object.DestroyImmediate(runtimeData);}
}}


















