using System;
using System.Collections;
using System.Collections.Generic;

namespace Engine;

public struct ReadOnlySet<T>(HashSet<T> set) : IEnumerable<T>, IEnumerable, ICollection<T>
{
	private HashSet<T> m_set = set;

	private static ReadOnlySet<T> m_empty = new ReadOnlySet<T>(new HashSet<T>());

	public static ReadOnlySet<T> Empty => m_empty;

	public int Count => m_set.Count;

	public bool IsReadOnly => true;

	public HashSet<T>.Enumerator GetEnumerator()
	{
		return m_set.GetEnumerator();
	}

	public bool Contains(T item)
	{
		return m_set.Contains(item);
	}

	public bool Overlaps(IEnumerable<T> items)
	{
		return m_set.Overlaps(items);
	}

	void ICollection<T>.Add(T item)
	{
		throw new InvalidOperationException("Cannot modify a readonly set.");
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new InvalidOperationException("Cannot modify a readonly set.");
	}

	void ICollection<T>.Clear()
	{
		throw new InvalidOperationException("Cannot modify a readonly set.");
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		m_set.CopyTo(array, arrayIndex);
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
