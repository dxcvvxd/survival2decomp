using System;
using System.Collections.Generic;
using System.Threading;

namespace Engine;

public static class Dispatcher
{
	private struct ActionInfo
	{
		public Action Action;

		public ManualResetEventSlim Event;
	}

	private static int? m_mainThreadId;

	private static List<ActionInfo> m_actionInfos = new List<ActionInfo>();

	private static List<ActionInfo> m_currentActionInfos = new List<ActionInfo>();

	public static int MainThreadId
	{
		get
		{
			if (!m_mainThreadId.HasValue)
			{
				throw new InvalidOperationException("Dispatcher is not initialized.");
			}
			return m_mainThreadId.Value;
		}
	}

	public static void Initialize()
	{
		m_mainThreadId = Environment.CurrentManagedThreadId;
	}

	public static void ExecuteActionsOnCurrentThread()
	{
		m_currentActionInfos.Clear();
		lock (m_actionInfos)
		{
			m_currentActionInfos.AddRange(m_actionInfos);
			m_actionInfos.Clear();
		}
		foreach (ActionInfo currentActionInfo in m_currentActionInfos)
		{
			try
			{
				currentActionInfo.Action();
			}
			catch (Exception ex)
			{
				Log.Error("Dispatched action failed. Reason: {0}", ex);
			}
			finally
			{
				if (currentActionInfo.Event != null)
				{
					currentActionInfo.Event.Set();
				}
			}
		}
	}

	public static void Dispatch(Action action, bool waitUntilCompleted = false)
	{
		if (!m_mainThreadId.HasValue)
		{
			throw new InvalidOperationException("Dispatcher is not initialized.");
		}
		if (m_mainThreadId.Value == Environment.CurrentManagedThreadId)
		{
			action();
			return;
		}
		if (waitUntilCompleted)
		{
			ActionInfo item = new ActionInfo
			{
				Action = action,
				Event = new ManualResetEventSlim(initialState: false)
			};
			lock (m_actionInfos)
			{
				m_actionInfos.Add(item);
			}
			item.Event.Wait();
			item.Event.Dispose();
			return;
		}
		lock (m_actionInfos)
		{
			m_actionInfos.Add(new ActionInfo
			{
				Action = action
			});
		}
	}

	internal static void BeforeFrame()
	{
		ExecuteActionsOnCurrentThread();
	}

	internal static void AfterFrame()
	{
	}
}
