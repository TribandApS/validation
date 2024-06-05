using Triband.Validation.Runtime;
using UnityEngine;

namespace Triband.Validation.Editor.Data
{
    public struct ValidationScope
    {
        public readonly Object Target;
        public ObjectType Type;

        /// <summary>
        /// If true, the scope is not the primary target of the current validation. For example, it might be a prefab
        /// that is used in a scene or an asset
        /// </summary>
        public bool IsDependency;

        public enum ObjectType
        {
            Scene,
            SceneObject,
            Prefab,
            Asset,
        }

        public ValidationScope(Object target, ObjectType type, bool isDependency)
        {
            Target = target;
            Type = type;
            IsDependency = isDependency;
        }
    }
}