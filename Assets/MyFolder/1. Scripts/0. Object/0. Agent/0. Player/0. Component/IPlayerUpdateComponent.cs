using UnityEngine;

public interface IPlayerUpdateComponent : IPlayerComponent
{
    public void Update();
    public void FixedUpdate();
    public void LateUpdate();
}