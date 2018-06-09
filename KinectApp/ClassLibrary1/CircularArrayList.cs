using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MossiApi
{
    public class CircularArrayList<T>: IList<T>
    {
        private T[] arr;
        private int first, last;

        public CircularArrayList(int size)
        {
            arr = new T[size+1];
            first = last = 0;
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < this.Count; i++ )
            {
                if (arr[i].Equals(item))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public T this[int index]
        {
            get
            {
                if (index<arr.Length)
                    return arr[(first + index)%arr.Length];
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (index < arr.Length)
                    arr[(first + index) % arr.Length] = value;
                throw new IndexOutOfRangeException();
            }
        }

        public void Add(T item)
        {
            arr[last] = item;
            last = ++last % arr.Length;
            if (last == first)
                first = ++first % arr.Length;
        }

        public void Clear()
        {
            first = last = 0;
            //not necessary mandatory
            for (int i = 0; i < arr.Length; i++)
                arr[i] = default(T);
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex>=this.Count)
                throw new ArgumentOutOfRangeException();
            if (array == null)
                throw new ArgumentNullException();
            if (this.Count - arrayIndex > array.Length)
                throw new ArgumentException();

            for (int i = 0; i < this.Count - arrayIndex; i++)
                array[i] = this[i + arrayIndex];
        }

        public int Count
        {
            get 
            { 
                int count = last - first;
                return count >= 0 ? count : count + arr.Length;
            }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            T[] newArr = new T[this.Count];
            this.CopyTo(newArr, 0);
            return new CircularArrayListEnumerator<T>(newArr);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class CircularArrayListEnumerator<T> : IEnumerator<T>
        {
            private T[] arr;
            private int cur = -1;

            public CircularArrayListEnumerator(T[] arr)
            {
                this.arr = arr;
            }

            public T Current
            {
                get 
                {
                    if (cur >= 0 && cur < arr.Length)
                        return arr[cur];
                    throw new IndexOutOfRangeException();
                }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if(cur+1>=0 && cur+1<arr.Length)
                {
                    cur++;
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                cur = -1;
            }
        }
    }
}
