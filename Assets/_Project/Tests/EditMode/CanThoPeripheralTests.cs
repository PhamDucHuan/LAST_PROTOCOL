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
    public sealed class CanThoPeripheralTests
    {
        [Test]
        public void ReferenceRoadsFormConnectedJunctionsAndHousingFacesDifferentStreets()
        {
            var layout=CanThoMapSetupTool.ReadLayout();Assert.That(layout.artRevision,Is.InRange(7,9));
            var roads=layout.peripheralRoads.ToDictionary(x=>x.id);
            CollectionAssert.AreEquivalent(new[]{"three_february","bung_xang","mac_thien_tich","tran_van_hoai","mau_than","nguyen_van_cu"},roads.Keys);
            float Distance(Vector3 point,CanThoMapSetupTool.PeripheralRoad road)
            {
                float best=float.MaxValue;
                for(int i=1;i<road.points.Length;i++)
                {
                    var a=road.points[i-1];var d=road.points[i]-a;
                    var t=Mathf.Clamp01(Vector3.Dot(point-a,d)/d.sqrMagnitude);
                    best=Mathf.Min(best,Vector3.Distance(point,a+d*t));
                }
                return best;
            }
            foreach(var id in new[]{"bung_xang","mac_thien_tich","tran_van_hoai"})
                Assert.Less(Distance(roads[id].points[0],roads["three_february"]),.05f,id+" begins at 3/2");
            Assert.Less(Distance(roads["mac_thien_tich"].points.Last(),roads["mau_than"]),.05f);
            Assert.Less(Distance(roads["tran_van_hoai"].points.Last(),roads["mau_than"]),.05f);
            Assert.Less(Distance(roads["bung_xang"].points.Last(),roads["nguyen_van_cu"]),.05f);
            Assert.Less(Distance(roads["mau_than"].points[0],roads["nguyen_van_cu"]),.05f);
            Assert.Greater(roads["bung_xang"].points.Length,8,"Shore route should bend rather than be a generic straight alley");
            Assert.That(layout.neighborhoodHouses.Count(x=>Mathf.Abs(Mathf.DeltaAngle(0,x.euler.y))>15),Is.GreaterThan(50));
            CollectionAssert.AreEquivalent(roads.Keys,layout.neighborhoodHouses.Select(x=>x.district).Distinct());
            Assert.Less(layout.waterBoundary.Average(x=>x.x),layout.campusBoundary.Min(x=>x.x),"Waterfront belongs on the west side of the retained campus");
        }

        [Test]
        public void LakeInteriorIsExcludedFromEnemyNavigationAndHasVisibleShoreProtection()
        {
            var layout=CanThoMapSetupTool.ReadLayout();
            var nav=NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.That(layout.waterChecks.Length,Is.GreaterThanOrEqualTo(3));
                foreach(var point in layout.waterChecks)
                    Assert.IsFalse(NavMesh.SamplePosition(point,out _,.5f,NavMesh.AllAreas),"Enemy may not walk across water: "+point);
                foreach(var point in layout.peripheralRoads.Single(x=>x.id=="bung_xang").points)
                    Assert.IsTrue(NavMesh.SamplePosition(point,out _,.5f,NavMesh.AllAreas),"Shore road blocked: "+point);
            }
            finally { nav.Remove(); }
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            Assert.IsTrue(model.GetComponentsInChildren<MeshRenderer>().Any(x=>x.name.Contains("62_BungXangWaterfront")&&x.sharedMaterials.Any(m=>m.name=="CT2_Water")));
            Assert.GreaterOrEqual(layout.collisions.Count(x=>x.name=="Waterfront guard rail"),layout.waterBoundary.Length);
        }

        [Test]
        public void AuthoredCameraProbeFacesAnActualRotatedHouse()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Additive);
            try
            {
                var layout=CanThoMapSetupTool.ReadLayout();var p=layout.neighborhoodCameraProbe;
                Physics.SyncTransforms();
                var backwards=Quaternion.Euler(layout.neighborhoodCameraEuler)*Vector3.back;
                Assert.IsTrue(Physics.Raycast(p,backwards,out var hit,4,1,QueryTriggerInteraction.Ignore));
                Assert.IsTrue(hit.collider.name.StartsWith("District house"),hit.collider.name);
                var marker=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>()).Single(x=>x.name=="Neighborhood Camera Probe");
                Assert.That(Vector3.Distance(marker.position,p),Is.LessThan(.001f));
            }
            finally { EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous); }
        }
    }
}
