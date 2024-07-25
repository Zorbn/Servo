namespace Servo;

public class ArrayList<T>
{
    public int Count { get; private set; }
    public T[] Array => array;

    private T[] array;

    public ArrayList(int size = 256)
    {
        array = new T[size];
    }

    public T this[int i]
    {
        get => array[i];
        set => array[i] = value;
    }

    public void Add(T element)
    {
        while (Count >= array.Length) Expand();

        array[Count] = element;
        ++Count;
    }

    public void Reserve(int amount)
    {
        while (Count + amount >= array.Length) Expand();
    }

    public void AddUnchecked(T element)
    {
        array[Count] = element;
        ++Count;
    }

    private void Expand()
    {
        System.Array.Resize(ref array, array.Length * 2);
    }

    public void Clear()
    {
        Count = 0;
    }
}