using System;
using System.Collections.Concurrent;

namespace PostStressTest
{
    /// <summary>
    /// Run of the mill IoC implementation
    /// </summary>
    public class IoC
    {
        public const string GlobalIoCId = "global";

        private ConcurrentDictionary<string, object> _objects = new ConcurrentDictionary<string, object>();

        public IoC CopyRegisteredObjects(IoC other)
        {
            foreach (var kvp in other._objects)
            {
                _objects[kvp.Key] = kvp.Value;
            }

            return this;
        }

        public IoC Register(string id, object value)
        {
            _objects[id] = value;
            return this;
        }

        public IoC Register<T>(object value)
        {
            return Register(typeof(T).Name, value);
        }

        public IoC Register<T>()
        {
            return Register(typeof(T).Name, Activator.CreateInstance<T>());
        }

        public T Resolve<T>(string id)
        {
            if (_objects.TryGetValue(id, out var result))
            {
                return (T)result;
            }
            return default(T);
        }

        public T Resolve<T>()
        {
            if (_objects.TryGetValue(typeof(T).Name, out var result))
            {
                return (T)result;
            }
            return default(T);
        }

        public void Remove(string id)
        {
            _objects.TryRemove(id, out var dummy);
        }
    }
}
