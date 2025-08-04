using System;
using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main
{
    public class EnemyControll  : NetworkBehaviour
    {

        private EnemyAi ai;
        private EnemyStateMachine stateMachine;

        [Header("===Config Setting===")]
        [SerializeField] private EnemyConfig config;
        
       
        [Header("===Component Setting===")]
        private Dictionary<Type,IEnemyComponent> AllComponents = new Dictionary<Type, IEnemyComponent>();
        private Dictionary<Type,IEnemyComponent> EnemyComponents = new Dictionary<Type, IEnemyComponent>();
        private List<IEnemyUpdateComponent> UpdateComponents = new List<IEnemyUpdateComponent>();

        [Header("===Debug Setting===")]
        [SerializeField] private bool debugGizmos = true;
        [SerializeField] private bool debugLog = true;
        
        
        private GameObject currenttarget;
        
        
        public EnemyConfig Config => config;
        public GameObject Currenttarget => currenttarget;
        public EnemyStateMachine StateMachine => stateMachine;
        
        

        public override void OnStartServer()
        {
            Init();
            ComponentInit();
            EventSub();
        }

        private void Init()
        {
            if (config == null)
            {
                LogError("Config 파일이 없습니다", this);
                return;
            }
            TryGetComponent(out ai);
            TryGetComponent(out stateMachine);
        }

        private void EventSub()
        {
            stateMachine.StateChangeCallback += StateChage;
        }

        #region Component

        
        private void ComponentInit()
        {
            ComponentAdd(new EnemyMovement());
            ComponentAdd(new EnemyCombat());
            ComponentAdd(new EnemyPercetion());
            ComponentActivate(new EnemyPercetion());
            ComponentActivate(new EnemyCombat());
            ComponentActivate(new EnemyMovement());
        }

        private void ComponentAdd(IEnemyComponent com)
        {
            AllComponents.Add(com.GetType(),com);
            com.Init(this);
        }

        private void ComponentActivate<T>(T component) where T: IEnemyComponent
        {
            if (AllComponents.TryGetValue(component.GetType(), out var com))
            {
                EnemyComponents.Add(component.GetType(),com);
                if(com is IEnemyUpdateComponent update_com)
                    UpdateComponents.Add(update_com);
            }
        }
        private void ComponentDisactivate<T>(T component) where T: IEnemyComponent
        {
            if (EnemyComponents.TryGetValue(typeof(T), out var com))
            {
                EnemyComponents.Remove(typeof(T));
                if (component is IEnemyUpdateComponent update_com)
                    UpdateComponents.Remove(update_com);
                component.OnDisable();
            } 
        }
        public IEnemyComponent GetEnemyComponent(Type type)
        {
            if (EnemyComponents.ContainsKey(type))
                return EnemyComponents[type];
            return null;
        }

        #endregion

        #region State

        /// <summary>
        /// 상태 변경 시 컴포넌트로 콜백
        /// </summary>
        /// <param name="oldState"></param>
        /// <param name="newState"></param>
        public void StateChage(IEnemyState oldState,IEnemyState newState)
        {
            foreach (KeyValuePair<Type, IEnemyComponent> component in EnemyComponents)
            {
                component.Value.ChangedState(oldState,newState);
            }
        }

        #endregion
        
        /*==================Public ========================*/
        public void SetTarget(GameObject target)
        {
            currenttarget = target;
        }
        
        /*==================Private ========================*/
        private void Update()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.Update();
            }
        }

        private void FixedUpdate()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.FixedUpdate();
            }
        }

        private void LateUpdate()
        {
            foreach (IEnemyUpdateComponent updateComponent in UpdateComponents)
            {
                updateComponent.LateUpdate();
            }
        }


        private void Log(string message,Object context = null)
        {
            if(debugLog)
                LogManager.Log(LogCategory.Enemy,message,context);
        }

        private void LogError(string message,Object context = null)
        {
            if(debugLog)
                LogManager.LogError(LogCategory.Enemy,message,context);
        }


        private void OnDrawGizmos()
        {
            if (!debugGizmos || config) return;
            
            Vector2 baseDirection = Vector2.right;
            float FieldOfViewAngle = config.fieldOfViewAngle;
            float DetectionRange = config.detectionRange;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position,config.detectionRange);
            
            if (EnemyComponents.Count > 0)
            {
                if (EnemyComponents.ContainsKey(typeof(EnemyPercetion)))
                {
                    EnemyPercetion percetion = (EnemyPercetion)EnemyComponents[typeof(EnemyPercetion)];
                    Gizmos.color = percetion.HasLineOfSight ? Color.green : Color.yellow;   
                    
                
                    if (Currenttarget != null)
                    {
                        baseDirection = ((Vector2)Currenttarget.transform.position - (Vector2)transform.position).normalized;
                    }
                    
                    int segments = 16;
                    float angleStep = FieldOfViewAngle / segments;
                
                    Vector3 previousPoint = transform.position;
                
                    for (int i = 0; i <= segments; i++)
                    {
                        float angle = -FieldOfViewAngle * 0.5f + angleStep * i;
                        Vector2 direction = Quaternion.Euler(0, 0, angle) * baseDirection; // 타겟 방향 기준 회전
                        Vector3 point = transform.position + (Vector3)direction * DetectionRange;
                    
                        if (i > 0)
                        {
                            Gizmos.DrawLine(previousPoint, point);
                        }
                    
                        previousPoint = point;
                    }
                
                    // 시야각의 양 끝점을 시작점과 연결
                    Vector2 leftDirection = Quaternion.Euler(0, 0, -FieldOfViewAngle * 0.5f) * baseDirection;
                    Vector2 rightDirection = Quaternion.Euler(0, 0, FieldOfViewAngle * 0.5f) * baseDirection;
                
                    Vector3 leftPoint = transform.position + (Vector3)leftDirection * DetectionRange;
                    Vector3 rightPoint = transform.position + (Vector3)rightDirection * DetectionRange;
                
                    Gizmos.DrawLine(transform.position, leftPoint);
                    Gizmos.DrawLine(transform.position, rightPoint);
                    
                    
                    
                    Gizmos.color = Color.red;
                
                    if (Currenttarget != null)
                    {
                        Vector2 direction = ((Vector2)Currenttarget.transform.position - (Vector2)transform.position).normalized;
                        float distance = Vector2.Distance(transform.position, Currenttarget.transform.position);
                    
                        Gizmos.DrawRay(transform.position, (Vector3)direction * distance);
                    }
            
                    // 마지막으로 본 위치 표시
                    if (percetion.LastSeenPosition != Vector3.zero)
                    {
                        Gizmos.color = Color.gray;
                        Gizmos.DrawWireSphere(percetion.LastSeenPosition, 0.5f);
                        Gizmos.DrawLine(transform.position, percetion.LastSeenPosition);
                    }
                }
            }
            else
            {
                int segments = 16;
                float angleStep = FieldOfViewAngle / segments;
                
                Vector3 previousPoint = transform.position;
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = -FieldOfViewAngle * 0.5f + angleStep * i;
                    Vector2 direction = Quaternion.Euler(0, 0, 90) * baseDirection; // 타겟 방향 기준 회전
                    Vector3 point = transform.position + (Vector3)direction * DetectionRange;
                    
                    if (i > 0)
                    {
                        Gizmos.DrawLine(previousPoint, point);
                    }
                    
                    previousPoint = point;
                }
                
                // 시야각의 양 끝점을 시작점과 연결
                Vector2 leftDirection = Quaternion.Euler(0, 0, -FieldOfViewAngle * 0.5f) * baseDirection;
                Vector2 rightDirection = Quaternion.Euler(0, 0, FieldOfViewAngle * 0.5f) * baseDirection;
                
                Vector3 leftPoint = transform.position + (Vector3)leftDirection * DetectionRange;
                Vector3 rightPoint = transform.position + (Vector3)rightDirection * DetectionRange;
                
                Gizmos.DrawLine(transform.position, leftPoint);
                Gizmos.DrawLine(transform.position, rightPoint);
            }
            
        }

        private void OnValidate()
        {
            if (!debugGizmos) return;
        }
    }
}
