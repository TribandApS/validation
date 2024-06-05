using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Triband.Validation.Editor;
using Triband.Validation.Editor.Data;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Triband.Validation
{
    [TestFixtureSource(typeof(ScenePathsProvider))]
    public class SceneTesting
    {
        readonly SceneInfo m_ScenePath;

        public SceneTesting(SceneInfo scenePath)
        {
            m_ScenePath = scenePath;
        }

        [OneTimeSetUp]
        public void LoadScene()
        {
            ValidationManager.MarkUnitTestActive();
            EditorSceneManager.OpenScene(m_ScenePath.path);
        }

        [Test]
        public void PassesValidation()
        {
            IssueCollection issuesCollection = ValidationManager.RunSynchronousSceneValidation();

            // Deal with failures
            var sb = new StringBuilder();
            List<Issue> errorsOnly = issuesCollection.issues.Where(i => i.severity == ValidationSeverity.Error).ToList();
            if (errorsOnly.Count != 0)
            {
                sb.AppendLine($"{errorsOnly.Count} check(s) have failed:");
                foreach (var check in errorsOnly)
                {
                    sb.AppendLine($"{Path.GetFileName(check.sourceInfo.filePath)} ({check.infoText})");
                }
            
                // Add a helpful message
                string sceneName = Path.GetFileNameWithoutExtension(m_ScenePath.ToString());
                sb.AppendLine($"\nOpen Scene ({sceneName}) and run from the Validation Window to debug objects.");
            
                Debug.Log(sb.ToString());
            
                Assert.IsTrue(errorsOnly.Count(issue => issue.severity == ValidationSeverity.Error) == 0, sb.ToString());
            }
        }

        [OneTimeTearDown]
        public void AfterTestsRan()
        {
            ValidationManager.MarkUnitTestFinished();
        }
    }
    
    class ScenePathsProvider : IEnumerable<SceneInfo>
    {
        IEnumerator<SceneInfo> IEnumerable<SceneInfo>.GetEnumerator()
        {
            string[] scenePaths = ValidationTestProviderUtility.GetProvider().GetTestScenePaths().Distinct().ToArray();
            foreach (string scenePath in scenePaths)
            {
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    continue;
                }
                
                string sceneGUID = AssetDatabase.AssetPathToGUID(scenePath);
                yield return new SceneInfo(scenePath, sceneGUID);
            }
        }
    
        public IEnumerator GetEnumerator() => ((IEnumerable<SceneInfo>) this).GetEnumerator();

        static string GetScenePathFromAsset(Object asset)
        {
            if (!asset)
            {
                return string.Empty;
            }
            
            // We get the scene path via a serialized property to avoid having to set up asmdefs.
            var serializedAsset = new SerializedObject(asset);
            SerializedProperty pathProp = serializedAsset.FindProperty("m_ScenePath");
            string scenePath = pathProp.stringValue;
            pathProp.Dispose();
            serializedAsset.Dispose();

            return scenePath;
        }
    }

    public struct SceneInfo
    {
        public string path { get; }
        public string guid { get; }

        public SceneInfo(string path, string guid)
        {
            this.path = path;
            this.guid = guid;
        }
        
        public override string ToString()
        {
            string folderPath = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(folderPath);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            string shortGuid = guid.Substring(0, 4);
            
            return $"{folderName}/{sceneName} [{shortGuid}]";
        }
    }
}