using System;
using System.Linq;
using Triband.Validation.Editor;
using Triband.Validation.Runtime;
using Triband.Validation.Runtime.Attributes;
using UnityEditor;
using UnityEngine;

namespace Tiband.Validation.Samples
{
    public class NullRefValidator : SerializedFieldValidator
    {
        static readonly string[] nameSpacesToIgnore =
        {
            "UnityEngine",
            "TMPro",
            "Cinemachine",
        };

        static readonly string[] fieldNamesToIgnore =
        {
        };

        static readonly string[] assembliesToIgnore =
        {
        };

        public override bool CanValidateProperty(Type targetType, string propertyPath,
            SerializedPropertyType propertyType,
            Type objectReferenceType, Attribute[] attributes)
        {
            if (propertyType != SerializedPropertyType.ObjectReference)
            {
                return false;
            }

            //todo check if this warning is a problem
            if (attributes.ContainsType<OptionalReferenceAttribute>())
            {
                return false;
            }

            var @namespace = targetType.Namespace;
            if (@namespace != null)
            {
                if (nameSpacesToIgnore.Any(@namespace.StartsWith))
                {
                    return false;
                }
            }

            if (fieldNamesToIgnore.Any(propertyPath.EndsWith))
            {
                return false;
            }

            if (assembliesToIgnore.Any(targetType.Assembly.FullName.StartsWith))
            {
                return false;
            }

            return true;
        }

        public override void ValidateProperty(IValidationContext context, SerializedProperty property,
            Component component,
            Attribute[] attributes)
        {
            if (attributes.ContainsType<OptionalReferenceInPrefabAttribute>())
            {
                if (context.isSceneObject == false)
                {
                    return;
                }
            }

            if (attributes.TryGetAttribute(out RequiredIfAttribute requiredIf))
            {
                SerializedProperty conditionProperty =
                    property.serializedObject.FindProperty(requiredIf.conditionName);
                if (conditionProperty != null && conditionProperty.propertyType == SerializedPropertyType.Boolean)
                {
                    if (conditionProperty.boolValue == false)
                    {
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"Property {property.serializedObject.targetObject.name}.{property.propertyPath} has a RequiredIf attribute, but the field {requiredIf.conditionName} does not exist",
                        property.serializedObject.targetObject);
                }
            }

            context.IsNotNull(property.objectReferenceValue, null, () => $"Property <b>{property.propertyPath}</b> is <b><color=red>null</color></b>");
        }
    }
}