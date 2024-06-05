using Triband.Validation.Runtime.Interface;
using UnityEngine;

namespace Triband.Validation.Runtime
{
    public static class ValidationUtils
    {
        public static bool ShouldBeSkippedDueToValidationParent(GameObject gameObject)
        {
            var validationParent = gameObject.GetComponentInParent<IValidationParent>(true);
            if (validationParent != null)
            {
                if (validationParent.SkipValidationOfChildren())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldBeSkippedDueToValidationParent(Component component)
        {
            return ShouldBeSkippedDueToValidationParent(component.gameObject);
        }
    }
}