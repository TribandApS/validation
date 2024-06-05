using System.IO;
using Triband.Validation.Editor;
using Triband.Validation.Runtime;

namespace Oculus.Platform.Models.Triband.Validation.Examples
{
    //This is a sample of how you can validate project structure in a project:
    //It enforces two rules:
    //   1: All scenes need to be inside a subfolder of "Assets/Scenes"
    //   2: A scene is not allowed to use assets from another scene's folder. 
    //
    //We used this approach in What The Bat? to prevent cross-level dependencies, 
    //but of course project architecture is different from project to project. 
    public class SceneAssetValidatorExample : AssetDependencyPathValidator
    {
        public override void ValidatePath(string parentPath, string childPath, IValidationContext context)
        {
            //in this example we only care about scenes
            if (Path.GetExtension(parentPath) != ".unity")
            {
                return;
            }

            //if an asset is in the same folder as the scene, that's fine
            var sceneFolder = Path.GetDirectoryName(parentPath);
            if (sceneFolder.StartsWith(parentPath))
            {
                return;
            }

            //if the asset is inside Assets/Scenes folder, but _not_ in the same folder
            //as the scenes, that's not ok
            context.IsFalse(childPath.StartsWith("Assets/Scenes"), null, () =>
            {
                return $"Scene {parentPath} is using scene-specific asset {childPath}, this is not allowed";
            });
        }
    }
}