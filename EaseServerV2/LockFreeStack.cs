using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace EaseServer
{
    /// <summary>
    /// 无锁的Stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>http://www.cnblogs.com/overred/archive/2009/09/30/Lock-Free-Stack.html</remarks>
    public class LockFreeStack<T> : IEnumerable<T>
    {

        #region Fields

        private Node _nodeList;

        #endregion

        #region Methods

        public T Pop()
        {
            try
            {
                Node n;
                do
                {
                    n = _nodeList;
                    if (n == null) throw new ArgumentNullException("stack empty!");
                }
                while (Interlocked.CompareExchange(ref _nodeList, n.Next, n) != n);
                return n.Value;
            }
            finally
            {
                Interlocked.Decrement(ref _count);
            }
        }

        public void Push(T value)
        {
            try
            {
                Node n = new Node();
                n.Value = value;

                Node o;
                do
                {
                    o = _nodeList;
                    n.Next = o;
                }
                while (Interlocked.CompareExchange(ref _nodeList, n, o) != o);
            }
            finally
            {
                Interlocked.Increment(ref _count);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            while (_nodeList != null)
            {
                yield return _nodeList.Value;
                _nodeList = _nodeList.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Interlocked.Exchange(ref _count, 0);
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        #endregion

        #region Attribute

        private long _count;
        public long Count
        {
            get { return Interlocked.Read(ref _count); }
            set { _count = value; }
        }

        #endregion

        private class Node
        {
            internal T Value;
            internal Node Next;
        }
    }

    /// <summary>
    /// 无锁的Queue
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LockFreeQueue<T> : IEnumerable<T>
    {
        class Node
        {
            internal T Value;
            internal Node Next;
        }

        private Node _head;
        private Node _tail;

        public LockFreeQueue()
        {
            _head = _tail = new Node();
        }

        public int Count
        {
            get
            {
                int count = 0;
                for (Node curr = _head.Next; curr != null; curr = curr.Next)
                    count++;
                return count;
            }
        }

        /// <summary>
        /// 判断当前队列是否为空
        /// </summary>
        public bool IsEmpty
        {
            get { return _head.Next == null; }
        }

        private Node GetTailAndCatchUp()
        {
            Node tail = _tail;
            Node next = tail.Next;

            // Update the tail until it really points to the end.
            while (next != null)
            {
                Interlocked.CompareExchange(ref _tail, next, tail);
                tail = _tail;
                next = tail.Next;
            }

            return tail;
        }

        /// <summary>
        /// 添加到尾部
        /// </summary>
        /// <param name="obj"></param>
        public void Enqueue(T obj)
        {
            // Create a new node.
            Node newNode = new Node();
            newNode.Value = obj;

            // Add to the tail end.
            Node tail;
            do
            {
                tail = GetTailAndCatchUp();
                newNode.Next = tail.Next;
            }
            while (Interlocked.CompareExchange(ref tail.Next, newNode, null) != null);

            // Try to swing the tail. If it fails, we'll do it later.
            Interlocked.CompareExchange(ref _tail, newNode, tail);
        }

        /// <summary>
        /// 取出队列中的第一个元素
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool TryDequeue(out T val)
        {
            while (true)
            {
                Node head = _head;
                Node next = head.Next;

                if (next == null)
                {
                    val = default(T);
                    return false;
                }
                else
                {
                    if (Interlocked.CompareExchange(ref _head, next, head) == head)
                    {
                        // Note: this read would be unsafe with a C++
                        // implementation. Another thread may have dequeued
                        // and freed 'next' by the time we get here, at
                        // which point we would try to dereference a bad
                        // pointer. Because we're in a GC-based system,
                        // we're OK doing this -- GC keeps it alive.
                        val = next.Value;
                        return true;
                    }
                }
            }
        }

        public bool TryPeek(out T val)
        {
            Node curr = _head.Next;
            if (curr == null)
            {
                val = default(T);
                return false;
            }
            else
            {
                val = curr.Value;
                return true;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            Node curr = _head.Next;
            Node tail = GetTailAndCatchUp();
            while (curr != null)
            {
                yield return curr.Value;

                if (curr == tail)
                    break;
                curr = curr.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
