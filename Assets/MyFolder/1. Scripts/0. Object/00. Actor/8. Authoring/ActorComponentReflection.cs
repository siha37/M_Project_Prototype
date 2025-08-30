using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MyFolder._1._Scripts._0._Object._00._Actor._0._Core;

namespace MyFolder._1._Scripts._00._Actor._8._Authoring
{
    /// <summary>
    /// IActorComponent 하위 클래스들을 탐색하는 유틸리티
    /// </summary>
    public static class ActorComponentReflection
    {
        private static List<Type> _cachedComponentTypes;
        
        /// <summary>
        /// 모든 IActorComponent 하위 클래스를 반환합니다.
        /// </summary>
        public static List<Type> GetAllComponentTypes()
        {
            if (_cachedComponentTypes != null)
                return _cachedComponentTypes;

            var componentTypes = new List<Type>();
            var targetInterface = typeof(IActorComponent);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // IActorComponent를 구현하는 클래스만
                        if (!targetInterface.IsAssignableFrom(type))
                            continue;
                        
                        // 추상 클래스나 인터페이스 제외
                        if (type.IsAbstract || type.IsInterface)
                            continue;
                        
                        // 매개변수 없는 생성자가 있는지 확인
                        if (type.GetConstructor(Type.EmptyTypes) == null)
                            continue;

                        componentTypes.Add(type);
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 일부 타입 로드 실패 시에도 로드된 타입들은 처리
                    foreach (var type in ex.Types)
                    {
                        if (type == null) continue;
                        if (!targetInterface.IsAssignableFrom(type)) continue;
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (type.GetConstructor(Type.EmptyTypes) == null) continue;
                        
                        componentTypes.Add(type);
                    }
                }
                catch
                {
                    // 어셈블리 접근 실패 시 무시
                    continue;
                }
            }

            // 이름순으로 정렬
            _cachedComponentTypes = componentTypes
                .Distinct()
                .OrderBy(t => t.Namespace ?? "")
                .ThenBy(t => t.Name)
                .ToList();

            return _cachedComponentTypes;
        }

        /// <summary>
        /// 캐시를 초기화합니다 (새로운 컴포넌트가 추가되었을 때 사용)
        /// </summary>
        public static void ClearCache()
        {
            _cachedComponentTypes = null;
        }

        /// <summary>
        /// 타입으로부터 IActorComponent 인스턴스를 생성합니다.
        /// </summary>
        public static IActorComponent CreateInstance(Type componentType)
        {
            if (componentType == null)
                return null;

            if (!typeof(IActorComponent).IsAssignableFrom(componentType))
                return null;

            try
            {
                return Activator.CreateInstance(componentType) as IActorComponent;
            }
            catch
            {
                return null;
            }
        }
    }
}
