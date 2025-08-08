using System;
using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main
{
    public class EnemyControll  : NetworkBehaviour
    {
        private EnemyStateMachine stateMachine;
        private EnemyStatus status;
        private EnemyNetworkSync networkSync;
        private NavMeshAgent navagent;

        [Header("===Config Setting===")]
        [SerializeField] private EnemyConfig config;
        
       
        [Header("===Component Setting===")]
        private Dictionary<Type,IEnemyComponent> AllComponents = new Dictionary<Type, IEnemyComponent>();
        private Dictionary<Type,IEnemyComponent> EnemyComponents = new Dictionary<Type, IEnemyComponent>();
        private List<IEnemyUpdateComponent> UpdateComponents = new List<IEnemyUpdateComponent>();

        [Header("===Object Setting===")] 
        [SerializeField] private Transform shotPivot;
        [SerializeField] private Transform shotPoint;
        
        [Header("===Debug Setting===")]
        [SerializeField] private bool debugGizmos = true;
        [SerializeField] private bool debugLog = true;
        
        
        [SerializeField] private GameObject currentTarget;
        
        
        public EnemyConfig Config => config;
        public GameObject CurrentTarget => currentTarget;
        public EnemyStateMachine StateMachine => stateMachine;
        public EnemyNetworkSync NetworkSync => networkSync;
        public EnemyStatus Status => status;
        public Transform ShotPivot => shotPivot;
        public Transform ShotPoint => shotPoint;
        
        

        public override void OnStartServer()
        {
            Init();
            ComponentInit();
            EventSub();
            StateInit();
        }

        public override void OnStartClient()
        {
            if(IsServerInitialized) return;
            
            Init();
            ClientComponentInit();
            stateMachine.enabled = false;
            navagent.enabled = false;
            transform.rotation = Quaternion.identity;
        }

        private void Init()
        {
            if (config == null)
            {
                LogError("Config 파일이 없습니다", this);
                return;
            }
            TryGetComponent(out stateMachine);
            TryGetComponent(out status);
            TryGetComponent(out networkSync);
            TryGetComponent(out navagent);
        }

        private void EventSub()
        {
            stateMachine.StateChangeCallback += StateChage;
        }
        
        #region Component

        private void ClientComponentInit()
        {
            ComponentAdd(new EnemyCombat());
            ComponentDisactivate(new EnemyCombat());
        }
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

        public void ComponentActivate<T>(T component) where T: IEnemyComponent
        {
            if (AllComponents.TryGetValue(component.GetType(), out var com))
            {
                EnemyComponents.Add(component.GetType(),com);
                if(com is IEnemyUpdateComponent update_com)
                    UpdateComponents.Add(update_com);
            }
        }
        public void ComponentDisactivate<T>(T component) where T: IEnemyComponent
        {
            if (EnemyComponents.TryGetValue(typeof(T), out var com))
            {
                EnemyComponents.Remove(typeof(T));
                if (component is IEnemyUpdateComponent update_com)
                    UpdateComponents.Remove(update_com);
                component.OnDisable();
            } 
        }
        public IEnemyComponent GetEnemyActiveComponent(Type type)
        {
            if (EnemyComponents.ContainsKey(type))
                return EnemyComponents[type];
            return null;
        }

        public IEnemyComponent GetEnemyAllComponent(Type type)
        {
            if (AllComponents.ContainsKey(type))
                return AllComponents[type];
            return null;
        }

        #endregion

        #region State

        private void StateInit()
        {
            StateMachine.Init();
        }
        
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
            currentTarget = target;
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
            if (!debugGizmos || !config) return;
            
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
                    
                
                    if (CurrentTarget != null)
                    {
                        baseDirection = ((Vector2)CurrentTarget.transform.position - (Vector2)transform.position).normalized;
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
                
                    if (CurrentTarget != null)
                    {
                        Vector2 direction = ((Vector2)CurrentTarget.transform.position - (Vector2)transform.position).normalized;
                        float distance = Vector2.Distance(transform.position, CurrentTarget.transform.position);
                    
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
