using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Dict
{
    public class Benchmarks
    {
        private const int Count = 1000000;
        private readonly Table<string, int> _fastDictionary;
        private readonly Dictionary<string, int> _dictionary;
        private readonly ConcurrentDictionary<string, int> _concurrentDictionaryDictionary;
        public Benchmarks()
        {
            _fastDictionary = new Table<string, int>(Enumerable.Range(0, Count).Select(i => new Pair<string, int>(i.ToString(), i)).ToArray());
            var items = Enumerable.Range(0, Count).Select(i => new KeyValuePair<string, int>(i.ToString(), i)).ToArray();
            _dictionary = new Dictionary<string, int>(items);
            _concurrentDictionaryDictionary = new ConcurrentDictionary<string, int>(items);
        }
        
        [Benchmark]
        public void FastDictionary()
        {
            _fastDictionary.Get("323234");
        }

        [Benchmark]
        public void Dictionary()
        {
            _dictionary.TryGetValue("323234", out _);
        }
        
        [Benchmark]
        public void ConcurrentDictionary()
        {
            _concurrentDictionaryDictionary.TryGetValue("323234", out _);
        }
    }
    
    internal class Pair<TKey, TValue>
    {
        public readonly TKey Key;
        public readonly TValue Value;
        public Pair<TKey, TValue> Next;

        public Pair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
    
    internal class Table<TKey, TValue>
    {
        protected readonly uint Divisor;
        protected readonly Pair<TKey, TValue>[] Buckets;

        public static uint GetDivisor(int count)
        {
            return ((uint)count + 1) << 2;
        }

        public Table(Pair<TKey, TValue>[] pairs)
        {
            Divisor = GetDivisor(pairs.Length);
            Buckets = new Pair<TKey, TValue>[Divisor];
            var emptyPair = new Pair<TKey, TValue>(default(TKey), default(TValue));
            for (var i = 0; i < Buckets.Length; i++)
            {
                Buckets[i] = emptyPair;
            }

            var buckets = System.Linq.Enumerable.Select(System.Linq.Enumerable.GroupBy(pairs, pair => (uint)pair.Key.GetHashCode() % Divisor), groups => new
            {
                number = groups.Key,
                pairs = System.Linq.Enumerable.ToArray(groups)
            });

            foreach (var bucket in buckets)
            {
                Buckets[bucket.number] = bucket.pairs[0];
                for (var index = 1; index < bucket.pairs.Length; index++)
                {
                    bucket.pairs[index - 1].Next = bucket.pairs[index];
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl((System.Runtime.CompilerServices.MethodImplOptions)0x300)]
        public TValue Get(TKey key)
        {
            var pair = Buckets[(uint)key.GetHashCode() % Divisor];
            do
            {
                if (Equals(pair.Key, key))
                {
                    return pair.Value;
                }

                pair = pair.Next;
            } while (pair != null);

            return default(TValue);
        }
    }
}
