namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerCamouflageComponent : PlayerUpdateComponent
    {
        public override void Start(){}
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
            
        }

        public override void KeyPress()
        {
            
        }

        public override void KeyExit() { }

        public override void Update(){}
        public override void FixedUpdate(){}
        public override void LateUpdate(){}
    }
}