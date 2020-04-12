namespace PostStressTest.StateMachine
{
    public enum StatePhase
    {
        Created,
        Initialized,
        Started,
        Stopped
    }

    public interface IState
    {
        StatePhase Phase { get; }

        IoC Context { get; }

        void Initialize(IoC context);

        void Start();

        void Update();

        void Stop();
    }
}
