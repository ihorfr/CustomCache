using System;
using System.Collections.Generic;
using System.Threading;

namespace SimplyMemoryCache
{
    class Program
    {
        static void Main(string[] args)
        {
            const int cacheTimeout = 2000;
            CustomCache<long, string> c = new CustomCache<long, string>();

            for (long i = 0; i < 1000000; i++)
                c.AddOrUpdate(i, i.ToString(), cacheTimeout);

            Thread.Sleep(100);

            var data = c.Get(34);
            var data34 = c.Get(34);

            Console.WriteLine(data);
            Console.WriteLine(data34 ?? "NULL");

            Console.ReadLine();
        }
    }

    public class CustomCache<TKey, TValue> where TValue : class
    {
        private readonly Dictionary<TKey, TValue> data = new Dictionary<TKey, TValue>();
        private Dictionary<TKey, DateTime> times = new Dictionary<TKey, DateTime>();
        private readonly ReaderWriterLockSlim rwLocker = new ReaderWriterLockSlim();
        
        public TValue Get(TKey key)
        {
            rwLocker.EnterReadLock();
            try
            {
                TValue val = data.TryGetValue(key, out val) ? val : default(TValue);
                CheckTime(key);
                return val;
            }
            finally { rwLocker.ExitReadLock(); }
        }

        public TValue this[TKey key] => Get(key);

        private bool CheckTime(TKey key)
        {
            bool isValid = false;
            DateTime time;
            //rwLocker.EnterReadLock();
            try
            {
                if (times.TryGetValue(key, out time))
                {
                    if (time < DateTime.Now)
                    {
                        data.Remove(key);
                        times.Remove(key);
                        isValid = false;
                    }
                    else
                    {
                        isValid = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return isValid;
        }

        public void AddOrUpdate(TKey key, TValue cacheObject, int timeout)
        {
            rwLocker.EnterWriteLock();
            try
            {
                if (!data.ContainsKey(key))
                {
                    data.Add(key, cacheObject);
                }
                else
                {
                    data[key] = cacheObject;
                }
                times.Add(key, DateTime.Now.AddMilliseconds(timeout));
            }
            finally { rwLocker.ExitWriteLock(); }
        }
    }
}
