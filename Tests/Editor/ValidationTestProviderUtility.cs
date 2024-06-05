using System;
using System.Linq;
using Triband.Validation.Editor;
using Triband.Validation.Runtime.Interface;

namespace Triband.Validation
{
    public sealed class ValidationTestProviderUtility
    {
        public static IValidationSystemSceneProvider GetProvider()
        {
#if UNITY_EDITOR
            // Fast path for Editor
            var customProviders = UnityEditor.TypeCache.GetTypesDerivedFrom(typeof(IValidationSystemSceneProvider))
                .Where(t => t != typeof(DefaultProvider))
                .ToArray();
#else
            // Slow path for runtime. Cant use type cache :(
            var customProviders = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a != null)
                        .SelectMany(a => a.GetTypes())
                        .Where(t => !t.IsInterface)
                        .Where(t => typeof(IValidationTestProvider).IsAssignableFrom(t))
                        .Where(t => t != typeof(DefaultProvider))
                        .ToArray();
#endif
            
            // Return first custom provider if it exists
            if(customProviders.Length > 0)
            {
                return (IValidationSystemSceneProvider)Activator.CreateInstance(customProviders[0]);
            }

            // Otherwise return default
            return new DefaultProvider();
        }
    }
}