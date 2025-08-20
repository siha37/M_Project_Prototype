using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerInputControll : NetworkBehaviour
    {
        //INPUTACTIONS
        PlayerInput playerinput;
        InputAction move;
        InputAction look;
        InputAction attack;
        InputAction interact;
        InputAction reload;
        InputAction skill_1;

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
        public InputActionCallBack skill_1StartCallback;
        public InputActionCallBack skill_1StopCallback;

        public bool IsActive_skill_1 = false;

        public override void OnStartClient()
        {
            playerinput = GetComponent<PlayerInput>();
            if (!IsOwner)
            {
                playerinput.enabled = false;
            }
            else
            {
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
                RegisterInputActions();
            }
            
            
        }

        private void OnEnable()
        {
            RegisterInputActions();
        }

        private void OnDisable()
        {
            UnregisterInputActions();
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

        private void Skill1Start(InputAction.CallbackContext context)
        {
            if(!IsActive_skill_1)
                return;
            skill_1StartCallback?.Invoke();
        }

        private void Skill1Stop(InputAction.CallbackContext context)
        {
            skill_1StopCallback?.Invoke();
        }

        public override void OnStopClient()
        {
            UnregisterInputActions();
        }

        private void OnDestroy()
        {
            UnregisterInputActions();
        }

        private void RegisterInputActions()
        {
            if (!playerinput || !playerinput.enabled)
                return;
            move = playerinput.currentActionMap.FindAction("Move");
            look = playerinput.currentActionMap.FindAction("Look");
            attack = playerinput.currentActionMap.FindAction("Attack");
            interact = playerinput.currentActionMap.FindAction("Interact");
            reload = playerinput.currentActionMap.FindAction("Reload");
            skill_1 = playerinput.currentActionMap.FindAction("Skill_1");
            
            if (move != null)
            {
                move.performed += MovePerformed;
                move.canceled += MoveCancle;
            }
        
            if (look != null)
            {
                look.performed += LookPerformed;
            }
        
            if (attack != null)
            {
                attack.started += AttackStart;
                attack.canceled += AttackCancel;
            }
        
            if (interact != null)
            {
                interact.started += InteractStart;
                interact.performed += InteractPerformed;
                interact.canceled += InteractCanceled;
            }
        
            if (reload != null)
            {
                reload.started += ReloadStart;
            }

            if (skill_1 != null)
            {
                skill_1.started += Skill1Start;
                skill_1.canceled += Skill1Stop;
            }
        }
        private void UnregisterInputActions()
        {
            if (!playerinput || !playerinput.enabled)
                return;
            if (move != null)
            {
                move.performed -= MovePerformed;
                move.canceled -= MoveCancle;
            }
        
            if (look != null)
            {
                look.performed -= LookPerformed;
            }
        
            if (attack != null)
            {
                attack.started -= AttackStart;
                attack.canceled -= AttackCancel;
            }
        
            if (interact != null)
            {
                interact.started -= InteractStart;
                interact.performed -= InteractPerformed;
                interact.canceled -= InteractCanceled;
            }
        
            if (reload != null)
            {
                reload.started -= ReloadStart;
            }
            
            if (skill_1 != null)
            {
                skill_1.started -= Skill1Start;
                skill_1.canceled -= Skill1Stop;
            }
        }
    }
}
