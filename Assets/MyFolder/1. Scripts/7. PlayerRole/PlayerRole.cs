namespace MyFolder._1._Scripts._7._PlayerRole
{
    public class PlayerRole : NetworkBehaviour
    {
        private PlayerControll controll;
        private AgentStatus state;
        private PlayerRoleType type;
        private RoleDefinition definition;

        public override void OnStartClient()
        {
            if(IsOwner)
            {
                if(!TryGetComponent(out controll))
                {
                    LogManager.LogError(LogCategory.System,$"controll이 없습니다 {controll}");
                }
                type = PlayerRoleManager.instance.GetLocalPlayerRole();
                definition = PlayerRoleManager.instance.GetDefinition(type);
            }
        }

        private void SetState()
        {
            state.SetDefinition(definition);
        }
        private void SetSkill()
        {

        }
    }
}