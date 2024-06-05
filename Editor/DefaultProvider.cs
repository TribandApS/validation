using System.Collections;
using System.Linq;
using Triband.Validation.Runtime.Interface;

namespace Triband.Validation.Editor
{
    public sealed class DefaultProvider : IValidationSystemSceneProvider
    {
        const string EXCLUDE_LABEL = "ExcludeValidation";

        public string[] GetTestScenePaths()
        {
            return UnityEditor.EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(s =>
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(s);
                    var labels = UnityEditor.AssetDatabase.GetLabels(asset);
                    return !labels.Contains(EXCLUDE_LABEL);
                }).ToArray();
        }
    }
}
