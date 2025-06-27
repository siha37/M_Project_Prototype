using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
public class EnemyNetwork : NetworkBehaviour
{
    [SerializeField] private EnemyControll enemyControll;
    [SerializeField] private NavMeshAgent navAgent;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (enemyControll == null)
            enemyControll = GetComponent<EnemyControll>();
        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsServer)
        {
            if (enemyControll == null)
                enemyControll = GetComponent<EnemyControll>();
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>();
            if (enemyControll != null)
                enemyControll.enabled = false;
            if (navAgent != null)
                navAgent.enabled = false;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            SyncTransform(transform.position, transform.rotation);
        }
    }

    [ObserversRpc]
    private void SyncTransform(Vector3 position, Quaternion rotation)
    {
        if (IsServer)
            return;
        transform.SetPositionAndRotation(position, rotation);
    }
}
