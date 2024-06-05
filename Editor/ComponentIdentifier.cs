using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Triband.Validation.Editor
{
    public struct ComponentIdentifier
    {
        private sealed class GuidFileIDInstanceIDEqualityComparer : IEqualityComparer<ComponentIdentifier>
        {
            public bool Equals(ComponentIdentifier x, ComponentIdentifier y)
            {
                return x.guid.Equals(y.guid) && x.fileID == y.fileID && x.instanceID == y.instanceID;
            }

            public int GetHashCode(ComponentIdentifier obj)
            {
                return HashCode.Combine(obj.guid, obj.fileID, obj.instanceID);
             }
        }

        public static IEqualityComparer<ComponentIdentifier> GuidFileIDInstanceIDComparer { get; } = new GuidFileIDInstanceIDEqualityComparer();

        public bool Equals(ComponentIdentifier other)
        {
            return guid.Equals(other.guid) && fileID == other.fileID && instanceID == other.instanceID;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(guid, fileID, instanceID);
        }

        public ComponentIdentifier(Object obj)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out fileID))
            {
                instanceID = -1;
            }
            else
            {
                instanceID = obj.GetInstanceID();
                guid = string.Empty;
                fileID = -1;
            }
            
        }

        public override string ToString()
        {
            return $"{nameof(instanceID)}:{instanceID} {nameof(fileID)}:{fileID} {nameof(guid)}:{guid}";
        }

        public readonly string guid;
        public readonly long fileID;
        public readonly int instanceID;

        //[MenuItem("Test/TestID")]
        public static void TryGet()
        {
//          Debug.Log(new ComponentIdentifier(Selection.activeGameObject.transform));
            //
            // var obj = new SerializedObject(Selection.activeGameObject);
            // var serializedProperty = obj.FindProperty("m_LocalIdentifierInFile");
            // Debug.Log(serializedProperty.longValue);

            var l = Selection.activeGameObject.GetComponent<Light>();
        }
    }
}