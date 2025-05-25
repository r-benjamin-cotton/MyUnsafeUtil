#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEBUG_FINALIZE_CHECK
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MyUnsafeUtil
{
    public readonly ref struct MyReadOnlyNativeArray<T> where T : unmanaged
    {
        private readonly MyNativeArray<T> myNativeArray;
        public MyReadOnlyNativeArray(MyNativeArray<T> myNativeArray)
        {
            this.myNativeArray = myNativeArray;
        }
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => myNativeArray[index];
        }

        public int Length => (myNativeArray == null) ? 0 : myNativeArray.Length;
        public bool IsCreated => (myNativeArray != null) && myNativeArray.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> to)
        {
            myNativeArray.AsReadOnlySpan().CopyTo(to);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(NativeArray<T> to)
        {
            myNativeArray.CopyTo(to);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return myNativeArray.AsReadOnlySpan();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> Slice(int start)
        {
            return Slice(start, myNativeArray.Length - start);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> Slice(int start, int length)
        {
            return myNativeArray.AsReadOnlySpan().Slice(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator MyReadOnlyNativeArray<T>(MyNativeArray<T> myNativeArray)
        {
            return new MyReadOnlyNativeArray<T>(myNativeArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(MyReadOnlyNativeArray<T> myNativeArray)
        {
            return myNativeArray.NativeArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(MyReadOnlyNativeArray<T> myNativeArray)
        {
            return myNativeArray.NativeArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<T>(MyReadOnlyNativeArray<T> myNativeArray)
        {
            return myNativeArray.myNativeArray;
        }
        public static bool operator ==(MyReadOnlyNativeArray<T> x, object y)
        {
            if (y != null)
            {
                return false;
            }
            return !x.IsCreated;
        }
        public static bool operator !=(MyReadOnlyNativeArray<T> x, object y)
        {
            return !(x == y);
        }
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                return false;
            }
            return !IsCreated;
        }
        public override int GetHashCode()
        {
            return myNativeArray.GetHashCode() ^ -1;
        }

        public NativeArray<T> NativeArray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => myNativeArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MyNativeArray<T>.Enumerator GetEnumerator()
        {
            return myNativeArray.GetEnumerator();
        }
    }
    public class MyNativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable where T : unmanaged
    {
        private bool disposedValue = false;
        private NativeArray<T> nativeArray = default;
        private unsafe T* ptr = null;
        private GCHandle gcHandle;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle atomicSafetyHandle;
#endif
#if DEBUG_FINALIZE_CHECK
        private System.Diagnostics.StackTrace stackTrace = null;
#endif

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private MyNativeArray<T> myNativeArray;
            private int readPos;
            private T value;
            public Enumerator(MyNativeArray<T> myNativeArray)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (myNativeArray == null)
                {
                    throw new ArgumentNullException("array");
                }
#endif
                this.myNativeArray = myNativeArray;
                readPos = -1;
                value = default;
            }
            public readonly T Current => value;
            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                myNativeArray = null;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var len = myNativeArray.Length;
                var pos = ++readPos;
                if (pos < len)
                {
                    value = myNativeArray[pos];
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

        public MyNativeArray(int length, bool noDispose)
        {
#if DEBUG_FINALIZE_CHECK
            if (noDispose)
            {
                stackTrace = null;
            }
            else
            {
                //stackTrace = new System.Diagnostics.StackTrace();
                stackTrace = new System.Diagnostics.StackTrace(true);
            }
#endif
            nativeArray = new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            unsafe
            {
                ptr = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray);
            }
        }
        public MyNativeArray(int length, Allocator allocator = Allocator.Persistent, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
#if DEBUG_FINALIZE_CHECK
            //stackTrace = new System.Diagnostics.StackTrace();
            stackTrace = new System.Diagnostics.StackTrace(true);
#endif
            nativeArray = new NativeArray<T>(length, allocator, options);
            unsafe
            {
                ptr = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray);
            }
        }
        public MyNativeArray(T[] managedArray)
        {
            gcHandle = GCHandle.Alloc(managedArray, GCHandleType.Pinned);
            var intPtr = gcHandle.AddrOfPinnedObject();
            unsafe
            {
                ptr = (T*)intPtr;
                nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, managedArray.Length, Allocator.None);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            atomicSafetyHandle = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, atomicSafetyHandle);
#endif
        }
        public MyNativeArray(IntPtr intPtr, int length)
        {
            unsafe
            {
                ptr = (T*)intPtr;
                nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            atomicSafetyHandle = AtomicSafetyHandle.Create();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, atomicSafetyHandle);
#endif
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
#if DEBUG_FINALIZE_CHECK
                    stackTrace = null;
#endif
                }
#if DEBUG_FINALIZE_CHECK
                else if (stackTrace != null)
                {
                    UnityEngine.Debug.Log(stackTrace);
                }
#endif
                {
                    if (gcHandle.IsAllocated)
                    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        AtomicSafetyHandle.CheckDeallocateAndThrow(atomicSafetyHandle);
                        AtomicSafetyHandle.Release(atomicSafetyHandle);
#endif
                        gcHandle.Free();
                    }
                    if (nativeArray.IsCreated)
                    {
                        nativeArray.Dispose();
                    }
                }
                unsafe
                {
                    ptr = null;
                }
                gcHandle = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                atomicSafetyHandle = default;
#endif
                nativeArray = default;
                disposedValue = true;
            }
        }
        ~MyNativeArray()
        {
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            nativeArray.AsSpan().Clear();
        }
        public void Fill(T value)
        {
            nativeArray.AsSpan().Fill(value);
        }
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if ((index < 0) || (index >= nativeArray.Length))
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
                return nativeArray[index];
#endif
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if ((index < 0) || (index >= nativeArray.Length))
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
                nativeArray[index] = value;
#endif
            }
        }

        public int Length => nativeArray.Length;
        public bool IsCreated => nativeArray.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> to)
        {
            nativeArray.AsReadOnlySpan().CopyTo(to);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(ReadOnlySpan<T> from)
        {
            from.CopyTo(nativeArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(NativeArray<T> to)
        {
            nativeArray.CopyTo(to);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(NativeArray<T> from)
        {
            nativeArray.CopyFrom(from);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(MyNativeArray<T> to)
        {
            nativeArray.CopyTo(to.nativeArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(MyNativeArray<T> from)
        {
            nativeArray.CopyFrom(from.nativeArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom<E>(E from) where E : IEnumerable<T>
        {
            var idx = 0;
            var end = nativeArray.Length;
            foreach (var v in from)
            {
                if (idx >= end)
                {
                    return;
                }
                nativeArray[idx++] = v;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return nativeArray.AsSpan();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return nativeArray.AsReadOnlySpan();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan<U> Reinterpret<U>() where U : struct
        {
            return nativeArray.Reinterpret<U>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Slice(int start)
        {
            return Slice(start, nativeArray.Length - start);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Slice(int start, int length)
        {
            return nativeArray.AsSpan().Slice(start, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ReadOnlySpan<byte> AsReadOnlyByteSpan()
        {
            return new ReadOnlySpan<byte>(ptr, Length * sizeof(T));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<byte> AsByteSpan()
        {
            return new Span<byte>(ptr, Length * sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(MyNativeArray<T> myNativeArray)
        {
            if (myNativeArray == null)
            {
                return default;
            }
            return myNativeArray.NativeArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(MyNativeArray<T> myNativeArray)
        {
            if (myNativeArray == null)
            {
                return default;
            }
            return myNativeArray.NativeArray;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator NativeArray<T>(MyNativeArray<T> myNativeArray)
        {
            if (myNativeArray == null)
            {
                return default;
            }
            return myNativeArray.NativeArray;
        }
        public ref NativeArray<T> NativeArray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref nativeArray;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetUnsafeIntPtr()
        {
            unsafe
            {
                return (IntPtr)ptr;
            }
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
    }
}
