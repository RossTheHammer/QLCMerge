using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLCMerge.Common
{
    public class ObservableDictionary<T,V> : IObservable<KeyValuePair<T,V>>, IDictionary<T,V>
    {
        private readonly Dictionary<T,V> _dictionary = new Dictionary<T,V>();
        //private IList<IObserver<KeyValuePair<T, V>>> _observers = new List<IObserver<KeyValuePair<T, V>>>();

        public V this[T key] 
        { 
            get => _dictionary.GetValueOrDefault(key); 
            set => _dictionary[key] = value; 
        }

        public ICollection<T> Keys => _dictionary.Keys;

        public ICollection<V> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(T key, V value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<T, V> item)
        {
            _dictionary.Add(item.Key, item.Value);
            //foreach (var observer in _observers)
            //{
            //    observer.
            //}
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<T, V> item) => _dictionary.Contains(item);

        public bool ContainsKey(T key) => _dictionary.ContainsKey(key);

        public void CopyTo(KeyValuePair<T, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<T, V>> GetEnumerator() => _dictionary.GetEnumerator();

        public bool Remove(T key) => _dictionary.Remove(key);

        public bool Remove(KeyValuePair<T, V> item) => _dictionary.Remove(item.Key);

        public IDisposable Subscribe(IObserver<KeyValuePair<T, V>> observer)
        {
            //_observers.Add(observer);
            throw new NotImplementedException();
        }

        public bool TryGetValue(T key, out V value) => _dictionary.TryGetValue(key, out value);
        

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
