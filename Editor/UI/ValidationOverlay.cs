using System.Collections.Generic;
using System.Linq;
using Triband.Validation.Editor.Data;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Triband.Validation.Editor.UI
{
    [Overlay(typeof(SceneView), "Validation", true)]
    public class ValidationOverlay : Overlay
    {
        #region Private Fields

        private List<string> _situationNames;
        private DropdownField _dropdownField;
        private VisualElement _root;
        private TextElement _infoText;
        private Button _openValidationWindowButton;
        private IssueCollection _issueCollection;
        private VisualElement _validationRunningPanel;
        private VisualElement _validationResultsPanel;

        #endregion

        #region Public methods

        public override VisualElement CreatePanelContent()
        {
            _root = new VisualElement();

            _validationRunningPanel = new VisualElement();
            _validationRunningPanel.Add(new Label("Validating..."));
            _validationRunningPanel.visible = false;
            _root.Add(_validationRunningPanel);
            _validationResultsPanel = new VisualElement();
            _root.Add(_validationResultsPanel);

            _infoText = new TextElement
            {
                text = "No results",
                style = { color = new StyleColor(Color.grey) }
            };
            _validationResultsPanel.Add(_infoText);

            var button = new Button { text = "Update" };
            button.clicked += StartValidation;
            _validationResultsPanel.Add(button);

            _openValidationWindowButton = new Button { text = "Open Validation Window" };
            _openValidationWindowButton.clicked += () =>
            {
                var validationWindow = EditorWindow.GetWindow<ValidationWindow>();
                validationWindow.Show();
            };

            UpdateEventCallbacks(displayed);
            
            UpdateFromResult();
            ValidationManager.RunValidation();

            return _root;
        }

        private static void StartValidation()
        {
            ValidationManager.RunValidation();
        }

        public override void OnCreated()
        {
            displayedChanged += UpdateEventCallbacks;
            UpdateEventCallbacks(displayed);
        }

        void UpdateEventCallbacks(bool isVisible)
        {
            if (isVisible)
            {
                EditorSceneManager.sceneSaved += OnSceneSaved;
                EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

                ValidationManager.onValidationStarted += OnValidationStarted;
                ValidationManager.onValidationFinished += UpdateFromResult;

                PrefabStage.prefabStageOpened += PrefabStageChanged;
                PrefabStage.prefabStageClosing += PrefabStageChanged;

                EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
            else
            {
                EditorSceneManager.sceneSaved -= OnSceneSaved;
                EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;

                ValidationManager.onValidationFinished -= UpdateFromResult;
                ValidationManager.onValidationStarted -= OnValidationStarted;

                PrefabStage.prefabStageOpened -= PrefabStageChanged;
                PrefabStage.prefabStageClosing -= PrefabStageChanged;
                
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            }
        } 

        void OnPlayModeChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode || obj == PlayModeStateChange.EnteredPlayMode)
            {
                _root.SetEnabled(false);
            }
            else
            {
                _root.SetEnabled(true);
            }
        }

        void PrefabStageChanged(PrefabStage _)
        {
            StartValidation();
        }


        private void OnValidationStarted()
        {
            _validationResultsPanel.visible = false;
            _validationRunningPanel.visible = true;
        }

        #endregion

        #region Private methods

        private void OnSceneChanged(Scene _, Scene newScene)
        {
            //When running Unit Tests, Unity first loads a completely empty scene
            //Running validation in this one causes crashes
            if (newScene.name == string.Empty && newScene.rootCount == 0)
            {
                return;
            }

            if (ValidationManager.isRunningUnitTest)
            {
                return;
            }

            StartValidation();
        }

        private void OnSceneSaved(Scene _)
        {
            StartValidation();
        }

        private void UpdateFromResult()
        {
            _validationResultsPanel.visible = true;
            _validationRunningPanel.visible = false;

            if (!displayed)
            {
                return;
            }

            _issueCollection = ValidationManager.currentIssueCollection;

            if (_issueCollection.issues.Count > 0)
            {
                var errorCount =
                    _issueCollection.issues.Count(issue => issue.severity == ValidationSeverity.Error);
                
                var warningCount =
                    _issueCollection.issues.Count(issue => issue.severity == ValidationSeverity.Warning);

                _infoText.enableRichText = true;
                _infoText.text = string.Empty;
                
                if (errorCount > 0)
                {
                    _infoText.text += $"<color=red>{errorCount} Errors Found In Scene</color>";
                    if (warningCount > 0)
                    {
                        _infoText.text += "\n";
                    }
                }
                
                if (warningCount > 0)
                {
                    _infoText.text += $"<color=yellow>{warningCount} Warnings Found In Scene</color>";
                }

                if (!_validationResultsPanel.Contains(_openValidationWindowButton))
                {
                    _validationResultsPanel.Add(_openValidationWindowButton);
                }
            }
            else
            {
                _infoText.text = "No Issues Found";
                _infoText.style.color = new StyleColor(Color.green);

                if (_validationResultsPanel.Contains(_openValidationWindowButton))
                {
                    _validationResultsPanel.Remove(_openValidationWindowButton);
                }
            }
        }

        #endregion
    }
}