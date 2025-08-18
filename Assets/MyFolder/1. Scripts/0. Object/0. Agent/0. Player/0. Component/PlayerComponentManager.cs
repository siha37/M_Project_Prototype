using UnityEngine;

public class PlayerComponentManager : MonoBehaviour
{
    List<IPlayerComponent> components;
    List<IPlayerUpdateComponent> update_components;
    private void Start()
    {
    }

    private void ComponentInit()
    {
        components.Clear();
        components.Add(new PlayerCamouflageComponent());
        
        update_components.Clear();
        components.ForEach(com=> {
            if(com is IPlayerUpdateComponent as up_com)
            {
                update_components.Add(up_com);
            }
        });
    }


    private void Update()
    {
        update_components.ForEach(com=>com.Update());
    }
    private void FixedUpdate()
    {
        update_components.ForEach(com=>com.FixedUpdate());
    }
    private void LateUpdate()
    {
        update_components.ForEach(com=>com.FixedUpdate());
    }
}