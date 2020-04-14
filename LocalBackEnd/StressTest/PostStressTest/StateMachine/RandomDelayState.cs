using System;

namespace PostStressTest.StateMachine
{
    public class RandomDelayState : BasicState
    {
        public int MinDelayMs { get; set; }

        public int MaxDelayMs { get; set; }

        private int _delayMs;
        private DateTime _startTime;

        protected override void OnStarted()
        {
            _delayMs = Context.Resolve<Random>().Next(MinDelayMs, MaxDelayMs);
            _startTime = DateTime.Now;
        }

        public override void Update()
        {
            if ((DateTime.Now - _startTime).TotalMilliseconds > _delayMs)
            {
                Stop();
            }
        }
    }
}
