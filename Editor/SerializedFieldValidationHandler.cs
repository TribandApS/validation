using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEngine;

namespace Triband.Validation.Editor
{
    [UsedImplicitly]
    public class SerializedFieldValidationHandler : Validator<Component>
    {
        internal class PropertyData
        {
            private Attribute[] _attributes;
            public readonly List<SerializedFieldValidator> validator;

            public PropertyData(Attribute[] attributes, List<SerializedFieldValidator> compatibleValidators)
            {
                _attributes = attributes;
                validator = compatibleValidators;
            }

            public Attribute[] attributes => _attributes;
            
            public bool TryGetAttribute<T>(out T result) where T : Attribute
            {
                foreach (var attribute in _attributes)
                {
                    if (attribute is T cast)
                    {
                        result = cast;
                        return true;
                    }
                }

                result = default;
                return false;
            }
        }

        private readonly SerializedFieldValidator[] _validationRules;

        private Dictionary<Type, Dictionary<string,PropertyData>> _cachedPropertyData = new Dictionary<Type, Dictionary<string,PropertyData>>();

        public SerializedFieldValidationHandler()
        {
            _validationRules = TypeCache.GetTypesDerivedFrom<SerializedFieldValidator>()
                .Select(Activator.CreateInstance).Cast<SerializedFieldValidator>().ToArray();
        }

        protected override void Validate(Component instance, IValidationContext context)
        {
            if (_validationRules.Length == 0) return;
            
            var componentType = instance.GetType();
            if (!_cachedPropertyData.TryGetValue(componentType, out var cachedPropertyData))
            {
                cachedPropertyData = new Dictionary<string, PropertyData>();
                _cachedPropertyData.Add(componentType, cachedPropertyData);
            }
            
            var serializeObject = new SerializedObject(instance);

            var iterator = serializeObject.GetIterator();
            do
            {
                HandleProperty(context, instance, iterator, cachedPropertyData,componentType);
            } while (iterator.NextVisible(true));
        }

        private void HandleProperty(IValidationContext context, Component component, SerializedProperty property, Dictionary<string, PropertyData> cachedPropertyData, Type componentType)
        {
            if (!cachedPropertyData.TryGetValue((property.propertyPath), out PropertyData propertyData))
            {
                var attributes = property.GetAttributes(true);
                List<SerializedFieldValidator> compatibleValidators = new List<SerializedFieldValidator>();
                foreach (var referenceValidator in _validationRules)
                {
                    if (referenceValidator.CanValidateProperty(componentType, property.propertyPath,
                            property.propertyType, null, attributes))
                    {
                        compatibleValidators.Add(referenceValidator);
                    }
                }

                propertyData = new PropertyData(attributes, compatibleValidators);
                cachedPropertyData.Add(property.propertyPath, propertyData);
            }

            foreach (var validationRule in propertyData.validator)
            {
                validationRule.ValidateProperty(context, property, component, propertyData.attributes);
            }
        }
    }
}
