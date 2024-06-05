using System;
using System.Collections.Generic;
using System.Linq;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using Debug = System.Diagnostics.Debug;

namespace Triband.Validation.Editor.Data
{
    public class Issue
    {
        public readonly ValidationScope validationScope;
        public readonly SourceInfo sourceInfo;
        public readonly ValidationSeverity severity;

        public Issue parent { get; private set; }
        public string infoText { get; private set; }
        public Action autoFix { get; private set; }

        public List<Issue> children { get; private set; } = null;
        
        public bool isDependency => validationScope.IsDependency;
        public bool isAsset => validationScope.Type == ValidationScope.ObjectType.Asset;

        internal Issue(ValidationScope validationScope, SourceInfo sourceInfo, string infoText, Action autoFix, ValidationSeverity severity)
        {
            this.validationScope = validationScope;
            this.sourceInfo = sourceInfo;
            this.infoText = infoText;
            this.autoFix = autoFix;
            this.severity = severity;
        }

#if UNITY_EDITOR
        public void ApplyAutoFixIfPossible()
        {
            if (autoFix != null)
            {
                autoFix.Invoke();

                if (validationScope.Type == ValidationScope.ObjectType.Prefab)
                {
                    if (validationScope.IsDependency)
                    {
                        throw new Exception("Auto fixes to prefabs are not allowed to be applied to prefabs that are dependencies, since those " +
                                            "prefabs are not actually opened for editing and changing them from code will corrupt the prefab.");
                    }
                }

                Debug.Assert(validationScope.Target != null, nameof(validationScope.Target) + " != null");

                if (validationScope.Target != null)
                {
                    EditorUtility.SetDirty(validationScope.Target);
                    AssetDatabase.SaveAssetIfDirty(validationScope.Target);
                }

                if (validationScope.Type == ValidationScope.ObjectType.Scene)
                {
                    EditorSceneManager.MarkAllScenesDirty();
                }

                infoText = "fixed";
                autoFix = null;
            }
        }
#endif
        public void ResolveParent(List<Issue> checks)
        {
            var validatedObject = validationScope.Target;
            if (validatedObject == null)
            {
                return;
            }
            var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(validatedObject);
            if (prefabObject == null)
            {
                return;
            }

            parent = checks.FirstOrDefault(otherCheck =>
            {
                if (otherCheck.validationScope.Target != prefabObject)
                {
                    return false;
                }

                return otherCheck.sourceInfo.Equals(sourceInfo);
            });
            if (parent != null)
            {
                parent.RegisterChild(this);
            }
        }

        private void RegisterChild(Issue child)
        {
            if (children == null)
            {
                children = new List<Issue>();
            }
            children.Add(child);
        }
    }
}
