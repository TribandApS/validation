using System;
using System.Runtime.CompilerServices;

namespace Triband.Validation.Runtime
{
    public interface IValidationContext: IDisposable
    {
        bool isSceneObject { get; }
        
        bool AreEqual(object expected, object value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
        bool AreNotEqual(object expected, object value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
        bool IsNotNull(object value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
        bool IsNull(object value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
        bool IsTrue(bool value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
        bool IsFalse(bool value, Action autoFix = null, Func<string> errorText = null, ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "", [CallerLineNumber] int sourceCodeLine = 0);
    }
}