using System;
using System.Threading;

namespace EaseServer.Interface
{
	public static class Locks
	{
		public static void GetReadLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
			{
				lockAcquired = locks.TryEnterUpgradeableReadLock(1);
			}
		}

		public static void GetReadOnlyLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
			{
				lockAcquired = locks.TryEnterReadLock(1);
			}
		}

		public static void GetWriteLock(ReaderWriterLockSlim locks)
		{
			bool lockAcquired = false;
			while (!lockAcquired)
			{
				lockAcquired = locks.TryEnterWriteLock(1);
			}
		}

		public static void ReleaseReadOnlyLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsReadLockHeld)
			{
				locks.ExitReadLock();
			}
		}

		public static void ReleaseReadLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsUpgradeableReadLockHeld)
			{
				locks.ExitUpgradeableReadLock();
			}
		}

		public static void ReleaseWriteLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsWriteLockHeld)
			{
				locks.ExitWriteLock();
			}
		}

		public static void ReleaseLock(ReaderWriterLockSlim locks)
		{
			Locks.ReleaseWriteLock(locks);
			Locks.ReleaseReadLock(locks);
			Locks.ReleaseReadOnlyLock(locks);
		}

		public static ReaderWriterLockSlim GetLockInstance()
		{
			return Locks.GetLockInstance(LockRecursionPolicy.SupportsRecursion);
		}

		public static ReaderWriterLockSlim GetLockInstance(LockRecursionPolicy recursionPolicy)
		{
			return new ReaderWriterLockSlim(recursionPolicy);
		}
	}
}
