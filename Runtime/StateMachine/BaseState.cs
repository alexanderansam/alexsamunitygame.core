namespace AlexSamGame.Core
{
    public class BaseState
    {
        public virtual void Enter(object enterParams) { }
        public virtual void Exit() { }
        public virtual void UpdateState(float dt) { }
    }
}
