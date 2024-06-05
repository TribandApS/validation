using System;

namespace Triband.Validation.Runtime.Attributes
{
    /// <summary>
    /// Validation: Prefab can have this field null, but it must be set in the scene.
    /// </summary>
    public class OptionalReferenceInPrefabAttribute : Attribute
    {
    }
}