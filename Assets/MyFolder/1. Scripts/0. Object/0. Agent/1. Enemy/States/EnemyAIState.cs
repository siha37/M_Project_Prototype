using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using UnityEngine;

/// <summary>
/// 적 AI 상태의 추상 기본 클래스
/// State Pattern의 핵심 인터페이스 정의
/// 모든 구체적인 AI 상태는 이 클래스를 상속받아야 함
/// </summary>
public abstract class EnemyAIState
{
    /// <summary>
    /// 상태의 타입 (enum으로 타입 안전성 보장)
    /// </summary>
    public abstract EnemyAIStateType StateType { get; }
    
    /// <summary>
    /// 상태의 표시 이름 (디버깅용)
    /// </summary>
    public abstract string StateName { get; }
    
    /// <summary>
    /// 상태 진입 시 호출되는 메서드
    /// 상태 초기화 로직을 구현
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    public abstract void Enter(EnemyAI ai);
    
    /// <summary>
    /// 매 프레임 업데이트 시 호출되는 메서드
    /// 상태별 핵심 로직을 구현
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    public abstract void Update(EnemyAI ai);
    
    /// <summary>
    /// 상태 종료 시 호출되는 메서드
    /// 정리 작업을 구현
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    public abstract void Exit(EnemyAI ai);
    
    /// <summary>
    /// 상태별 고정 업데이트 (물리 연산용)
    /// 필요한 경우에만 오버라이드
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    public virtual void FixedUpdate(EnemyAI ai)
    {
        // 기본적으로는 아무것도 하지 않음
        // 물리 기반 이동이 필요한 상태에서만 오버라이드
    }
    
    /// <summary>
    /// 다른 상태로 전환 가능한지 확인
    /// 상태 전환 조건을 구현
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <param name="toState">전환하려는 상태 타입</param>
    /// <returns>전환 가능 여부</returns>
    public virtual bool CanTransitionTo(EnemyAI ai, EnemyAIStateType toState)
    {
        // 기본적으로는 모든 전환을 허용
        // 특정 상태에서 제한이 필요한 경우 오버라이드
        return true;
    }
    
    /// <summary>
    /// 상태 우선순위 반환 (동시에 여러 조건이 만족될 때 사용)
    /// 높은 값이 우선순위가 높음
    /// </summary>
    public virtual int GetPriority()
    {
        return 0; // 기본 우선순위
    }
    
    /// <summary>
    /// 디버깅용 정보 반환
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <returns>디버그 정보 문자열</returns>
    public virtual string GetDebugInfo(EnemyAI ai)
    {
        return $"{StateName} - 기본 상태 정보";
    }
    
    /// <summary>
    /// 상태별 기즈모 그리기 (Scene 뷰에서 시각화)
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    public virtual void DrawGizmos(EnemyAI ai)
    {
        // 기본적으로는 아무것도 그리지 않음
        // 필요한 상태에서만 오버라이드하여 시각화
    }
    
    /// <summary>
    /// 상태 전환 시 애니메이션 트리거 (필요한 경우)
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <param name="fromState">이전 상태</param>
    public virtual void OnTransitionFrom(EnemyAI ai, EnemyAIState fromState)
    {
        // 애니메이션 전환 로직을 여기에 구현
        // 필요한 상태에서만 오버라이드
    }
    
    /// <summary>
    /// 네트워크 동기화 데이터 생성
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <returns>동기화할 상태 데이터</returns>
    public virtual EnemyStateData CreateSyncData(EnemyAI ai)
    {
        if (ai == null) return EnemyStateData.Default;
        
        EnemyStateData data = new EnemyStateData((Vector2)ai.transform.position, StateType);
        
        // 전투 상태 설정
        if (ai.Combat != null)
        {
            data.lookAngle = ai.Combat.LookAngle;
        }
        
        // 타겟 정보 설정
        if (ai.CurrentTarget != null)
        {
            ai.CurrentTarget.TryGetComponent(out NetworkBehaviour playerObj);
            if (playerObj != null)
            {
                data.targetClientId = playerObj.Owner?.ClientId ?? -1;
                data.hasValidTarget = data.targetClientId >= 0;
            }
        }
        
        return data;
    }
    
    /// <summary>
    /// 상태 비교 (같은 상태인지 확인)
    /// </summary>
    /// <param name="other">비교할 상태</param>
    /// <returns>같은 상태 타입인지 여부</returns>
    public bool IsSameStateType(EnemyAIState other)
    {
        return other != null && StateType == other.StateType;
    }
    
    /// <summary>
    /// 상태 이름으로 비교
    /// </summary>
    /// <param name="stateName">비교할 상태 이름</param>
    /// <returns>같은 상태인지 여부</returns>
    public bool IsState(string stateName)
    {
        return StateName.Equals(stateName, System.StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    ///ToString 오버라이드 (디버깅용)
    /// </summary>
    /// <returns>상태 정보 문자열</returns>
    public override string ToString()
    {
        return $"{StateName}({StateType})";
    }
    
    // ===============================================
    // 헬퍼 메서드들 (하위 상태에서 공통으로 사용)
    // ===============================================
    
    /// <summary>
    /// 안전한 로그 출력
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <param name="message">로그 메시지</param>
    /// <param name="logLevel">로그 레벨</param>
    protected void Log(EnemyAI ai, string message, EnemyConfig.LogLevel logLevel = EnemyConfig.LogLevel.Info)
    {
        if (ai?.Config && (int)logLevel <= (int)ai.Config.logLevel)
        {
            string fullMessage = $"[{StateName}] {message}";
            
            switch (logLevel)
            {
                case EnemyConfig.LogLevel.Error:
                    LogManager.LogError(LogCategory.Enemy, fullMessage, ai);
                    break;
                case EnemyConfig.LogLevel.Warning:
                    LogManager.LogWarning(LogCategory.Enemy, fullMessage, ai);
                    break;
                case EnemyConfig.LogLevel.Info:
                case EnemyConfig.LogLevel.Verbose:
                default:
                    LogManager.Log(LogCategory.Enemy, fullMessage, ai);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 타겟과의 거리 확인
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <returns>타겟과의 거리 (타겟이 없으면 float.MaxValue)</returns>
    protected float GetDistanceToTarget(EnemyAI ai)
    {
        return ai?.CurrentTarget != null ? 
            Vector3.Distance(ai.transform.position, ai.CurrentTarget.position) : 
            float.MaxValue;
    }
    
    /// <summary>
    /// 설정값 가져오기 (null 체크 포함)
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <returns>설정 객체 (null이면 기본값 반환)</returns>
    protected EnemyConfig GetConfig(EnemyAI ai)
    {
        return ai?.Config;
    }
    
    /// <summary>
    /// 이벤트 시스템 가져오기 (null 체크 포함)
    /// </summary>
    /// <param name="ai">AI 컨트롤러 참조</param>
    /// <returns>이벤트 객체</returns>
    protected EnemyEvents GetEvents(EnemyAI ai)
    {
        return ai?.Events;
    }
} 