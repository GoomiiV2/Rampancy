namespace Rampancy
{
    public class ResizeableArray<T>
    {
        public T[] Array;
        public int Count { private set; get; } = 0;

        public ResizeableArray(int capacity = 10)
        {
            Array = new T[capacity];
            Count = capacity;
        }

        public int Add(T item)
        {
            if (Count == Array.Length) {
                System.Array.Resize(ref Array, Array.Length * 2);
            }

            Array[Count++] = item;

            return Count;
        }
    }
}