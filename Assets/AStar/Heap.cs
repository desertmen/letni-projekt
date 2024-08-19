using System.Collections.Generic;

public enum HeapType
{
    MIN_HEAP = -1,
    MAX_HEAP = 1
}

public class Heap<T> where T : IHeapItem<T>
{
    private List<T> heap = new List<T>();
    private HeapType heapType;
    
    // if implementing custom IComparable, CompareTo MUST return 0, 1 or -1, or it breaks
    public Heap(HeapType heapType)
    {
        this.heapType = heapType;
    }

    public void clear()
    {
        heap.Clear();
    }

    public void push(T item)
    {
        heap.Add(item);
        item.heapIndex = heap.Count - 1;
        sortUp(heap.Count - 1);
    }

    public T pop()
    {
        if(heap.Count == 0)
            return default(T);

        T top = heap[0];
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        heapify();
        return top;
    }

    public T peek()
    {
        if(heap.Count > 0)
            return heap[0];
        return default(T);
    }

    public void updateUp(T item)
    {
        if (contains(item))
        {
            sortUp(item.heapIndex);
        }
    }

    public bool isEmpty() { return heap.Count == 0; }
    public bool contains(T item) { 
        return item.heapIndex >= 0 && item.heapIndex < heap.Count && item.Equals(heap[item.heapIndex]); 
    }

    public int size() { return heap.Count;}

    private void heapify()
    {
        int top = 0;
        int correct = top;

        do
        {
            top = correct;
            int left = getLeftIdx(top);
            int right = getRightIdx(top);

            if (left < heap.Count && heap[left].CompareTo(heap[top]) == (int)heapType)
            {
                correct = left;
            }
            if (right < heap.Count && heap[right].CompareTo(heap[top]) == (int)heapType && heap[right].CompareTo(heap[left]) == (int)heapType)
            {
                correct = right;
            }
            if (top != correct)
            {
                swap(top, correct);
            }
        } while (top != correct);
    }

    private void sortUp(int idx)
    {
        int parrentIdx = getParrentIdx(idx);

        // swap until parrent is smaller/larget depending on heap type
        while (heap[idx].CompareTo(heap[parrentIdx]) == (int)heapType)
        {
            swap(idx, parrentIdx);

            idx = parrentIdx;
            parrentIdx = getParrentIdx(idx);
        }
    }

    private void swap(int idx1, int idx2)
    {
        T tmp = heap[idx1];
        heap[idx1] = heap[idx2];
        heap[idx2] = tmp;

        heap[idx1].heapIndex = idx1;
        heap[idx2].heapIndex = idx2;
    }

    private int getParrentIdx(int idx) { return (idx - 1) / 2; }
    private int getLeftIdx(int idx) { return (idx * 2) + 1; }
    private int getRightIdx(int idx) { return (idx * 2) + 2; }
}
