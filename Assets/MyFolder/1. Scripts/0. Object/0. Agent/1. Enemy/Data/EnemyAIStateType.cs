/// <summary>
/// 적 AI 상태 열거형
/// 기존 string 기반 상태를 타입 안전한 enum으로 대체
/// </summary>
public enum EnemyAIStateType
{
    /// <summary>
    /// 순찰 상태 - 기본 이동 및 경계
    /// </summary>
    Patrol = 0,
    
    /// <summary>
    /// 추적 상태 - 타겟을 발견하여 추적 중
    /// </summary>
    Chase = 1,
    
    /// <summary>
    /// 공격 상태 - 사격 범위 내에서 공격 수행
    /// </summary>
    Attack = 2,
    
    /// <summary>
    /// 후퇴 상태 - 타겟과 거리 조절을 위한 후퇴
    /// </summary>
    Retreat = 3
}

/// <summary>
/// AI 상태 유틸리티 확장 메서드
/// </summary>
public static class EnemyAIStateTypeExtensions
{
    /// <summary>
    /// 상태를 문자열로 변환 (디버깅용)
    /// </summary>
    public static string ToDisplayString(this EnemyAIStateType state)
    {
        return state switch
        {
            EnemyAIStateType.Patrol => "순찰",
            EnemyAIStateType.Chase => "추적",
            EnemyAIStateType.Attack => "공격",
            EnemyAIStateType.Retreat => "후퇴",
            _ => "알 수 없음"
        };
    }
    
    /// <summary>
    /// 문자열을 상태로 변환 (기존 코드 호환성용)
    /// </summary>
    public static EnemyAIStateType FromString(string stateString)
    {
        return stateString?.ToLower() switch
        {
            "patrol" => EnemyAIStateType.Patrol,
            "chase" => EnemyAIStateType.Chase,
            "attack" => EnemyAIStateType.Attack,
            "retreat" or "return" => EnemyAIStateType.Retreat,
            _ => EnemyAIStateType.Patrol
        };
    }
    
    /// <summary>
    /// 상태가 전투 관련 상태인지 확인
    /// </summary>
    public static bool IsCombatState(this EnemyAIStateType state)
    {
        return state == EnemyAIStateType.Attack || state == EnemyAIStateType.Chase;
    }
    
    /// <summary>
    /// 상태가 이동 관련 상태인지 확인
    /// </summary>
    public static bool IsMovementState(this EnemyAIStateType state)
    {
        return state == EnemyAIStateType.Chase || state == EnemyAIStateType.Retreat || state == EnemyAIStateType.Patrol;
    }
} 