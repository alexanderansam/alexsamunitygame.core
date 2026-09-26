using System;
using System.Collections.Generic;

namespace AlexSamGame.Core
{
    public class StateMachine
    {
        private readonly Dictionary<string, Func<BaseState>> stateFactories;
        private BaseState current = new BaseState(); // аналог self.empty

        public StateMachine(Dictionary<string, Func<BaseState>> states)
        {
            stateFactories = states;
        }

        public void Change(string stateName, object enterParams = null)
        {
            current.Exit();
            current = stateFactories[stateName](); // нет ключа → KeyNotFoundException, как assert в Lua
            current.Enter(enterParams);
        }

        public void UpdateState(float dt) => current.UpdateState(dt);
    }
}
