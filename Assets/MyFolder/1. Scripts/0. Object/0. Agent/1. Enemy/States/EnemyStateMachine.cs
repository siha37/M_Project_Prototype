using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyStateMachine : MonoBehaviour
    {   
        private Dictionary<Type,IEnemyState> States = new Dictionary<Type, IEnemyState>();
        private IEnemyState CurrentState;
        private IEnemyState PreviousState;
        
        [SerializeField] string CurrentStateName;
        [SerializeField] string PreviousStateName;
        
        public Action<IEnemyState,IEnemyState> StateChangeCallback;

        void Awake()
        {
            StateInit();
            StateChage(typeof(EnemyPatrolState));
        }

        private void Update()
        {
            CurrentState?.Update();
        }

        private void StateInit()
        {    
            States = new Dictionary<Type, IEnemyState>();
    
            // ✅ 수동으로 상태 등록 (가장 안전함)
            RegisterState(new EnemyPatrolState());
            RegisterState(new EnemyAttackState());
            RegisterState(new EnemyDieState());
            RegisterState(new EnemyMoveState());
        }

        private void RegisterState(IEnemyState state)
        {
            try
            {
                States[state.GetType()] = state;
                LogManager.Log(LogCategory.Enemy, $"상태 등록: {state.GetType().Name}");
            }
            catch (System.Exception e)
            {
                LogManager.LogError(LogCategory.Enemy, $"상태 등록 실패: {state.GetType().Name} - {e.Message}");
            }
        }
        public bool StateChage(Type stateType)
        {
            if (!CompareState(stateType) && States.ContainsKey(stateType))
            {
                IEnemyState state = States[stateType];
                if (state.CanStateChange())
                {
                    PreviousState = CurrentState;
                    PreviousStateName = PreviousState?.GetName();
                    CurrentState = state;
                    CurrentStateName = CurrentState.GetName();
                    PreviousState?.OnStateExit();
                    CurrentState.OnStateEnter();
                    StateChangeCallback?.Invoke(PreviousState, CurrentState);
                    return true;
                }
            }

            return false;
        }

        public string GetStateName()
        {
            return CurrentState.GetName();
        }

        public bool CompareState(Type type)
        {
            if (CurrentState != null)
                return CurrentState.GetType() == type;
            else
                return false;
        }
    }
}