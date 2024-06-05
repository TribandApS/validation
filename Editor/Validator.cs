using System;
using Triband.Validation.Runtime;
using Object = UnityEngine.Object;

namespace Triband.Validation.Editor
{
    public abstract class Validator<T> : IValidator where T : Object
    {
        protected abstract void Validate(T instance, IValidationContext context);

        public void DoValidation(Object instance, IValidationContext context)
        {
            var validator = instance as T;
            if (validator == null)
            {
                throw new NullReferenceException(nameof(instance));
            }

            Validate(validator, context);
        }

        public bool CanValidate(Object component)
        {
            return component != null && component is T;
        }
    }

    public interface IValidator
    {
        public void DoValidation(Object instance, IValidationContext context);
        bool CanValidate(Object component);
    }
}