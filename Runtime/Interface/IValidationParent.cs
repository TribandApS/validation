namespace Triband.Validation.Runtime.Interface
{
    public interface IValidationParent
    {
        /// <summary>
        /// If this method returns true, validation is skipped for this object and any children in the hierarchy.
        /// </summary>
        /// <returns>True if validation should be skipped for this object and any children in the hierarchy.</returns>
        bool SkipValidationOfChildren();
    }
}