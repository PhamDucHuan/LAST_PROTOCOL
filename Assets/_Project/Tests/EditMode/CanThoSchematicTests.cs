using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace LastProtocol.Tests
{
    public sealed class CanThoSchematicTests
    {
        [Test]
        public void ReferenceDistrictsHaveTheCorrectSpatialRelationships()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.artRevision, Is.InRange(4, 11));
            Vector3 At(string id) => layout.landmarks.First(x => x.id == id || x.id.StartsWith(id + "_")).position;
            Assert.Less(At("22_Dorm_B").x, At("19_Fisheries").x);
            Assert.Less(At("19_Fisheries").x, At("16_Engineering").x);
            Assert.Less(At("16_Engineering").x, At("23_Dorm_A").x);
            Assert.Greater(At("19_Fisheries").z, At("09_Agriculture").z);
            Assert.Greater(At("23_Dorm_A").z, At("14_Economics").z);
            Assert.Less(At("09_Agriculture").x, At("07_ICT").x);
            Assert.Less(At("07_ICT").x, At("08_NaturalSciences").x);
            Assert.Greater(At("41_ClassroomC1").z, At("02_Administration").z);
            Assert.Less(At("24_TurtleAuditorium").x, At("02_Administration").x);
            Assert.Greater(At("03_LearningResource").x, At("02_Administration").x);
            Assert.Greater(At("48_BanyanGarden").z, At("05_Classrooms_A3").z);
            foreach (var id in new[] { "37_Preparatory", "38_PracticeSchool", "39_Law", "40_Languages",
                "42_YouthOffice", "43_ClassroomD2", "44_D1Postgraduate", "45_Greenhouses", "47_LRCLawn" })
                Assert.IsTrue(layout.landmarks.Any(x => x.id == id), "Missing reference landmark: " + id);
            Assert.AreEqual(layout.landmarks.Length, layout.landmarks.Select(x => x.id).Distinct().Count(), "Stable IDs must be unique");
            Assert.AreEqual(3, layout.gates.Length);
            Assert.Less(layout.gates.Single(x => x.id == "C").position.x, layout.gates.Single(x => x.id == "B").position.x);
            Assert.Less(layout.gates.Single(x => x.id == "B").position.x, layout.gates.Single(x => x.id == "A").position.x);
        }

        [Test]
        public void AllFacultyEntrancesAndMissionRalliesRemainReachableAfterRecomposition()
        {
            var nav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>(CanThoMapSetupTool.NavigationPath));
            try
            {
                Assert.IsTrue(NavMesh.SamplePosition(new Vector3(0, 0, -40), out var start, .5f, NavMesh.AllAreas));
                foreach (var landmark in CanThoMapSetupTool.ReadLayout().landmarks)
                {
                    Assert.IsTrue(NavMesh.SamplePosition(landmark.approach, out var target, .8f, NavMesh.AllAreas), landmark.title);
                    var path = new NavMeshPath();
                    Assert.IsTrue(NavMesh.CalculatePath(start.position, target.position, NavMesh.AllAreas, path), landmark.title);
                    Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status, landmark.title);
                }
                var mission = AssetDatabase.LoadAssetAtPath<MissionDefinition>(MissionSetupTool.DefinitionPath);
                foreach (var objective in mission.objectives)
                    Assert.IsTrue(NavMesh.SamplePosition(objective.center, out _, .5f, NavMesh.AllAreas), objective.title);
            }
            finally { nav.Remove(); }
        }

        [Test]
        public void AngledPerimeterUsesOrientedShapesAndFitsEnvironmentArtBudget()
        {
            var layout = CanThoMapSetupTool.ReadLayout();
            Assert.That(layout.campusBoundary.Length, Is.GreaterThan(15), "Reference is an irregular footprint");
            Assert.That(layout.collisions.Count(x => x.name.StartsWith("Campus fence ") && Mathf.Abs(x.euler.y % 90) > 1),
                Is.GreaterThan(8), "Slanted edges must not use blocking world-aligned AABBs");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CanThoMapSetupTool.ModelPath);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            long triangles = model.GetComponentsInChildren<MeshFilter>().Sum(x => (long)x.sharedMesh.triangles.Length / 3);
            Assert.Less(renderers.Length, 500, "Merged collection/material export budget");
            Assert.Less(triangles, 600000, "Campus environment triangle budget; does not assert FPS");
            Assert.IsFalse(renderers.Any(x => x.name.Contains("90_InspectionLabels")), "Inspection labels must not ship in the model");
        }
    }
}
