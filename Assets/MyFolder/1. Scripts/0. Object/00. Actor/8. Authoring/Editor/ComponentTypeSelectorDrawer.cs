using System;
using MyFolder._1._Scripts._00._Actor._8._Authoring;
using UnityEditor;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._8._Authoring.Editor
{
    /// <summary>
    /// ComponentTypeSelector의 커스텀 PropertyDrawer
    /// IActorComponent 하위 클래스들을 드롭다운으로 표시합니다.
    /// </summary>
    [CustomPropertyDrawer(typeof(ComponentTypeSelector))]
    public class ComponentTypeSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 현재 선택된 타입 가져오기
            var assemblyQualifiedNameProp = property.FindPropertyRelative("_assemblyQualifiedTypeName");
            var displayNameProp = property.FindPropertyRelative("_displayName");
            
            Type currentType = null;
            if (!string.IsNullOrEmpty(assemblyQualifiedNameProp.stringValue))
            {
                currentType = Type.GetType(assemblyQualifiedNameProp.stringValue);
            }

            // 사용 가능한 컴포넌트 타입들 가져오기
            var availableTypes = ActorComponentReflection.GetAllComponentTypes();
            var displayNames = new string[availableTypes.Count + 1];
            displayNames[0] = "None";
            
            for (int i = 0; i < availableTypes.Count; i++)
            {
                var type = availableTypes[i];
                // 클래스명만 표시
                displayNames[i + 1] = type.Name;
            }

            // 현재 선택된 인덱스 찾기
            int currentIndex = 0;
            if (currentType != null)
            {
                int typeIndex = availableTypes.IndexOf(currentType);
                if (typeIndex >= 0)
                    currentIndex = typeIndex + 1; // +1 for "None" option
            }

            // 드롭다운 표시
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames);
            
            // 선택이 변경되었을 때 처리
            if (newIndex != currentIndex)
            {
                if (newIndex <= 0)
                {
                    // "None" 선택
                    assemblyQualifiedNameProp.stringValue = "";
                    displayNameProp.stringValue = "None";
                }
                else
                {
                    // 특정 타입 선택
                    var selectedType = availableTypes[newIndex - 1];
                    assemblyQualifiedNameProp.stringValue = selectedType.AssemblyQualifiedName;
                    displayNameProp.stringValue = selectedType.Name;
                }
                
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
