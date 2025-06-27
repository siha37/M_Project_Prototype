using UnityEngine;

public class EnemyManager : SingleTone<EnemyManager>
{
    private int currentEnemyCount = 0;
    private int maxEnemyCount = 0;

    public int CurrentEnemyCount => currentEnemyCount;
    public int MaxEnemyCount => maxEnemyCount;

    public void AddSpawner()
    {
        maxEnemyCount += 5;
        Debug.Log($"[{gameObject.name}] 스포너 추가됨. 최대 적 수량: {maxEnemyCount}");
    }

    public void RemoveSpawner()
    {
        maxEnemyCount = Mathf.Max(0, maxEnemyCount - 5);
        Debug.Log($"[{gameObject.name}] 스포너 제거됨. 최대 적 수량: {maxEnemyCount}");
    }

    public bool CanSpawnEnemy()
    {
        return currentEnemyCount < maxEnemyCount;
    }

    public void AddEnemy()
    {
        currentEnemyCount++;
        Debug.Log($"[{gameObject.name}] 적 생성됨. 현재 적 수량: {currentEnemyCount}/{maxEnemyCount}");
    }

    public void RemoveEnemy()
    {
        currentEnemyCount = Mathf.Max(0, currentEnemyCount - 1);
        Debug.Log($"[{gameObject.name}] 적 제거됨. 현재 적 수량: {currentEnemyCount}/{maxEnemyCount}");
    }
}
