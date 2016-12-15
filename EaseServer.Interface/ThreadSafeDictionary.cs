using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace EaseServer.Interface
{
    public interface IThreadSafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        void MergeSafe(TKey key, TValue newValue);

        void RemoveSafe(TKey key);
    }

	[Serializable]
	public class ThreadSafeDictionary<TKey, TValue> : IThreadSafeDictionary<TKey, TValue>, IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
	{
		private IDictionary<TKey, TValue> dict;

		[NonSerialized]
		private ReaderWriterLockSlim dictionaryLock = Locks.GetLockInstance(LockRecursionPolicy.NoRecursion);

		public virtual TValue this[TKey key]
		{
			get
			{
				TValue result;
				using (new ReadOnlyLock(this.dictionaryLock))
				{
					result = this.dict[key];
				}
				return result;
			}
			set
			{
				using (new WriteLock(this.dictionaryLock))
				{
					this.dict[key] = value;
				}
			}
		}

		public virtual ICollection<TKey> Keys
		{
			get
			{
				ICollection<TKey> result;
				using (new ReadOnlyLock(this.dictionaryLock))
				{
					result = new List<TKey>(this.dict.Keys);
				}
				return result;
			}
		}

		public virtual ICollection<TValue> Values
		{
			get
			{
				ICollection<TValue> result;
				using (new ReadOnlyLock(this.dictionaryLock))
				{
					result = new List<TValue>(this.dict.Values);
				}
				return result;
			}
		}

		public virtual int Count
		{
			get
			{
				int count;
				using (new ReadOnlyLock(this.dictionaryLock))
				{
					count = this.dict.Count;
				}
				return count;
			}
		}

		public virtual bool IsReadOnly
		{
			get
			{
				bool isReadOnly;
				using (new ReadOnlyLock(this.dictionaryLock))
				{
					isReadOnly = this.dict.IsReadOnly;
				}
				return isReadOnly;
			}
		}

		public ThreadSafeDictionary()
		{
			this.dict = new Dictionary<TKey, TValue>();
		}

		public ThreadSafeDictionary(IEqualityComparer<TKey> comparer)
		{
			this.dict = new Dictionary<TKey, TValue>(comparer);
		}

		public void RemoveSafe(TKey key)
		{
			using (new ReadLock(this.dictionaryLock))
			{
				if (this.dict.ContainsKey(key))
				{
					using (new WriteLock(this.dictionaryLock))
					{
						this.dict.Remove(key);
					}
				}
			}
		}

		public void MergeSafe(TKey key, TValue newValue)
		{
			using (new WriteLock(this.dictionaryLock))
			{
				if (this.dict.ContainsKey(key))
				{
					this.dict.Remove(key);
				}
				this.dict.Add(key, newValue);
			}
		}

		public virtual bool Remove(TKey key)
		{
			bool result;
			using (new WriteLock(this.dictionaryLock))
			{
				result = this.dict.Remove(key);
			}
			return result;
		}

		public virtual bool ContainsKey(TKey key)
		{
			bool result;
			using (new ReadOnlyLock(this.dictionaryLock))
			{
				result = this.dict.ContainsKey(key);
			}
			return result;
		}

		public virtual bool TryGetValue(TKey key, out TValue value)
		{
			bool result;
			using (new ReadOnlyLock(this.dictionaryLock))
			{
				result = this.dict.TryGetValue(key, out value);
			}
			return result;
		}

		public virtual void Clear()
		{
			using (new WriteLock(this.dictionaryLock))
			{
				this.dict.Clear();
			}
		}

		public virtual bool Contains(KeyValuePair<TKey, TValue> item)
		{
			bool result;
			using (new ReadOnlyLock(this.dictionaryLock))
			{
				result = this.dict.Contains(item);
			}
			return result;
		}

		public virtual void Add(KeyValuePair<TKey, TValue> item)
		{
			using (new WriteLock(this.dictionaryLock))
			{
				this.dict.Add(item);
			}
		}

		public virtual void Add(TKey key, TValue value)
		{
			using (new WriteLock(this.dictionaryLock))
			{
				this.dict.Add(key, value);
			}
		}

		public virtual bool Remove(KeyValuePair<TKey, TValue> item)
		{
			bool result;
			using (new WriteLock(this.dictionaryLock))
			{
				result = this.dict.Remove(item);
			}
			return result;
		}

		public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			using (new ReadOnlyLock(this.dictionaryLock))
			{
				this.dict.CopyTo(array, arrayIndex);
			}
		}

		public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			throw new NotSupportedException("Cannot enumerate a threadsafe dictionary.  Instead, enumerate the keys or values collection");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotSupportedException("Cannot enumerate a threadsafe dictionary.  Instead, enumerate the keys or values collection");
		}
	}


    public abstract class BaseLock : IDisposable
    {
        protected ReaderWriterLockSlim _Locks;

        public BaseLock(ReaderWriterLockSlim locks)
        {
            this._Locks = locks;
        }

        public abstract void Dispose();
    }

    public class ReadLock : BaseLock
    {
        public ReadLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetReadLock(this._Locks);
        }

        public override void Dispose()
        {
            Locks.ReleaseReadLock(this._Locks);
        }
    }

    public class ReadOnlyLock : BaseLock
    {
        public ReadOnlyLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetReadOnlyLock(this._Locks);
        }

        public override void Dispose()
        {
            Locks.ReleaseReadOnlyLock(this._Locks);
        }
    }

    public class WriteLock : BaseLock
    {
        public WriteLock(ReaderWriterLockSlim locks)
            : base(locks)
        {
            Locks.GetWriteLock(this._Locks);
        }

        public override void Dispose()
        {
            Locks.ReleaseWriteLock(this._Locks);
        }
    }

}
