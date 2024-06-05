using System;
using System.Collections;
using System.Collections.Generic;
using Triband.Validation.Editor.Data;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Triband.Validation.Editor
{
    [InitializeOnLoad]
    public static class ValidationManager
    {
        public static void RunValidation(bool async = true)
        {
            try
            {
                onValidationStarted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("The following exception was thrown when invoking ValidationRunner.onValidationStarted");
                Debug.LogError(e);
            }

            if (_CurrentRoutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_CurrentRoutine);
            }

            _CurrentRoutine = EditorCoroutineUtility.StartCoroutineOwnerless(CoroutineUtilities.RunCoroutineWithFixedInterval(DoValidation(ValidationFinished)));
        }

        public static IssueCollection currentIssueCollection { get; private set; } = new IssueCollection(new List<Issue>(), "");

        public static Action onValidationFinished;

        public static Action onValidationStarted;
        private static EditorCoroutine _CurrentRoutine;

        public static bool isRunningUnitTest { get; private set; }
        public static bool isAboutToEnterPlayMode { get; private set; }

        static ValidationManager()
        {
            EditorApplication.playModeStateChanged += newPlayMode =>
            {
                switch (newPlayMode)
                {
                    case PlayModeStateChange.ExitingEditMode:
                    case PlayModeStateChange.EnteredPlayMode:
                        isAboutToEnterPlayMode = true;
                        if (_CurrentRoutine != null)
                        {
                            EditorCoroutineUtility.StopCoroutine(_CurrentRoutine);
                            _CurrentRoutine = null;
                            ValidationFinished(new IssueCollection(new List<Issue>(), "Validation was aborted due to entering play mode"));
                        }

                        break;
                    case PlayModeStateChange.ExitingPlayMode:
                    case PlayModeStateChange.EnteredEditMode:
                        isAboutToEnterPlayMode = false;
                        break;
                }
            };
        }
        
        private static void ValidationFinished(IssueCollection issueCollection)
        {
            currentIssueCollection = issueCollection;
            _CurrentRoutine = null;
            onValidationFinished?.Invoke();
        }

        private static  IEnumerator DoValidation(Action<IssueCollection> onFinished)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                yield return ValidationCore.PrefabValidation(prefabStage, onFinished, true);
            }
            else
            {
                yield return ValidationCore.FullSceneValidation(onFinished, true);
            }
        }

        public static IssueCollection RunSynchronousSceneValidation()
        {
            IssueCollection issueCollection = default;
            CoroutineUtilities.RunCoroutineSynchronously(ValidationCore.FullSceneValidation((newResult) => { issueCollection = newResult; }, false));
            return issueCollection;
        }

        public static void MarkUnitTestActive()
        {
            isRunningUnitTest = true;
        }

        public static void MarkUnitTestFinished()
        {
            isRunningUnitTest = false;
        }
    }
}