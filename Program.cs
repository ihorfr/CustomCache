using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyMemoryCache
{
    class Program
    {
        static void Main(string[] args)
        {
            const int cacheLiveTime = 5000;
            CustomCache<long, string> c = new CustomCache<long, string>();

            for (long i = 0; i < 1000000; i++)
                c.AddOrUpdate(i, i.ToString(), cacheLiveTime);

            var data = c.Get(34);
            Console.WriteLine(data ?? "NULL");

            Thread.Sleep(5000);
            var data34 = c.Get(34);
            Console.WriteLine(data34 ?? "NULL");


            Thread.Sleep(1000);
            data34 = c.Get(34);
            Console.WriteLine(data34 ?? "NULL");

            Console.ReadLine();
        }
    }

    public class CustomCache<TKey, TValue> where TValue : class
    {
        private readonly Dictionary<TKey, CacheObject<TValue>> data = new Dictionary<TKey, CacheObject<TValue>>();
        private readonly ReaderWriterLockSlim rwLocker = new ReaderWriterLockSlim();

        public TValue Get(TKey key)
        {
            rwLocker.EnterReadLock();
            try
            {
                CacheObject<TValue> val = data.TryGetValue(key, out val) ? val : default(CacheObject<TValue>);

                if (val != null)
                {
                    // Инвалидация кеша
                    // Если необходимо инвалидировать весь кеш - запустить цикл с анализом невалидных ExpiredTime
                    new Task(() =>
                    {
                        if (val.ExpiredTime < DateTime.Now)
                        {
                            Console.WriteLine("Invalidation Key = " + key + "\t Value = " + val.CacheValue + "\t Time expired:" + val.ExpiredTime);
                            data.Remove(key);
                        }
                    }).Start();
                }
                else
                    return null;

                return val.CacheValue;
            }
            finally { rwLocker.ExitReadLock(); }
        }

        public TValue this[TKey key] => Get(key);

        public void AddOrUpdate(TKey key, TValue value, int timeout)
        {
            rwLocker.EnterWriteLock();
            try
            {
                CacheObject<TValue> cacheObject = new CacheObject<TValue>() { CacheValue = value, ExpiredTime = DateTime.Now.AddMilliseconds(timeout) };
                if (!data.ContainsKey(key))
                {
                    data.Add(key, cacheObject);
                }
                else
                {
                    data[key] = cacheObject;
                }
            }
            finally { rwLocker.ExitWriteLock(); }
        }
    }

    public class CacheObject<TCacheValue> where TCacheValue : class
    {
        public TCacheValue CacheValue { get; set; }
        public DateTime ExpiredTime { get; set; }
    }
}
