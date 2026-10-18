using System;
using System.Linq;
using Engine;

namespace Game;

public class TerrainGeometryDynamicArray<T> : DynamicArray<T>, IDisposable
{
	private static ArrayCache<T> m_cache = new ArrayCache<T>(from n in Enumerable.Range(4, 30)
		select 1 << n, 0.66f, 60f, 0.33f, 5f);

	public void Dispose()
	{
		base.Count = 0;
		base.Capacity = 0;
	}

	protected override T[] Allocate(int capacity)
	{
		return m_cache.Rent(capacity, clearArray: false);
	}

	protected override void Free(T[] array)
	{
		m_cache.Return(array);
	}
}
