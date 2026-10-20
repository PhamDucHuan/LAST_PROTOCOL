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
    public sealed class CanThoNeighborhoodTests
    {
        [Test]
        public void ServiceAlleysAndFoodCourtConnectToCampusWithoutLongDetours()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.artRevision, Is.InRange(6, 10));
            Assert.That(layout.neighborhoodHouses.Length, Is.GreaterThan(100));
            Assert.That(layout.neighborhoodChecks.Length, Is.GreaterThan(20));
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0,0,-40), out var start, .5f, NavMesh.AllAreas));
                foreach (var point in layout.neighborhoodChecks)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(point, out var target, .5f, NavMesh.AllAreas), point.ToString());
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path));
                    Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,point.ToString());
                    float length=0;
                    for (int i=1;i<path.corners.Length;i++) length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                    Assert.Less(length,Vector3.Distance(start.position,target.position)*1.7f+20,point.ToString());
                }
                foreach (var house in layout.neighborhoodHouses)
                    Assert.IsFalse(NavMesh.SamplePosition(house.position,out _,.5f,NavMesh.AllAreas),house.name+" interior");
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void EveryNewBuildingHasCollisionAndGroundAndFitsWorldBounds()
        {
            WithCampus(scene =>
            {
                var layout=CanThoMapSetupTool.ReadLayout();
                var bounds=new Bounds(layout.bounds.center,layout.bounds.size);
                var colliders=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<BoxCollider>()).ToArray();
                var ground=colliders.Single(x=>x.name=="Campus soil");
                foreach (var house in layout.neighborhoodHouses)
                {
                    var collider=colliders.Single(x=>x.name==house.name);
                    Assert.That(Vector3.Distance(collider.transform.position,house.position+Vector3.up*house.size.y*.5f),Is.LessThan(.001f));
                    Assert.AreEqual(house.size,collider.size);
                    Assert.Less(Quaternion.Angle(collider.transform.rotation,Quaternion.Euler(house.euler)),.01f,house.name);
                    Assert.IsTrue(bounds.Contains(house.position),house.name);
                    Assert.IsTrue(ground.Raycast(new Ray(house.position+Vector3.up*2,Vector3.down),out _,3),house.name);
                }
                Assert.That(ground.bounds.min.z,Is.LessThan(-300));
                Assert.That(colliders.Single(x=>x.name=="South retaining wall").transform.position.z,Is.LessThan(-300));
                Assert.AreEqual(1,scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LevelEnvironment>()).Count());
            });
        }

        [Test]
        public void PlayerCanWalkOutOfGateAroundHousingBlocksAndBackToCampus()
        {
            WithCampus(scene =>
            {
                var template=AssetDatabase.LoadAssetAtPath<GameObject>(MultiplayerSetupTool.PlayerPath).GetComponent<CharacterController>();
                var route=CanThoMapSetupTool.ReadLayout().neighborhoodRoute;
                var go=new GameObject("Neighborhood walking probe");
                try
                {
                    go.transform.position=route[0]+Vector3.up*(template.height*.5f-template.center.y+.05f);
                    var controller=go.AddComponent<CharacterController>();
                    controller.height=template.height;controller.radius=template.radius;controller.center=template.center;
                    controller.stepOffset=template.stepOffset;controller.skinWidth=template.skinWidth;
                    controller.slopeLimit=template.slopeLimit;controller.minMoveDistance=template.minMoveDistance;
                    foreach(var point in route.Skip(1))
                    {
                        for(int step=0;step<1800;step++)
                        {
                            var direction=point-go.transform.position;direction.y=0;
                            if(direction.magnitude<.12f)break;
                            controller.Move(Vector3.ClampMagnitude(direction,.12f)+Vector3.down*.1f);
                        }
                        var remaining=point-go.transform.position;remaining.y=0;
                        Assert.Less(remaining.magnitude,.2f,"Stuck before "+point+" actual="+go.transform.position);
                        Assert.That(go.transform.position.y+controller.center.y-controller.height*.5f,Is.InRange(-.15f,.5f));
                    }
                }
                finally { Object.DestroyImmediate(go); }
            });
        }

        private static void WithCampus(System.Action<Scene> check)
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Additive);
            try { Physics.SyncTransforms();check(scene); }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous); }
        }
    }
}
