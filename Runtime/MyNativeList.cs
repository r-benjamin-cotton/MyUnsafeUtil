using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MyUnsafeUtil
{
    public class MyNativeList<T> : IList<T>, IDisposable, IEnumerable<T>, IEnumerable where T : unmanaged
    {
        private bool disposedValue;
        private NativeArray<T> array;
        private readonly unsafe T* ptr;
        //private T[] array;
        private int writePos;

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private MyNativeList<T> list;
            private int readPos;
            private T value;
            public Enumerator(MyNativeList<T> list)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (list == null)
                {
                    throw new ArgumentNullException("list");
                }
#endif
                this.list = list;
                readPos = -1;
                value = default;
            }
            public readonly T Current => value;
            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                list = null;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var len = list.Count;
                var pos = ++readPos;
                if (pos < len)
                {
                    value = list[pos];
                    return true;
                }
                else
                {
                    readPos = len;
                    value = default;
                    return false;
                }
            }
            public void Reset()
            {
                readPos = -1;
            }
        }

        public MyNativeList(int capacity)
        {
            array = new NativeArray<T>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            unsafe
            {
                ptr = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(array);
            }
            //array = new T[capacity];
            writePos = 0;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }
                array.Dispose();
                //array = null;
                disposedValue = true;
            }
        }
        ~MyNativeList()
        {
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if false//UNITY_EDITOR || DEVELOPMENT_BUILD
                if ((index < 0) || (index >= writePos))
                {
                    throw new IndexOutOfRangeException();
                }
#endif
#if true
                unsafe
                {
                    return ptr[index];
                }
#else
                return array[index];
#endif
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if false//UNITY_EDITOR || DEVELOPMENT_BUILD
                if ((index < 0) || (index >= writePos))
                {
                    throw new IndexOutOfRangeException();
                }
#endif
#if true
                unsafe
                {
                    ptr[index] = value;
                }
#else
                array[index] = value;
#endif
            }
        }

        public int Count => writePos;
        public bool IsReadOnly => false;
        public ref NativeArray<T> NativeArray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref array;
            }
        }
        public void SetCount(int count)
        {
            if (count > array.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            writePos = count;
        }
        public IntPtr GetUnsafeIntPtr()
        {
            unsafe
            {
                return (IntPtr)ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (writePos >= array.Length)
            {
                throw new OverflowException();
            }
#endif
            this[writePos++] = item;
        }
        public void Clear()
        {
            writePos = 0;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            for (int i = 0, end = writePos; i < end; i++)
            {
                if (item.Equals(this[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (index > writePos)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (writePos >= array.Length)
            {
                throw new OverflowException();
            }
#endif
            for (int i = writePos; i > index; i--)
            {
                this[i] = this[i - 1];
            }
            writePos++;
            this[index] = item;
        }
        public void RemoveAt(int index)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (index > writePos)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (writePos <= 0)
            {
                throw new InvalidOperationException();
            }
#endif
            writePos--;
            for (int i = index; i < writePos; i++)
            {
                this[i] = this[i + 1];
            }
        }
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }
    }
}
