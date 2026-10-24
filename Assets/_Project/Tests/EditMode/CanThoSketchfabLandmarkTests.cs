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
    public sealed class CanThoSketchfabLandmarkTests
    {
        [Test]
        public void ImportedLandmarksUseTheirOwnUrpSurfacesWithinExistingParcels()
        {
            var scene=EditorSceneManager.OpenScene(CanThoMapSetupTool.ScenePath,OpenSceneMode.Additive);
            try
            {
                var renderers=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MeshRenderer>()).ToArray();
                var groups=new[]{"24_TurtleAuditorium","05_Classrooms_A3","25_Sports"};
                var parcels=new[]{new Bounds(new Vector3(-45.5f,15,12.5f),new Vector3(27.3f,30,21.8f)),new Bounds(new Vector3(96,15,-21.36f),new Vector3(35,30,24.3f)),new Bounds(new Vector3(70,15,130),new Vector3(48,30,26))};
                for(int i=0;i<groups.Length;i++)
                {
                    var imported=renderers.Where(r=>r.name.StartsWith(groups[i]+"__CT12_")).ToArray();Assert.Greater(imported.Length,0,groups[i]);
                    Assert.IsTrue(imported.All(r=>r.sharedMaterials.All(m=>m!=null && m.name.StartsWith("CT12_") && m.shader.name=="Universal Render Pipeline/Lit")));
                    var parcel=parcels[i];parcel.Expand(.02f);
                    foreach(var r in imported){Assert.IsTrue(parcel.Contains(r.bounds.min),r.name);Assert.IsTrue(parcel.Contains(r.bounds.max),r.name);}
                }
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        [Test]
        public void A3CourtyardRemainsWalkableBetweenSolidClassroomWings()
        {
            var layout=CanThoMapSetupTool.ReadLayout();var nav=NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0,0,-40),out var start,.5f,NavMesh.AllAreas));
                foreach(var id in new[]{"05_Classrooms_A3_3","05_Classrooms_A3_4","24_TurtleAuditorium","25_Sports"})
                {
                    var point=layout.landmarks.Single(x=>x.id==id).approach;Assert.IsTrue(NavMesh.SamplePosition(point,out var target,.6f,NavMesh.AllAreas),id);
                    var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(start.position,target.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status,id);
                }
                foreach(var wing in layout.collisions.Where(x=>x.name.StartsWith("Sketchfab A3 wing ")))
                    Assert.IsFalse(NavMesh.SamplePosition(new Vector3(wing.position.x,0,wing.position.z),out _,.2f,NavMesh.AllAreas),wing.name);
                Assert.AreEqual(8,layout.collisions.Count(x=>x.name.StartsWith("Sketchfab A3 wing ")));
                Assert.IsFalse(layout.collisions.Any(x=>x.name=="NHÀ HỌC A3" || x.name=="Gym" || x.name.StartsWith("Turtle Hall porch")));
            }
            finally{nav.Remove();}
        }
    }
}
