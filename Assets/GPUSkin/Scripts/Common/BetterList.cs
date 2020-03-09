﻿

namespace Common
{
    public class BetterList<T>
    {
        public  T[] buffer;
        public  int size;
        private int bufferIncrement = 0;

        public T this[int i]
        {
            get { return buffer[i]; }
            set { buffer[i] = value; }
        }

        public BetterList(int bufferIncrement = 8)
        {
            this.bufferIncrement = System.Math.Max(1, bufferIncrement);
        }

        void AllocMore()
        {
            T[] newList = (buffer != null) ? new T[buffer.Length + bufferIncrement] : new T[bufferIncrement];
            if (buffer != null && size > 0)
            {
                buffer.CopyTo(newList, 0);
            }
            buffer = newList;
        }

        public void Clear()
        {
            size = 0;
        }

        public void Release()
        {
            size = 0;
            buffer = null;
        }

        public void Add(T item)
        {
            if (buffer == null || size == buffer.Length)
            {
                AllocMore();
            }
            buffer[size++] = item;
        }

        public void AddRange(T[] items)
        {
            if (items == null)
            {
                return;
            }
            int len = items.Length;
            if (len == 0)
            {
                return;
            }

            if (buffer == null)
            {
                buffer = new T[System.Math.Max(bufferIncrement, len)];
                items.CopyTo(buffer, 0);
                size = len;
            }
            else
            {
                if (size + len > buffer.Length)
                {
                    T[] newList = new T[System.Math.Max(buffer.Length + bufferIncrement, size + len)];
                    buffer.CopyTo(newList, 0);
                    items.CopyTo(newList, size);
                    buffer = newList;
                }
                else
                {
                    items.CopyTo(buffer, size);
                }
                size += len;
            }
        }

        public void RemoveAt(int index)
        {
            if (buffer != null && index > -1 && index < size)
            {
                --size;
                for(int i = index; i < size; ++ i)
                {
                    buffer[i] = buffer[i + 1];
                }
                buffer[size] = default(T);
            }
        }

        public T Pop()
        {
            if (buffer == null || size == 0)
            {
                return default(T);
            }
            --size;
            T t = buffer[size];
            buffer[size] = default(T);
            return t;
        }

        public T Peek()
        {
            if (buffer == null || size == 0)
            {
                return default(T);
            }
            return buffer[size - 1];
        }

    }
}
