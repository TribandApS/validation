using Triband.Validation.Runtime;

namespace Triband.Validation.Editor
{
    public abstract class AssetDependencyPathValidator
    {
        /// <summary>
        /// Evaluates whether a parent is allowed to have an dependency to a child. For example,
        /// parentPath might point to a scene and childPath to a texture used in the level.
        /// Return false if the level is not allowed to use the texture
        /// </summary>
        /// <param name="parentPath">a prefab or scene that is used using the child</param>
        /// <param name="childPath">a prefab or asset that is used by the parent</param>
        /// <returns></returns>

        public abstract void ValidatePath(string parentPath, string childPath, IValidationContext context);
    }
}