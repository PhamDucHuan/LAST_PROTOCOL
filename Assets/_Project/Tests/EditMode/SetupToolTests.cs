using System.IO;
using System.Linq;
using LastProtocol.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace LastProtocol.Tests
{
    public sealed class SetupToolTests
    {
        [Test]
        public void SetupCanRunAgainWithoutOverwritingScenesPrefabsOrConfig()
        {
            if (UnityEngine.Application.isBatchMode && string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            MultiplayerSetupTool.Generate();
            string[] paths = { MultiplayerSetupTool.MenuPath, MultiplayerSetupTool.GamePath,
                MultiplayerSetupTool.PlayerPath, MultiplayerSetupTool.RoomPath, MultiplayerSetupTool.ConfigPath,
                MultiplayerSetupTool.PrefabListPath };
            byte[][] before = paths.Select(File.ReadAllBytes).ToArray();
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string[] buildBefore = EditorBuildSettings.scenes.Select(x => x.path + x.enabled).ToArray();
            MultiplayerSetupTool.Generate();
            for (int i = 0; i < paths.Length; i++) CollectionAssert.AreEqual(before[i], File.ReadAllBytes(paths[i]), paths[i]);
            CollectionAssert.AreEqual(buildBefore, EditorBuildSettings.scenes.Select(x => x.path + x.enabled).ToArray());
            Assert.AreEqual(active, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            MultiplayerSetupTool.Validate();
        }
    }
}
