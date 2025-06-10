using UnityEngine;
using System.Collections;

public class PlayerControll : MonoBehaviour
{
    PlayerInputControll playerInputControll;
    PlayerState state;
    Transform tf;
    Rigidbody2D rd2D;
    private AgentUI agentUI;

    private float lookAngle;
    public Transform shotPivot;
    public Transform shotPoint;
    public GameObject bulletPrefab;

    private bool canShoot = true;
    private bool isReloading = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerInputControll = GetComponent<PlayerInputControll>();
        state = GetComponent<PlayerState>();
        agentUI = GetComponent<AgentUI>();
        tf = transform;
        rd2D = GetComponent<Rigidbody2D>();
        playerInputControll.movePerformedCallback += Move;
        playerInputControll.moveStopCallback += MoveStop;
        playerInputControll.lookPerformedCallback += Look;
        playerInputControll.attackCallback += AttackTrigger;
        playerInputControll.reloadCallback += ReloadTrigger;
    }

    void Move(Vector2 direction)
    {
        rd2D.linearVelocity = new Vector2(direction.x, direction.y) * PlayerState.speed;
    }
    void MoveStop()
    {
        rd2D.linearVelocity = Vector2.zero;
    }

    void Look(Vector2 position)
    {
        //Debug.Log("Look Position: " + position);    
        Vector2 targetVector = Vector2.zero;
        Vector2 pivotVector = Vector2.zero;
        switch (playerInputControll.controllerType)
        {
            case PlayerInputControll.ControllerType.Keyboard:
                Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(position);
                targetVector = worldMousePos - (Vector2)transform.position;
                break;
            case PlayerInputControll.ControllerType.Gamepad:
                if (position.sqrMagnitude > 0.001f)
                {
                    targetVector = position.normalized;
                }
                else
                {
                    return;
                }
                break;
        }

        lookAngle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg;
        shotPivot.rotation = Quaternion.Euler(new Vector3(0, 0, lookAngle));
    }

    private void AttackTrigger()
    {
        if (!canShoot || isReloading) return;

        GameObject bullet = Instantiate(bulletPrefab, shotPoint.position, Quaternion.Euler(new Vector3(0, 0, lookAngle)));
        Projectile projectile = bullet.GetComponent<Projectile>();
        projectile.Init(AgentState.bulletSpeed, AgentState.bulletDamage, AgentState.bulletRange, gameObject);

        state.UpdateBulletCount(-1);

        if (state.bulletCurrentCount <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        StartCoroutine(ShootDelay());
    }

    private IEnumerator ShootDelay()
    {
        canShoot = false;
        yield return new WaitForSeconds(AgentState.bulletDelay);
        canShoot = true;
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        agentUI.StartReloadUI();
        
        float reloadTimer = 0f;
        while (reloadTimer < AgentState.bulletReloadTime)
        {
            reloadTimer += Time.deltaTime;
            float progress = reloadTimer / AgentState.bulletReloadTime;
            agentUI.UpdateReloadProgress(progress);
            yield return null;
        }

        state.UpdateBulletCount(AgentState.bulletMaxCount);
        agentUI.EndReloadUI();
        isReloading = false;
    }

    private void ReloadTrigger()
    {
        if (!isReloading && state.bulletCurrentCount < AgentState.bulletMaxCount)
        {
            StartCoroutine(Reload());
        }
    }
}
