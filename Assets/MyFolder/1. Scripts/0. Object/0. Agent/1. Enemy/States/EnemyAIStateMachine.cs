using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 AI 상태 머신
/// 상태 생성, 전환, 업데이트, 히스토리 관리를 담당
/// </summary>
public class EnemyAIStateMachine
{
    // 상태 인스턴스 관리
    private Dictionary<EnemyAIStateType, EnemyAIState> stateInstances;
    private EnemyAIState currentState;
    private EnemyAIState previousState;
    
    // 상태 머신 소유자
    private EnemyAI ownerAI;
    
    // 상태 전환 히스토리 (디버깅용)
    private Queue<StateTransition> transitionHistory;
    private const int MaxHistoryCount = 10;
    
    // 상태 전환 제어
    private bool isTransitioning = false;
    private float lastTransitionTime = 0f;
    private const float MinTransitionInterval = 0.1f; // 최소 전환 간격 (스패밍 방지)
    
    // 이벤트
    public event Action<EnemyAIStateType, EnemyAIStateType> OnStateChanged; // from, to
    public event Action<EnemyAIState> OnStateEntered;
    public event Action<EnemyAIState> OnStateExited;
    
    /// <summary>
    /// 현재 상태
    /// </summary>
    public EnemyAIState CurrentState => currentState;
    
    /// <summary>
    /// 이전 상태
    /// </summary>
    public EnemyAIState PreviousState => previousState;
    
    /// <summary>
    /// 현재 상태 타입
    /// </summary>
    public EnemyAIStateType CurrentStateType => currentState?.StateType ?? EnemyAIStateType.Patrol;
    
    /// <summary>
    /// 상태 전환 중인지 여부
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    /// <summary>
    /// 상태 머신 초기화
    /// </summary>
    /// <param name="ai">소유자 AI</param>
    public void Initialize(EnemyAI ai)
    {
        ownerAI = ai;
        transitionHistory = new Queue<StateTransition>();
        
        // 모든 상태 인스턴스 생성
        CreateStateInstances();
        
        // 초기 상태를 Patrol로 설정
        ChangeState(EnemyAIStateType.Patrol, true);
        
        LogManager.Log(LogCategory.Enemy, $"EnemyAIStateMachine 초기화 완료 - 초기 상태: {currentState?.StateName}", ai);
    }
    
    /// <summary>
    /// 모든 상태 인스턴스 생성
    /// </summary>
    private void CreateStateInstances()
    {
        stateInstances = new Dictionary<EnemyAIStateType, EnemyAIState>
        {
            { EnemyAIStateType.Patrol, new PatrolState() },
            { EnemyAIStateType.Chase, new ChaseState() },
            { EnemyAIStateType.Attack, new AttackState() },
            { EnemyAIStateType.Retreat, new RetreatState() }
        };
    }
    
    /// <summary>
    /// 상태 업데이트 (매 프레임 호출)
    /// </summary>
    public void Update()
    {
        if (currentState != null && ownerAI != null)
        {
            currentState.Update(ownerAI);
        }
    }
    
    /// <summary>
    /// 고정 업데이트 (물리 연산용)
    /// </summary>
    public void FixedUpdate()
    {
        if (currentState != null && ownerAI != null)
        {
            currentState.FixedUpdate(ownerAI);
        }
    }
    
    /// <summary>
    /// 상태 변경 (타입 안전한 버전)
    /// </summary>
    /// <typeparam name="T">변경할 상태 타입</typeparam>
    /// <param name="forceTransition">강제 전환 여부</param>
    /// <returns>전환 성공 여부</returns>
    public bool ChangeState<T>(bool forceTransition = false) where T : EnemyAIState
    {
        // 상태 타입에서 enum 추출
        EnemyAIStateType stateType = GetStateTypeFromClass<T>();
        return ChangeState(stateType, forceTransition);
    }
    
    /// <summary>
    /// 상태 변경 (enum 버전)
    /// </summary>
    /// <param name="newStateType">변경할 상태 타입</param>
    /// <param name="forceTransition">강제 전환 여부</param>
    /// <returns>전환 성공 여부</returns>
    public bool ChangeState(EnemyAIStateType newStateType, bool forceTransition = false)
    {
        // 전환 유효성 검사
        if (!forceTransition && !CanTransitionTo(newStateType))
        {
            return false;
        }
        
        // 새 상태 가져오기
        if (!stateInstances.TryGetValue(newStateType, out EnemyAIState newState))
        {
            LogManager.LogError(LogCategory.Enemy, $"상태를 찾을 수 없습니다: {newStateType}", ownerAI);
            return false;
        }
        
        // 같은 상태로 전환하려는 경우 (강제 전환이 아니면 무시)
        if (!forceTransition && currentState != null && currentState.StateType == newStateType)
        {
            return false;
        }
        
        // 전환 시작
        isTransitioning = true;
        EnemyAIState oldState = currentState;
        
        try
        {
            // 이전 상태 정리
            if (currentState != null)
            {
                currentState.Exit(ownerAI);
                OnStateExited?.Invoke(currentState);
            }
            
            // 상태 변경
            previousState = currentState;
            currentState = newState;
            
            // 새 상태 진입
            currentState.Enter(ownerAI);
            OnStateEntered?.Invoke(currentState);
            
            // 전환 시간 업데이트
            lastTransitionTime = Time.time;
            
            // 히스토리 기록
            RecordTransition(oldState?.StateType ?? EnemyAIStateType.Patrol, newStateType);
            
            // 이벤트 발생
            OnStateChanged?.Invoke(oldState?.StateType ?? EnemyAIStateType.Patrol, newStateType);
            
            // AI 이벤트 시스템에도 통지
            ownerAI?.Events?.OnStateChanged?.SafeInvoke(oldState?.StateType ?? EnemyAIStateType.Patrol, newStateType);
            ownerAI?.Events?.OnStateEntered?.SafeInvoke(newStateType);
            
            LogManager.Log(LogCategory.Enemy, 
                $"상태 전환 완료: {oldState?.StateName ?? "None"} → {currentState.StateName}", ownerAI);
            
            return true;
        }
        catch (Exception e)
        {
            LogManager.LogError(LogCategory.Enemy, $"상태 전환 중 오류 발생: {e.Message}", ownerAI);
            return false;
        }
        finally
        {
            isTransitioning = false;
        }
    }
    
    /// <summary>
    /// 특정 상태로 전환 가능한지 확인
    /// </summary>
    /// <param name="toStateType">전환하려는 상태</param>
    /// <returns>전환 가능 여부</returns>
    public bool CanTransitionTo(EnemyAIStateType toStateType)
    {
        // 전환 중이면 불가
        if (isTransitioning) return false;
        
        // 최소 전환 간격 체크
        if (Time.time - lastTransitionTime < MinTransitionInterval) return false;
        
        // 현재 상태가 전환을 허용하는지 확인
        if (currentState != null && !currentState.CanTransitionTo(ownerAI, toStateType))
        {
            return false;
        }
        
        // 상태가 존재하는지 확인
        return stateInstances.ContainsKey(toStateType);
    }
    
    /// <summary>
    /// 이전 상태로 되돌리기
    /// </summary>
    /// <returns>되돌리기 성공 여부</returns>
    public bool RevertToPreviousState()
    {
        if (previousState != null)
        {
            return ChangeState(previousState.StateType);
        }
        
        LogManager.LogWarning(LogCategory.Enemy, "이전 상태가 없어 되돌릴 수 없습니다.", ownerAI);
        return false;
    }
    
    /// <summary>
    /// 상태 강제 리셋 (긴급 상황용)
    /// </summary>
    public void ForceReset()
    {
        LogManager.LogWarning(LogCategory.Enemy, "상태 머신 강제 리셋", ownerAI);
        
        isTransitioning = false;
        currentState?.Exit(ownerAI);
        ChangeState(EnemyAIStateType.Patrol, true);
    }
    
    /// <summary>
    /// 정리 작업
    /// </summary>
    public void Cleanup()
    {
        currentState?.Exit(ownerAI);
        
        OnStateChanged = null;
        OnStateEntered = null;
        OnStateExited = null;
        
        transitionHistory?.Clear();
        
        LogManager.Log(LogCategory.Enemy, "EnemyAIStateMachine 정리 완료", ownerAI);
    }
    
    /// <summary>
    /// 현재 상태 정보 가져오기
    /// </summary>
    /// <returns>상태 정보 문자열</returns>
    public string GetCurrentStateInfo()
    {
        if (currentState == null) return "No State";
        
        return $"{currentState.StateName} (진입 후 {Time.time - lastTransitionTime:F1}초)";
    }
    
    /// <summary>
    /// 전환 히스토리 가져오기
    /// </summary>
    /// <returns>전환 히스토리 배열</returns>
    public StateTransition[] GetTransitionHistory()
    {
        return transitionHistory.ToArray();
    }
    
    /// <summary>
    /// 디버깅용 정보 반환
    /// </summary>
    /// <returns>디버그 정보</returns>
    public string GetDebugInfo()
    {
        var info = $"현재: {currentState?.StateName ?? "None"}\n";
        info += $"이전: {previousState?.StateName ?? "None"}\n";
        info += $"전환 중: {isTransitioning}\n";
        info += $"히스토리: {transitionHistory.Count}/{MaxHistoryCount}\n";
        
        if (currentState != null)
        {
            info += $"상태 정보: {currentState.GetDebugInfo(ownerAI)}";
        }
        
        return info;
    }
    
    /// <summary>
    /// 기즈모 그리기 (현재 상태의 시각화)
    /// </summary>
    public void DrawGizmos()
    {
        currentState?.DrawGizmos(ownerAI);
    }
    
    // ===== Private Helper Methods =====
    
    /// <summary>
    /// 클래스 타입에서 상태 enum 추출
    /// </summary>
    private EnemyAIStateType GetStateTypeFromClass<T>() where T : EnemyAIState
    {
        return typeof(T).Name switch
        {
            nameof(PatrolState) => EnemyAIStateType.Patrol,
            nameof(ChaseState) => EnemyAIStateType.Chase,
            nameof(AttackState) => EnemyAIStateType.Attack,
            nameof(RetreatState) => EnemyAIStateType.Retreat,
            _ => EnemyAIStateType.Patrol
        };
    }
    
    /// <summary>
    /// 전환 히스토리 기록
    /// </summary>
    private void RecordTransition(EnemyAIStateType from, EnemyAIStateType to)
    {
        var transition = new StateTransition
        {
            fromState = from,
            toState = to,
            timestamp = Time.time
        };
        
        transitionHistory.Enqueue(transition);
        
        // 히스토리 크기 제한
        while (transitionHistory.Count > MaxHistoryCount)
        {
            transitionHistory.Dequeue();
        }
    }
}

/// <summary>
/// 상태 전환 기록 구조체
/// </summary>
[Serializable]
public struct StateTransition
{
    public EnemyAIStateType fromState;
    public EnemyAIStateType toState;
    public float timestamp;
    
    public override string ToString()
    {
        return $"{fromState} → {toState} ({timestamp:F2}s)";
    }
} 