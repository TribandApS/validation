using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Triband.Validation.Editor.Data;
using Triband.Validation.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Triband.Validation.Editor
{
    public struct ValidationContext : IValidationContext
    {
        private readonly SourceCodeParser m_Parser;
        private List<Issue> m_Checks;
        private readonly bool m_RichText;

        public ValidationContext(bool richText = true)
        {
            if (Application.isPlaying)
            {
                throw new Exception("Validation was started while editor was in play mode, this is not supported");
            }

            m_RichText = richText;
            m_Parser = new SourceCodeParser();
            m_Checks = new List<Issue>();
            m_CurrentScope = new ValidationScope(default, default, default);
        }

        public List<Issue> results => m_Checks;

        public bool IsAsset => m_CurrentScope.Type == ValidationScope.ObjectType.Asset;
        public bool isSceneObject => m_CurrentScope.Type == ValidationScope.ObjectType.SceneObject;

        private ValidationScope m_CurrentScope;

        public void Dispose()
        {
            m_Parser.Dispose();
            m_Checks = null;
        }


        internal void SetupScope(ValidationScope.ObjectType scopeType, Object target, bool isDependency)
        {
            m_CurrentScope = new ValidationScope(target, scopeType, isDependency);
        }

        public bool AreEqual(object expected, object value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error, [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            var result = value.Equals(expected);
            if (result)
            {
                return true;
            }

            var args = m_Parser.ParseArguments(2, filePath, sourceCodeLine, out var validationCheckIdentifier);

            string errorText;
            if (getResultText == null)
            {
                errorText = GenerateInfoText(result, args[1], expected.ToString(), value.ToString(), severity);
            }
            else
            {
                errorText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, errorText, severity, autoFix);

            return result;
        }

        public bool AreNotEqual(object expected, object value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            var result = !value.Equals(expected);
            if (result)
            {
                return true;
            }

            var args = m_Parser.ParseArguments(2, filePath, sourceCodeLine, out var validationCheckIdentifier);


            string errorText;
            if (getResultText == null)
            {
                errorText = GenerateInfoText(result, args[0], expected.ToString(), value.ToString(), severity);
            }
            else
            {
                errorText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, errorText, severity, autoFix);

            return result;
        }

        public bool IsNotNull(object value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            bool result;
            if (value is Object unityObject)
            {
                result = unityObject != null;
            }
            else
            {
                result = value != null;
            }

            if (result)
            {
                return true;
            }

            var args = m_Parser.ParseArguments(1, filePath, sourceCodeLine, out var validationCheckIdentifier);

            string errorText;
            if (getResultText == null)
            {
                errorText = GenerateInfoText(result, args[0], "not null", "null", severity);
            }
            else
            {
                errorText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, errorText, severity, autoFix);

            return result;
        }

        public bool IsNull(object value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            bool result;
            if (value is Object unityObject)
            {
                result = unityObject == null;
            }
            else
            {
                result = value == null;
            }

            if (result)
            {
                return true;
            }

            var args = m_Parser.ParseArguments(1, filePath, sourceCodeLine, out var validationCheckIdentifier);

            string resultText;
            if (getResultText == null)
            {
                resultText = GenerateInfoText(result, args[0], "", value?.ToString(), severity);
            }
            else
            {
                resultText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, resultText,  severity,autoFix);

            return result;
        }

        public bool IsTrue(bool value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            var result = value == true;
            if (result)
            {
                return true;
            }

            var args = m_Parser.ParseArguments(1, filePath, sourceCodeLine, out var validationCheckIdentifier);

            string resultText;
            if (getResultText == null)
            {
                resultText = GenerateInfoText(result, args[0], true.ToString(), value.ToString(), severity);
            }
            else
            {
                resultText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, resultText, severity, autoFix);

            return result;
        }

        public bool IsFalse(bool value, Action autoFix = null, Func<string> getResultText = null,
            ValidationSeverity severity = ValidationSeverity.Error,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int sourceCodeLine = 0)
        {
            var result = value == false;

            if (result)
            {
                return true;
            }


            var args = m_Parser.ParseArguments(1, filePath, sourceCodeLine, out var validationCheckIdentifier);

            string resultText;
            if (getResultText == null)
            {
                resultText = GenerateInfoText(result, args[0], false.ToString(), value.ToString(), severity);
            }
            else
            {
                resultText = getResultText();
            }

            RegisterIssue(validationCheckIdentifier, resultText, severity, autoFix);

            return result;
        }

        private string GenerateInfoText(bool result, string evaluatedFunction, string expectedResult,
            string actualResult, ValidationSeverity severity)
        {
            if (m_RichText)
            {
                if (result)
                {
                    return $"<b>{evaluatedFunction}</b> is <b><color=green>{expectedResult}</color></b>.";
                }
                else
                {
                    var highlightColor = severity == ValidationSeverity.Error ? "red" : "yellow";
                    return
                        $"<b>{evaluatedFunction}</b> is expected to be <b>{expectedResult}</b>, but is <b><color={highlightColor}>{actualResult}</color></b>";
                }
            }
            else
            {
                if (result)
                {
                    return $"{evaluatedFunction} is {expectedResult}.";
                }
                else
                {
                    return $"{evaluatedFunction} is expected to be {expectedResult}, but is {actualResult}";
                }
            }
        }

        private void RegisterIssue(SourceInfo sourceInfo, string infoText, ValidationSeverity severity, Action autoFix = null)
        {
#if UNITY_2021_1_OR_NEWER
            if (!m_RichText)
#endif
            {
                infoText = StripRichText(infoText);
            }

            if (m_CurrentScope.IsDependency)
            {
                if (m_CurrentScope.Type != ValidationScope.ObjectType.Asset)
                {
                    //we don't allow auto-fixing prefabs that are checked as dependencies, because
                    //modifying a prefab that is not open for editing from code will corrupt the prefab
                    autoFix = null;
                }
            }

            // Create and cache result
            var check = new Issue(m_CurrentScope, sourceInfo, infoText, autoFix, severity);
            m_Checks.Add(check);
        }

        private string StripRichText(string infoText)
        {
            return infoText.Replace("</b>", string.Empty).Replace("<b>", string.Empty)
                .Replace("<color=green>", string.Empty).Replace("<color=red>", string.Empty)
                .Replace("</color>", string.Empty);
        }
    }
}