using System;

public interface IHeapItem<T> : IComparable<T>
{
    int heapIndex { get; set; }
    T item { get; set; }
}
