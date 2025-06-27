using UnityEngine;
using System.Collections;

public class SpawnerObject : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float spawnInterval = 5f; // 스폰 간격
    [SerializeField] private float spawnDelay = 0f; // 초기 스폰 지연 시간
    
    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] 스폰할 프리팹이 설정되지 않았습니다.");
            enabled = false;
            return;
        }

        // EnemyManager에 스포너 추가
        EnemyManager.Instance.AddSpawner();

        // 스폰 코루틴 시작
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // 초기 지연 시간 대기
        yield return new WaitForSeconds(spawnDelay);

        while (true)
        {
            // 최대 적 수량 체크
            if (EnemyManager.Instance.CanSpawnEnemy())
            {
                SpawnEnemy();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // 프리팹 생성
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        GameObject target = PlayerManager.Instance.GetPlayer();
        // EnemyControll 컴포넌트 가져오기
        EnemyControll enemyControll = enemy.GetComponent<EnemyControll>();
        if (enemyControll)
        {
            // 타겟 설정 및 초기화
            enemyControll.Init(target.gameObject,transform.position);
            // EnemyManager에 적 추가
            EnemyManager.Instance.AddEnemy();
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 생성된 적에 EnemyControll 컴포넌트가 없습니다.");
        }
    }

    private void OnDrawGizmos()
    {
        // 스포너 위치 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
