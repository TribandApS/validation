using System;
using System.Linq;
using Triband.Validation.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

namespace Triband.Validation.Editor
{
    public class PrefabUsageValidator : Validator<GameObject>
    {
        readonly AssetDependencyPathValidator[] _rules;

        public PrefabUsageValidator()
        {
            _rules = TypeCache.GetTypesDerivedFrom<AssetDependencyPathValidator>().Where(t => !t.IsAbstract)
                .Select(Activator.CreateInstance).Cast<AssetDependencyPathValidator>().ToArray();
        }

        protected override void Validate(GameObject gameObject, IValidationContext context)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
            {
                string parentPath;

                if (context.isSceneObject)
                {
                    parentPath = gameObject.scene.path;
                }
                else
                {
                    PrefabStage prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);

                    if (prefabStage != null)
                    {
                        parentPath = prefabStage.assetPath;
                    }
                    else
                    {
                        parentPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject.transform);
                    }

                }
                CheckUsageForPrefabAndParentPrefabs(gameObject, context, parentPath, new HashSet<string>());
            }
        }
        
        //The prefab we're checking might be the child prefab of another prefab. We keep calling 
        //PrefabUtility.GetCorrespondingObjectFromSource to get all parents of our prefab to be sure
        void CheckUsageForPrefabAndParentPrefabs(GameObject gameObject, IValidationContext context, string parentPath, HashSet<string> alreadyValidatedPrefabPaths)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

            if (!alreadyValidatedPrefabPaths.Contains(prefabPath))
            {
                foreach (AssetDependencyPathValidator pathRule in _rules)
                {
                    pathRule.ValidatePath(parentPath, prefabPath, context);
                }

                alreadyValidatedPrefabPaths.Add(prefabPath);
            }

            var source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (source != gameObject && source != null)
            {
                CheckUsageForPrefabAndParentPrefabs(source, context, parentPath, alreadyValidatedPrefabPaths);
            }
        }
    }
}