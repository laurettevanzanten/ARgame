namespace PostStressTest.StateMachine
{
    public class BasicState : IState
    {
        public IoC Context { get; private set; }

        public StatePhase Phase { get; private set; }

        public void Initialize(IoC context)
        {
            Context = context;

            Phase = StatePhase.Initialized;

            OnInitialized();
        }

        protected virtual void OnInitialized()
        {
        }

        public void Start()
        {
            Phase = StatePhase.Started;
            OnStarted();
        }

        protected virtual void OnStarted()
        {
        }

        public void Stop()
        {
            Phase = StatePhase.Stopped;
            OnStopped();
        }

        protected virtual void OnStopped()
        {
        }

        public virtual void Update()
        {
        }
    }
}
