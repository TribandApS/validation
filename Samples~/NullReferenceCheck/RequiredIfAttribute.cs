using System;

namespace Tiband.Validation.Samples
{
    public class RequiredIfAttribute : Attribute
    {
        public readonly string conditionName;

        public RequiredIfAttribute(string conditionName) 
        {
            this.conditionName = conditionName;
        }
    }
}