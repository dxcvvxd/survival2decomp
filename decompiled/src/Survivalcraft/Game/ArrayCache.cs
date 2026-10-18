using System;
using System.Collections.Generic;
using System.Linq;
using Engine;

namespace Game;

public class ArrayCache<T>
{
	private class Bucket
	{
		public int Capacity;

		public Stack<T[]> Stack = new Stack<T[]>();
	}

	private object m_lock = new object();

	private Bucket[] m_buckets;

	private long m_cachedCount;

	private long m_usedCount;

	private float m_minCacheRatio1;

	private float m_minCacheDuration1;

	private double m_minCacheRatioLastTime1;

	private float m_minCacheRatio2;

	private float m_minCacheDuration2;

	private double m_minCacheRatioLastTime2;

	public ArrayCache(IEnumerable<int> bucketSizes, float minCacheRatio1, float minCacheTime1, float minCacheRatio2, float minCacheTime2)
	{
		m_buckets = (from s in bucketSizes
			orderby s
			select new Bucket
			{
				Capacity = s
			}).ToArray();
		m_minCacheRatio1 = minCacheRatio1;
		m_minCacheDuration1 = minCacheTime1;
		m_minCacheRatio2 = minCacheRatio2;
		m_minCacheDuration2 = minCacheTime2;
		m_minCacheRatioLastTime1 = Time.FrameStartTime;
		m_minCacheRatioLastTime2 = Time.FrameStartTime;
		Window.LowMemory += ClearCache;
		Time.QueueTimeDelayedExecution(0.0, CheckCache);
	}

	public T[] Rent(int capacity, bool clearArray)
	{
		lock (m_lock)
		{
			Bucket bucket = GetBucket(capacity);
			if (bucket != null)
			{
				if (bucket.Stack.Count > 0)
				{
					T[] array = bucket.Stack.Pop();
					if (clearArray)
					{
						Array.Clear(array, 0, array.Length);
					}
					m_cachedCount -= array.Length;
					m_usedCount += array.Length;
					return array;
				}
				m_usedCount += bucket.Capacity;
				return new T[bucket.Capacity];
			}
			return new T[capacity];
		}
	}

	public void Return(T[] array)
	{
		lock (m_lock)
		{
			Bucket bucket = GetBucket(array.Length);
			if (bucket != null)
			{
				bucket.Stack.Push(array);
				m_cachedCount += array.Length;
				m_usedCount -= array.Length;
			}
			float num = CalculateCacheRatio();
			if (num >= m_minCacheRatio1)
			{
				m_minCacheRatioLastTime1 = Time.FrameStartTime;
			}
			if (num >= m_minCacheRatio2)
			{
				m_minCacheRatioLastTime2 = Time.FrameStartTime;
			}
		}
	}

	private void CheckCache()
	{
		lock (m_lock)
		{
			float num = CalculateCacheRatio();
			if ((num < m_minCacheRatio1 && Time.FrameStartTime - m_minCacheRatioLastTime1 > (double)m_minCacheDuration1) || (num < m_minCacheRatio2 && Time.FrameStartTime - m_minCacheRatioLastTime2 > (double)m_minCacheDuration2))
			{
				ClearCache();
			}
			Time.QueueTimeDelayedExecution(Time.FrameStartTime + (double)(MathUtils.Min(m_minCacheDuration1, m_minCacheDuration2) / 5f), CheckCache);
		}
	}

	private Bucket GetBucket(int capacity)
	{
		for (int i = 0; i < m_buckets.Length; i++)
		{
			if (m_buckets[i].Capacity >= capacity)
			{
				return m_buckets[i];
			}
		}
		return null;
	}

	private void ClearCache()
	{
		Bucket[] buckets = m_buckets;
		for (int i = 0; i < buckets.Length; i++)
		{
			buckets[i].Stack.Clear();
		}
		m_cachedCount = 0L;
		m_minCacheRatioLastTime1 = Time.FrameStartTime;
		m_minCacheRatioLastTime2 = Time.FrameStartTime;
	}

	private float CalculateCacheRatio()
	{
		if (m_cachedCount <= 0)
		{
			return 1f;
		}
		return (float)m_usedCount / (float)(m_usedCount + m_cachedCount);
	}
}
