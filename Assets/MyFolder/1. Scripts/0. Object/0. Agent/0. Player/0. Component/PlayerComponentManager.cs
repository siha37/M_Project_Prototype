using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerComponentManager : MonoBehaviour
    {
        private PlayerInputControll playerInputControll;
        private PlayerControll playerControll;
        
        private Dictionary<Type,IPlayerComponent> components;
        private Dictionary<Type,PlayerUpdateComponent> update_components;
        private void Start()
        {
            Init();
            ComponentInit();
        }

        private void Init()
        {
            TryGetComponent(out playerInputControll);
            TryGetComponent(out playerControll);
        }
        private void ComponentInit()
        {
            components = new Dictionary<Type,IPlayerComponent>();
            update_components = new Dictionary<Type,PlayerUpdateComponent>();
            components.Clear();
            update_components.Clear();

            AddComponent<PlayerCamouflageComponent>();
        }

        private void AddComponent<T>() where T : IPlayerComponent,new()
        {
            //인스턴스 생성
            IPlayerComponent component = new T();
            components.Add(typeof(T),component);
            //컴포넌트 초기화
            component.Start(playerControll);
            //키 이벤트 등록
            component.SetKeyEvent(playerInputControll);
            
            //업데이트 컴포넌트 등록
            if(component is PlayerUpdateComponent up_com)
            {
                update_components.Add(typeof(T),up_com);
            }
        }

        public IPlayerComponent GetPComponent<T>() where T : IPlayerComponent
        {
            components.TryGetValue(typeof(T),out IPlayerComponent component);
            return component;
        }
        
        
        private void Update()
        {
            foreach (var component in update_components)
            {
                component.Value.Update();
            }
        }
        private void FixedUpdate()
        {
            foreach (var component in update_components)
            {
                component.Value.FixedUpdate();
            }
        }
        private void LateUpdate()
        {
            foreach (var component in update_components)
            {
                component.Value.LateUpdate();
            }
        }
    }
}