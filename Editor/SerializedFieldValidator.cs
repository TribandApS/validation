using System;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEngine;

namespace Triband.Validation.Editor
{
    public abstract class SerializedFieldValidator
    {
        /// <summary>
        /// Evaluate if a field can be evaluated at all. The result of this will be cached and
        /// only called once per field and the result is cached
        /// </summary>
        /// <param name="targetType"></param>
        /// <param name="propertyPath"></param>
        /// <param name="propertyType"></param>
        /// <param name="objectReferenceType"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public virtual bool CanValidateProperty(Type targetType, string propertyPath,
            SerializedPropertyType propertyType, Type objectReferenceType, Attribute[] attributes)
        {
            return true;
        }

        public abstract void ValidateProperty(IValidationContext context, SerializedProperty property,
            Component component, Attribute[] attributes);
    }
}
