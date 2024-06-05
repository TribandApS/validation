using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Triband.Validation.Editor.Data;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Triband.Validation.Editor.UI
{
    public class ValidationWindow : EditorWindow
    {
        private VisualTreeAsset _resultUI;
        private ScrollView m_ScrollView;
        private Label m_StatusText;
        private Button m_OptionsButton;
        private StyleBackground _styleErrorIcon;
        private StyleBackground _styleWarningIcon;

        [MenuItem("Triband/Validation/Validation Window")]
        public static void ShowWindow()
        {
            GetWindow(typeof(ValidationWindow));
        }

        private void CreateGUI()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            _styleErrorIcon =
                new StyleBackground(EditorGUIUtility.IconContent("console.erroricon.sml").image as Texture2D);
            _styleWarningIcon =
                new StyleBackground(EditorGUIUtility.IconContent("console.warnicon.sml").image as Texture2D);

            var uiAsset =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Packages/com.triband.validation/Editor/UI/ValidationWindow.uxml");
            VisualElement ui = uiAsset.Instantiate();

            rootVisualElement.Add(ui);

            _resultUI = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.triband.validation/Editor/UI/ValidationResult.uxml");

            m_ScrollView = ui.Q<ScrollView>("Results");
            ui.Q<Button>("Run").clicked += () => { ValidationManager.RunValidation(); };

            m_StatusText = ui.Q<Label>("Status");
            m_StatusText.text = "No results yet";

            m_OptionsButton = ui.Q<Button>("Options");
            m_OptionsButton.Add(new Image() { image = EditorGUIUtility.IconContent("_Menu").image });

            var dropdownMenu = new GenericMenu();
            dropdownMenu.AddItem(new GUIContent("Show Passing Tests"), false, () =>
            {
                if (ValidationManager.currentIssueCollection.issues.Count > 500)
                {
                    if (!EditorUtility.DisplayDialog("Are you sure?",
                            $"There are {ValidationManager.currentIssueCollection.issues.Count} passed checks, displaying them might slow down the editor a lot.",
                            "proceed", "cancel"))
                    {
                        return;
                    }
                }

                UpdateList();
            });

            m_OptionsButton.clicked += () => dropdownMenu.ShowAsContext();

            ValidationManager.onValidationFinished += UpdateList;
            ValidationManager.onValidationStarted += ClearResults;
            UpdateList();

            if (Application.isPlaying)
            {
                rootVisualElement.SetEnabled(false);
            }
        }

        void OnPlayModeChanged(PlayModeStateChange newPlayModeState)
        {
            rootVisualElement.SetEnabled(newPlayModeState == PlayModeStateChange.EnteredEditMode ||
                                         newPlayModeState == PlayModeStateChange.ExitingPlayMode);
        }

        void OnDestroy()
        {
            ValidationManager.onValidationFinished -= UpdateList;
            ValidationManager.onValidationStarted -= ClearResults;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }


        private void UpdateList()
        {
            ClearResults();
            var results = ValidationManager.currentIssueCollection.issues;

            List<Issue> resultsToShow;
            resultsToShow = results;

            if (ValidationManager.currentIssueCollection.issues.Count > 0)
            {
                m_StatusText.text = $" Failed tests {ValidationManager.currentIssueCollection.issues.Count}.";
            }
            else
            {
                m_StatusText.text = "All checks passed!";
            }

            AddResultsToList(resultsToShow.Where(r => r.parent != null == false).ToList(), m_ScrollView, false);
        }

        private void AddResultsToList(List<Issue> resultsToShow, VisualElement parentVisualElement, bool isChild)
        {
            var sceneScopeLevels = resultsToShow.Where(check => check.isDependency == false).ToList();
            if (sceneScopeLevels.Count > 0)
            {
                AddHeader("Checks in " + ValidationManager.currentIssueCollection.inspectedStageInfoText,
                    parentVisualElement);
                foreach (var check in sceneScopeLevels) AddResult(check, parentVisualElement, isChild);
            }

            var assetScopeLevels = resultsToShow.Where(check => check.isDependency).ToList();
            if (assetScopeLevels.Count > 0)
            {
                foreach (var group in assetScopeLevels.GroupBy(check => check.validationScope.Target))
                {
                    var dependencyPath = AssetDatabase.GetAssetPath(@group.Key);
                    AddHeader($"Checks in dependency {dependencyPath}", parentVisualElement);
                    foreach (var check in @group)
                    {
                        AddResult(check, parentVisualElement, isChild);
                    }
                }
            }
        }

        void ClearResults()
        {
            m_ScrollView.Clear();
            m_StatusText.text = "";
        }

        private void AddHeader(string text, VisualElement parentVisualElement)
        {
            var header = new Label(text);
            header.AddToClassList("Header");
            parentVisualElement.Add(header);
        }

        private void AddResult(Issue issue, VisualElement parentVisualElement, bool isChildList = false)
        {
            if (issue.parent != null && !isChildList)
            {
                return;
            }

            var resultContainer = _resultUI.Instantiate();

            resultContainer.userData = issue;

            resultContainer.Q<Label>("Source").text = Path.GetFileName(issue.sourceInfo.filePath);


            resultContainer.Q<Label>("Message").text = issue.infoText;

            resultContainer.Q<VisualElement>("Icon").style.backgroundImage = issue.severity switch
            {
                ValidationSeverity.Error => _styleErrorIcon,
                ValidationSeverity.Warning => _styleWarningIcon,
                _ => throw new ArgumentOutOfRangeException()
            };

            var openSourceButton = resultContainer.Q<Button>("OpenSource");
            openSourceButton.Add(new Image() { image = EditorGUIUtility.IconContent("_Menu").image });
            openSourceButton.clicked += () =>
            {
                var pathInAssetFolder =
                    issue.sourceInfo.filePath.Replace(Application.dataPath.Replace("/Assets", ""), "");
                Debug.Log(pathInAssetFolder);
                if (!AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(pathInAssetFolder),
                        issue.sourceInfo.sourceLine))
                {
                    System.Diagnostics.Process.Start($"{issue.sourceInfo.filePath}");
                }
            };

            var objectField = resultContainer.Q<ObjectField>("Object");

            var selectedObject = issue.validationScope.Target;
            if (selectedObject == null)
            {
                objectField.visible = false;
            }
            else
            {
                objectField.value = selectedObject;

                //hacky way to disable the ObjectPicker child of the object field, which
                //turns the ObjectField "read-only"
                objectField.Children().First().Children().ToArray()[1].visible = false;
            }

            var fixButton = resultContainer.Q<Button>("Fix");
            if (issue.autoFix == null)
            {
                fixButton.parent.Remove(fixButton);
            }

            void GetChildrenRecursive(Issue check, List<Issue> children)
            {
                if (check.children != null)
                {
                    foreach (var child in check.children)
                    {
                        children.Add(child);
                        GetChildrenRecursive(child, children);
                    }
                }
            }

            List<Issue> childChecks = new List<Issue>();
            GetChildrenRecursive(issue, childChecks);

            if (childChecks.Count > 0 && !isChildList)
            {
                var foldoutGroup = new Foldout();
                foldoutGroup.AddToClassList("Foldout");
                AddResultsToList(childChecks, foldoutGroup, true);
                foldoutGroup.text = $"{childChecks.Count} instances of this prefab also failed the check";
                foldoutGroup.value = false;
                resultContainer.Add(foldoutGroup);
            }

            fixButton.clicked += () =>
            {
                issue.ApplyAutoFixIfPossible();
                UpdateList();
            };

            parentVisualElement.Add(resultContainer);
        }
    }
}