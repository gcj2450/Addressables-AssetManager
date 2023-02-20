//namespace CoreFtp.Infrastructure.Caching
//{
//    using System;
//    using System.Collections;
//    using System.Collections.Generic;
//    using System.Threading.Tasks;

//    public class InMemoryCache : ICache
//    {
//        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

//        public InMemoryCache()
//        {
//            _cache = new Dictionary<string, object>();
//        }

//        public bool HasKey(string key)
//        {
//            return _cache.ContainsKey(key);
//        }

//        public object Get<T>(string key)
//        {
//            object outValue;
//            _cache.TryGetValue(key, out outValue);
//            return outValue;
//        }

//        public void Remove(string key)
//        {
//            _cache.Remove(key);
//        }

//        public T GetOrSet<T>(string key, Func<T> expression, TimeSpan expiresIn) where T : class
//        {
//            var found = Get<T>(key);
//            if (!Equals(found, default(T)))
//                return found;

//            var executed = expression.Invoke();
//            _cache.Set(key, executed, DateTime.Now + expiresIn);

//            return executed;
//        }

//        public void Add<T>(string key, T value, TimeSpan timespan) where T : class
//        {
//            _cache.Set(key, value, timespan);
//        }

//        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> expression, TimeSpan expiresIn) where T : class
//        {
//            var found = Get<T>(key);
//            if (found != null)
//                return await Task.FromResult(found);

//            var executed = await Task.Run(expression);
//            _cache.Set(key, executed, DateTime.Now + expiresIn);

//            return await Task.FromResult(executed);
//        }
//    }
//}