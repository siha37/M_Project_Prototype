using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerInputControll : MonoBehaviour
{
    //INPUTACTIONS
    PlayerInput playerinput;
    InputActionAsset inputActionAsset;
    InputAction move;
    InputAction look;
    InputAction attack;
    InputAction interact;
    InputAction reload;

    string Keyboard = "PlayerKeyboard";
    string Gamepad = "PlayerPad";
    public enum ControllerType
    {
        Keyboard,
        Gamepad
    }
    public ControllerType controllerType;

    public delegate void InputActionCallBack();
    public delegate void InputActionVector2CallBack(Vector2 vector2);

    public InputActionVector2CallBack movePerformedCallback;
    public InputActionCallBack moveStopCallback;
    public InputActionVector2CallBack lookPerformedCallback;
    public InputActionCallBack attackCallback;
    public InputActionCallBack reloadCallback;
    public InputActionCallBack interactStartCallback;
    public InputActionCallBack interactPerformedCallback;
    public InputActionCallBack interactCanceledCallback;

    private void Start()
    {
        playerinput = GetComponent<PlayerInput>();
        switch (controllerType)
        {
            case ControllerType.Keyboard:
                playerinput.SwitchCurrentActionMap(Keyboard);
                break;
            case ControllerType.Gamepad:
                playerinput.SwitchCurrentActionMap(Gamepad);
                break;
            default:
                break;
        }
        inputActionAsset = playerinput.actions;
        move = playerinput.currentActionMap.FindAction("Move");
        look = playerinput.currentActionMap.FindAction("Look");
        attack = playerinput.currentActionMap.FindAction("Attack");
        interact = playerinput.currentActionMap.FindAction("Interact");
        reload = playerinput.currentActionMap.FindAction("Reload");

        move.performed += MovePerformed;
        move.canceled += MoveCancle;
        look.performed += LookPerformed;
        attack.started += AttackStart;
        attack.canceled += AttackCancel;
        interact.started += InteractStart;
        interact.performed += InteractPerformed;
        interact.canceled += InteractCanceled;
        reload.started += ReloadStart;
    }

    private void OnEnable()
    {
        if(playerinput != null)
        {
            playerinput.enabled = true;
        }
    }

    private void OnDisable()
    {
        playerinput.enabled = false;
    }

    private bool isAttacking = false;

    private void AttackStart(InputAction.CallbackContext context)
    {
        isAttacking = true;
        StartCoroutine(AttackLoop());
    }

    private void AttackCancel(InputAction.CallbackContext context)
    {
        isAttacking = false;
    }

    private IEnumerator AttackLoop()
    {
        while (isAttacking)
        {
            attackCallback?.Invoke();
            yield return null;
        }
    }

    private void MovePerformed(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        movePerformedCallback?.Invoke(inputVector);
    }
    private void MoveCancle(InputAction.CallbackContext context)
    {
        moveStopCallback?.Invoke();
    }
    private void LookPerformed(InputAction.CallbackContext context)
    {
        lookPerformedCallback?.Invoke(context.ReadValue<Vector2>());
    }

    private void InteractStart(InputAction.CallbackContext context)
    {
        interactStartCallback?.Invoke();
    }
    private void InteractPerformed(InputAction.CallbackContext context)
    {
        interactPerformedCallback?.Invoke();
    }
    private void InteractCanceled(InputAction.CallbackContext context)
    {
        interactCanceledCallback?.Invoke();
    }

    private void ReloadStart(InputAction.CallbackContext context)
    {
        reloadCallback?.Invoke();
    }
}
