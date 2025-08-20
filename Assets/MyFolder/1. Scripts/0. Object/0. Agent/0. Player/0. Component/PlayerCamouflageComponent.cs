using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerCamouflageComponent : PlayerUpdateComponent
    {
        private PlayerControll controll;
        private PlayerNetworkSync playerSync;

        public override void Start(PlayerControll controll)
        {
            this.controll = controll;
            playerSync = controll.GetComponent<PlayerNetworkSync>();
        }
        public override void Stop(){}
        public override void SetKeyEvent(PlayerInputControll inputControll)
        {
            if (inputControll)
            {
                inputControll.skill_1StartCallback += KeyEnter;
                inputControll.skill_1StopCallback += KeyExit;
            }
        }

        public override void KeyEnter()
        {
            if(controll.GetColor() == Color.white)
            {
                playerSync.SetCanSee(false);
                controll.ColorChange(new Color(1, 0.259434f, 0.259434f, 1));
            }
            else
            {
                playerSync.SetCanSee(true);
                controll.ColorChange(Color.white);
            }
        }

        public override void KeyPress()
        {
        }

        public override void KeyExit()
        {
        }

        public override void Update(){}
        public override void FixedUpdate(){}
        public override void LateUpdate(){}
    }
}