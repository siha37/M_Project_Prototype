using System;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._00._Actor._8._Authoring
{
    /// <summary>
    /// 인스펙터에서 IActorComponent 하위 타입을 선택하기 위한 직렬화 클래스
    /// </summary>
    [Serializable]
    public class ComponentTypeSelector
    {
        [SerializeField] private string _assemblyQualifiedTypeName;
        [SerializeField] private string _displayName;

        /// <summary>
        /// 저장된 타입을 반환합니다. 해석에 실패하면 null.
        /// </summary>
        public Type GetSelectedType()
        {
            if (string.IsNullOrEmpty(_assemblyQualifiedTypeName))
                return null;
            
            return Type.GetType(_assemblyQualifiedTypeName);
        }

        /// <summary>
        /// 타입을 설정합니다.
        /// </summary>
        public void SetType(Type type)
        {
            if (type == null)
            {
                _assemblyQualifiedTypeName = null;
                _displayName = "None";
            }
            else
            {
                _assemblyQualifiedTypeName = type.AssemblyQualifiedName;
                _displayName = type.Name;
            }
        }

        /// <summary>
        /// 현재 선택이 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            var type = GetSelectedType();
            return type != null && !type.IsAbstract && !type.IsInterface;
        }

        /// <summary>
        /// 표시용 이름
        /// </summary>
        public string DisplayName => _displayName ?? "None";

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
