using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Triband.Validation.Editor.Data;
using Triband.Validation.Runtime;
using Triband.Validation.Runtime.Interface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_2020
using UnityEditor.Experimental.SceneManagement;
#endif

namespace Triband.Validation.Editor
{
    internal static class ValidationCore
    {
        private static readonly IValidator[] validators;
        private static readonly SceneValidator[] sceneValidators;
        private static readonly AssetDependencyPathValidator[] assetDependencyPathValidator;

        static ValidationCore()
        {
            validators = TypeCache.GetTypesDerivedFrom(typeof(Validator<>)).Where(type => !type.IsAbstract).Select(type => Activator.CreateInstance(type) as IValidator).ToArray();
            sceneValidators = TypeCache.GetTypesDerivedFrom(typeof(SceneValidator)).Where(type => !type.IsAbstract).Select(type => Activator.CreateInstance(type) as SceneValidator).ToArray();
            assetDependencyPathValidator = TypeCache.GetTypesDerivedFrom(typeof(AssetDependencyPathValidator)).Where(type => !type.IsAbstract).Select(type => Activator.CreateInstance(type) as AssetDependencyPathValidator).ToArray();

        }

        public static IEnumerator FullSceneValidation(Action<IssueCollection> onFinished, bool richText)
        {
            var context = new ValidationContext(richText);

            yield return RunSceneValidators(context);
            yield return ValidateAllObjectsInScenes(context);

            string infoText = "Scene(s) ";
            string[] openScenePaths = new string[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                openScenePaths[i] = SceneManager.GetSceneAt(i).path;
                infoText += openScenePaths[i] + "  ";
            }

            yield return ValidateDependencies(openScenePaths, context);

            onFinished?.Invoke(new IssueCollection(context.results, infoText));
        }

        public static IEnumerator PrefabValidation(PrefabStage prefabStage, Action<IssueCollection> onFinished, bool richText)
        {
            var context = new ValidationContext(richText);

            yield return ValidateGameObjectHierarchy(context, prefabStage.prefabContentsRoot, validators, ValidationScope.ObjectType.Prefab, false);

            yield return ValidateDependencies(new [] { prefabStage.assetPath }, context);

            onFinished?.Invoke(new IssueCollection(context.results, $"Prefab {System.IO.Path.GetFileName(prefabStage.assetPath)}"));
        }

        static IEnumerator CollectDependenciesRecursive(string parentAssetPaths, HashSet<string> allDependencies)
        {
            var dependencies = AssetDatabase.GetDependencies(parentAssetPaths, false);
            foreach (string newDependency in dependencies)
            {
                if (newDependency.EndsWith(".unity"))
                    continue;

                yield return null;
                if (!allDependencies.Contains(newDependency))
                {
                    allDependencies.Add(newDependency);
                    yield return CollectDependenciesRecursive(newDependency, allDependencies);
                }
            }
        }

        private static IEnumerator ValidateDependencies(string[] parentAssetPaths, ValidationContext context)
        {
            HashSet<string> dependenciesChecked = new HashSet<string>();

            foreach (var assetPath in parentAssetPaths)
            {
                yield return CollectDependenciesRecursive(assetPath, dependenciesChecked);
            }

            var dependencies = dependenciesChecked.Where(path => !parentAssetPaths.Contains(path));

            List<Object> assets=new List<Object>();
            foreach (var dependency in dependencies)
            {
                assets.Add(AssetDatabase.LoadAssetAtPath<Object>(dependency));
                yield return null;
            }

            foreach (var asset in assets)
            {
                if (asset is GameObject prefab)
                {
                    yield return ValidateGameObjectHierarchy(context, prefab, validators, ValidationScope.ObjectType.Prefab, true);
                }
                else
                {
                    foreach (var validator in validators)
                    {
                        if (validator.CanValidate(asset))
                        {
                            context.SetupScope(ValidationScope.ObjectType.Asset, asset, true);
                            validator.DoValidation(asset, context);
                        }
                    }
                }
            }

            context.SetupScope(ValidationScope.ObjectType.Asset, null, true);
            foreach (string parentAssetPath in parentAssetPaths)
            {
                foreach (string dependency in dependencies)
                {
                    foreach (AssetDependencyPathValidator dependencyPathValidator in assetDependencyPathValidator)
                    {
                        dependencyPathValidator.ValidatePath(parentAssetPath, dependency, context);
                    }
                }
            }

            yield return null;
        }

        // Validate all static methods implementing ValidateStaticMethodAttribute
        private static IEnumerator RunSceneValidators(ValidationContext context)
        {
            context.SetupScope(ValidationScope.ObjectType.Scene, null, false);
            foreach (var sceneValidator in sceneValidators)
            {
                try
                {
                    sceneValidator.DoValidation(context);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception was caught while running scene validator {sceneValidator}");
                    Debug.LogError(e);
                }

                yield return null;
            }
        }

        // Validate all Component instances of IRequireValidation
        private static IEnumerator ValidateAllObjectsInScenes(ValidationContext context)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                // Get all components in Scene
                var scene = SceneManager.GetSceneAt(i);
                var rootObjectsInScene = scene.GetRootGameObjects();

                for (var index = 0; index < rootObjectsInScene.Length; index++)
                {
                    var rootObjects = rootObjectsInScene[index];
                    yield return ValidateGameObjectHierarchy(context, rootObjects, validators, ValidationScope.ObjectType.SceneObject, false);
                }
            }
        }

        private static IEnumerator ValidateGameObjectHierarchy(ValidationContext context, GameObject rootObjects, IValidator[] validators, ValidationScope.ObjectType objectType, bool isDependency)
        {
            var gameObjects = rootObjects.GetComponentsInChildren<Transform>(true).Select(transform => transform.gameObject);

            foreach (var gameObject in gameObjects)
            {
                if (ValidationUtils.ShouldBeSkippedDueToValidationParent(gameObject))
                {
                    continue;
                }

                ValidateObject(context, gameObject, validators, objectType, isDependency);

                foreach (var component in gameObject.GetComponents<Component>())
                {
                    ValidateObject(context, component, validators, objectType, isDependency);
                }

                yield return null;
            }
        }

        private static void ValidateObject(ValidationContext context, Object @object, IValidator[] validators, ValidationScope.ObjectType objectType, bool isDependency)
        {
            try
            {
                context.SetupScope(objectType, @object, isDependency);

                if (@object is IRequireValidation validate)
                {
                    validate.Validate(context);
                }

                foreach (var validator in validators)
                {
                    if (validator.CanValidate(@object))
                    {
                        validator.DoValidation(@object, context);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception was caught while validating object {@context}", @object);
                Debug.LogError(e);
            }
        }
    }
}

