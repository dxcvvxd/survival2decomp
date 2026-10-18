using System;
using System.Collections;
using System.Collections.Generic;

namespace Engine;

public struct ReadOnlyList<T>(IReadOnlyList<T> list) : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	public struct Enumerator(IReadOnlyList<T> list) : IEnumerator<T>, IEnumerator, IDisposable
	{
		private IReadOnlyList<T> m_list = list;

		private int m_index = -1;

		public T Current => m_list[m_index];

		object IEnumerator.Current => m_list[m_index];

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			return ++m_index < m_list.Count;
		}

		public void Reset()
		{
			m_index = -1;
		}
	}

	private IReadOnlyList<T> m_list = list;

	private static ReadOnlyList<T> m_empty = new ReadOnlyList<T>(new T[0]);

	public static ReadOnlyList<T> Empty => m_empty;

	public T this[int index] => m_list[index];

	public int Count => m_list.Count;

	public Enumerator GetEnumerator()
	{
		return new Enumerator(m_list);
	}

	public int IndexOf(T item)
	{
		for (int i = 0; i < m_list.Count; i++)
		{
			if (object.Equals(m_list[i], item))
			{
				return i;
			}
		}
		return -1;
	}

	public bool Contains(T item)
	{
		return IndexOf(item) >= 0;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(m_list);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(m_list);
	}
}
