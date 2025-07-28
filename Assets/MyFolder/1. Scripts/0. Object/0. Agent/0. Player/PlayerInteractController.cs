using UnityEngine;
using System.Collections;

public class PlayerInteractController : MonoBehaviour
{
    [SerializeField] private IntractArea interactArea;
    private PlayerInputControll playerInput;
    private GameObject currentInteractableObject;
    private Coroutine reviveCoroutine;
    private AgentUI agentUI;

    private void Start()
    {
        playerInput = GetComponent<PlayerInputControll>();
        agentUI = GetComponent<AgentUI>();

        if (interactArea == null)
        {
            LogManager.LogError(LogCategory.Player, $"{gameObject.name} IntractArea 컴포넌트가 없습니다.", this);
            enabled = false;
            return;
        }

        if (playerInput == null)
        {
            LogManager.LogError(LogCategory.Player, $"{gameObject.name} PlayerInputControll 컴포넌트가 없습니다.", this);
            enabled = false;
            return;
        }

        if (agentUI == null)
        {
            LogManager.LogError(LogCategory.Player, $"{gameObject.name} AgentUI 컴포넌트가 없습니다.", this);
            enabled = false;
            return;
        }

        // 입력 이벤트 연결
        playerInput.interactStartCallback += OnInteractStart;
        playerInput.interactPerformedCallback += OnInteractPerformed;
        playerInput.interactCanceledCallback += OnInteractCanceled;
    }

    private void OnDestroy()
    {
        // 이벤트 연결 해제
        if (playerInput != null)
        {
            playerInput.interactStartCallback -= OnInteractStart;
            playerInput.interactPerformedCallback -= OnInteractPerformed;
            playerInput.interactCanceledCallback -= OnInteractCanceled;
        }
    }

    private void OnInteractStart()
    {
        // 가장 가까운 상호작용 가능한 오브젝트 찾기
        currentInteractableObject = interactArea.GetNearestObject();
        
        if (currentInteractableObject != null)
        {
            if (currentInteractableObject.CompareTag("Object"))
            {
                // Object와 상호작용
                IInteractable interactable = currentInteractableObject.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(gameObject);
                }
            }
            else if (currentInteractableObject.CompareTag("Player"))
            {
                // Player의 상태 확인
                PlayerState playerState = currentInteractableObject.GetComponent<PlayerState>();
                if (playerState != null && playerState.IsDead)
                {
                    // 네트워크 동기화된 부활 처리
                    PlayerNetworkSync targetNetworkSync = currentInteractableObject.GetComponent<PlayerNetworkSync>();
                    if (targetNetworkSync != null)
                    {
                        // 이전 부활 코루틴이 있다면 중지
                        if (reviveCoroutine != null)
                        {
                            StopCoroutine(reviveCoroutine);
                        }
                        // 새로운 부활 처리 시작
                        reviveCoroutine = StartCoroutine(RevivePlayerNetwork(targetNetworkSync));
                    }
                    else
                    {
                        // 폴백: 기존 방식
                        if (reviveCoroutine != null)
                        {
                            StopCoroutine(reviveCoroutine);
                        }
                        reviveCoroutine = StartCoroutine(RevivePlayer(playerState));
                    }
                }
            }
        }
    }

    private IEnumerator RevivePlayer(PlayerState playerState)
    {
        float elapsedTime = 0f;
        agentUI.StartReviveUI();

        while (elapsedTime < PlayerState.reviveDelay)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / PlayerState.reviveDelay;
            agentUI.UpdateReviveProgress(progress);
            yield return null;
        }

        // 부활 처리
        playerState.Revive();
        agentUI.EndReviveUI();
        reviveCoroutine = null;
    }
    
    private IEnumerator RevivePlayerNetwork(PlayerNetworkSync targetNetworkSync)
    {
        float elapsedTime = 0f;
        agentUI.StartReviveUI();

        while (elapsedTime < PlayerState.reviveDelay)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / PlayerState.reviveDelay;
            agentUI.UpdateReviveProgress(progress);
            yield return null;
        }

        // 네트워크 동기화된 부활 처리
        targetNetworkSync.RequestRevive();
        agentUI.EndReviveUI();
        reviveCoroutine = null;
    }

    private void OnInteractPerformed()
    {
        // 상호작용 수행 중 처리
    }

    private void OnInteractCanceled()
    {
        // 상호작용 취소 시 처리
        if (reviveCoroutine != null)
        {
            StopCoroutine(reviveCoroutine);
            reviveCoroutine = null;
            agentUI.EndReviveUI();
        }
        currentInteractableObject = null;
    }
}
