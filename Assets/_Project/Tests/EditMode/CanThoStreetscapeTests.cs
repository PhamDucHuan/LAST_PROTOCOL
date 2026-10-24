using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LastProtocol.Tests
{
    public sealed class CanThoStreetscapeTests
    {
        [Test]
        public void ResidentialLanesConnectToRoadsAndEnemyNavigation()
        {
            var layout=CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.artRevision,Is.InRange(8, 13));
            Assert.GreaterOrEqual(layout.neighborhoodHouses.Length,220);
            Assert.GreaterOrEqual(layout.localLanes.Length,5);
            var nav=NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                foreach(var lane in layout.localLanes)
                {
                    var start=lane.points[0];
                    Assert.IsTrue(layout.peripheralRoads.Any(r=>Enumerable.Range(1,r.points.Length-1).Any(i=>
                    {
                        var d=r.points[i]-r.points[i-1];
                        return Vector3.Distance(start,r.points[i-1]+d*Mathf.Clamp01(Vector3.Dot(start-r.points[i-1],d)/d.sqrMagnitude))<.1f;
                    })),lane.id+" must connect to a main road");
                    for(int i=0;i<=8;i++)
                    {
                        var point=Vector3.Lerp(start,lane.points[1],i/8f);
                        Assert.IsTrue(NavMesh.SamplePosition(point,out var target,.5f,NavMesh.AllAreas),lane.id+" "+point);
                        var path=new NavMeshPath();
                        Assert.IsTrue(NavMesh.CalculatePath(start,target.position,NavMesh.AllAreas,path));
                        Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,lane.id);
                    }
                }
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void PlayerCapsuleCanWalkEveryResidentialLaneInBothDirections()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Additive);
            var go=new GameObject("Residential lane walking probe");
            try
            {
                var template=AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath).GetComponent<CharacterController>();
                var controller=go.AddComponent<CharacterController>();
                controller.height=template.height;controller.radius=template.radius;controller.center=template.center;
                controller.skinWidth=template.skinWidth;controller.stepOffset=template.stepOffset;controller.slopeLimit=template.slopeLimit;
                foreach(var lane in CanThoMapSetupTool.ReadLayout().localLanes)
                {
                    controller.enabled=false;
                    go.transform.position=lane.points[0]+Vector3.up*(template.height*.5f-template.center.y+.05f);
                    controller.enabled=true;Physics.SyncTransforms();
                    foreach(var point in new[]{lane.points[1],lane.points[0]})
                    {
                        for(int step=0;step<1800;step++)
                        {
                            var d=point-go.transform.position;d.y=0;if(d.magnitude<.12f)break;
                            controller.Move(Vector3.ClampMagnitude(d,.12f)+Vector3.down*.1f);
                        }
                        var remaining=point-go.transform.position;remaining.y=0;
                        Assert.Less(remaining.magnitude,.2f,lane.id+" stuck at "+go.transform.position);
                        Assert.That(go.transform.position.y+controller.center.y-controller.height*.5f,Is.InRange(-.15f,.5f));
                    }
                }
            }
            finally { Object.DestroyImmediate(go);EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous); }
        }

        [Test]
        public void StreetTreesBenchesAndLampsHavePhysicalCollision()
        {
            var layout=CanThoMapSetupTool.ReadLayout();
            Assert.GreaterOrEqual(layout.streetscapeTrees.Length,60);Assert.GreaterOrEqual(layout.streetscapeBenches.Length,12);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Additive);
            try
            {
                var boxes=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<BoxCollider>()).ToArray();
                foreach(var pair in new[]{(layout.streetscapeTrees,"Peripheral shade tree trunk"),(layout.streetscapeBenches,"Shore bench seat"),(layout.streetscapeLamps,"Streetscape solar lamp")})
                    foreach(var point in pair.Item1)
                        Assert.IsTrue(boxes.Any(c=>c.name==pair.Item2&&Vector2.Distance(new Vector2(c.transform.position.x,c.transform.position.z),new Vector2(point.x,point.z))<.01f),pair.Item2+" "+point);
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous); }
        }
    }
}
