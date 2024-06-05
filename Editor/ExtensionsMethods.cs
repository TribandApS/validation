using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Triband.Validation.Editor
{
    public static class ExtensionsMethods
    {
        public static bool TryGetAttribute<TArrayType, TDesiredType>(this TArrayType[] array, out TDesiredType value) where TArrayType : Attribute where TDesiredType : Attribute
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] is TDesiredType result)
                {
                    value = result;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool ContainsType<TDesiredType>(this Attribute[] array) where TDesiredType : Attribute
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] is TDesiredType)
                {
                    return true;
                }
            }

            return false;
        }
    }
    
     internal static class SerializedPropertyExtensions
    {
        //From https://forum.unity.com/threads/serialiedproperty-check-if-it-has-a-propertyattribute.436103/#post-7349540
        public static T GetAttribute<T>(this SerializedProperty prop, bool inherit) where T : Attribute
        {
            if (prop == null)
            {
                return null;
            }

            Type t = prop.serializedObject.targetObject.GetType();

            FieldInfo f = null;
            PropertyInfo p = null;
            foreach (string name in prop.propertyPath.Split('.'))
            {
                f = TryGetFieldInfoRecursive(t, name);

                if (f == null)
                {
                    p = t.GetProperty(name, (BindingFlags)(-1));
                    if (p == null)
                    {
                        return null;
                    }

                    t = p.PropertyType;
                }
                else
                {
                    t = f.FieldType;
                }
            }

            T[] attributes;

            if (f != null)
            {
                attributes = f.GetCustomAttributes(typeof(T), inherit) as T[];
            }
            else if (p != null)
            {
                attributes = p.GetCustomAttributes(typeof(T), inherit) as T[];
            }
            else
            {
                return null;
            }

            return attributes != null && attributes.Length > 0 ? attributes[0] : null;
        }

        public static Attribute[] GetAttributes(this SerializedProperty prop, bool inherit)
        {
            if (prop == null)
            {
                return null;
            }

            Type t = prop.serializedObject.targetObject.GetType();

            FieldInfo f = null;
            PropertyInfo p = null;
            foreach (string name in prop.propertyPath.Split('.'))
            {
                f = TryGetFieldInfoRecursive(t, name);

                if (f == null)
                {
                    p = t.GetProperty(name, (BindingFlags)(-1));
                    if (p == null)
                    {
                        return Array.Empty<Attribute>();
                    }

                    t = p.PropertyType;
                }
                else
                {
                    t = f.FieldType;
                }
            }

            if (f != null)
            {
                return f.GetCustomAttributes(inherit) as Attribute[];
            }
            else if (p != null)
            {
                return p.GetCustomAttributes(inherit) as Attribute[];
            }

            throw new ArgumentException(
                $"Failed to get Attributes for property {prop.propertyPath} in type {prop.serializedObject.targetObject.GetType().FullName}");
        }

        public static FieldInfo TryGetFieldInfoRecursive(Type t, string name)
        {
            try
            {
                //all bind flags except IgnoreCase
                var bindingFlags = (BindingFlags)(-1) ^ BindingFlags.IgnoreCase;

                FieldInfo field = t.GetField(name, bindingFlags);   if (field != null)
                {
                    return field;
                }

                if (t.BaseType != null)
                {
                    return TryGetFieldInfoRecursive(t.BaseType, name);
                }

                return null;
            }
#pragma warning disable CS0168
            catch (Exception e)
#pragma warning restore CS0168
            {
                Debug.LogError($"{t.FullName}   field name: {name} did not work");
                throw;
            }
         
        }

    }
}