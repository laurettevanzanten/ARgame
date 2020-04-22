using System;
using System.Collections.Generic;

namespace PostStressTest.StateMachine
{
    public class BasicAgent : BasicState
    {
        public IoC AgentContext { get; private set; }
        private Dictionary<IState, Func< IState>> _stateMachine = new Dictionary<IState, Func<IState>>();

        private List<IState> _states = new List<IState>();

        public IState CurrentState
        {
            get;
            set;
        }
       
        public T AddState<T>(T state) where T : IState
        {
            _states.Add(state);
            return (T)state;
        }

        public T AddState<T>(Action<T> initializeState = null) where T : IState
        {
            var state = (T) Activator.CreateInstance(typeof(T));

            initializeState?.Invoke(state);

            _states.Add(state);
            return state;
        }

        public BasicAgent AddStateTransition(IState state, Func<IState> nextState)
        {
            _stateMachine[state] = nextState;
            return this;
        }


        protected override void OnInitialized()
        {
            AgentContext = new IoC()
                    .Register(IoC.GlobalIoCId, Context)
                    .Register<BasicAgent>(this)
                    .CopyRegisteredObjects(Context);

            InitializeAgentContext();

            foreach (var state in _states)
            {
                state.Initialize(AgentContext);
            }
        }

        protected virtual void InitializeAgentContext()
        {
        }

        protected override void OnStarted()
        {
            CurrentState.Start();
        }

        public override void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.Update();

                if (CurrentState.Phase == StatePhase.Stopped)
                {
                    if (_stateMachine.ContainsKey(CurrentState))
                    {
                        // get the next state
                        CurrentState = _stateMachine[CurrentState]();

                        if (CurrentState != null)
                        {
                            CurrentState.Start();
                        }
                        else
                        {
                            Stop();
                        }
                    }
                    else
                    {
                        // current state has no follow up
                        Stop();
                    }
                }
            }
        }

        protected override void OnStopped()
        {
            if (CurrentState != null && CurrentState.Phase == StatePhase.Started)
            {
                CurrentState.Stop();
            }
        }
    }
}
