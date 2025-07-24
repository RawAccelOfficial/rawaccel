using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace userinterface.Helpers;

public class ObjectPool<T> : IDisposable where T : class
{
    private readonly ConcurrentBag<T> objects = new();
    private readonly Func<T> objectGenerator;
    private readonly Action<T> resetAction;
    private bool disposed = false;

    public ObjectPool(Func<T> objectGenerator, Action<T> resetAction = null)
    {
        this.objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        this.resetAction = resetAction;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get()
    {
        if (objects.TryTake(out T item))
        {
            resetAction?.Invoke(item);
            return item;
        }
        
        return objectGenerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(T item)
    {
        if (item != null && !disposed)
        {
            objects.Add(item);
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            
            // Dispose all pooled objects if they implement IDisposable
            while (objects.TryTake(out T item))
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}