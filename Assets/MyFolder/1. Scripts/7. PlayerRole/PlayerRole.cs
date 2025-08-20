using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._3._SingleTone;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    public class PlayerRole : NetworkBehaviour
    {
        private PlayerControll controll;
        private PlayerInputControll inputControll;
        private AgentStatus state;
        private PlayerRoleType type;
        private PlayerRoleDefinition definition;

        public override void OnStartClient()
        {
            if(IsOwner)
            {
                TryGetComponent(out inputControll);
                TryGetComponent(out state);
                if(!TryGetComponent(out controll))
                {
                    LogManager.LogError(LogCategory.System,$"controll이 없습니다 {controll}");
                }
                else
                {
                    if (!PlayerRoleManager.Instance)
                    {
                        LogManager.LogError(LogCategory.System, $"{gameObject.name} : PlayerRoleManager is Null");
                    }
                    else
                    {
                        type = PlayerRoleManager.instance.GetLocalPlayerRole();
                        definition = PlayerRoleManager.instance.GetDefinition(type);
                        SetState();
                        SetSkill();   
                    }
                }
            }
        }

        private void SetState()
        {
            state.SetDefinition(definition);
        }
        private void SetSkill()
        {
            inputControll.IsActive_skill_1 = definition.CanUseSkill1;
        }
    }
}