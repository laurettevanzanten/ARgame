using System.Collections.Generic;

namespace PostStressTest
{
    /// <summary>
    /// Run of the mill IoC implementation
    /// </summary>
    public class IoC
    {
        public const string GlobalIoCId = "global";

        private Dictionary<string, object> _objects = new Dictionary<string, object>();

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


        public T Obtain<T>(string id)
        {
            if (_objects.TryGetValue(id, out var result))
            {
                return (T)result;
            }
            return default(T);
        }

        public T Obtain<T>()
        {
            if (_objects.TryGetValue(typeof(T).Name, out var result))
            {
                return (T)result;
            }
            return default(T);
        }
    }
}
