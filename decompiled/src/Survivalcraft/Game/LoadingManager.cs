using System;
using System.Linq;
using Engine;

namespace Game;

public static class LoadingManager
{
	private class Item
	{
		public Item Parent;

		public string Name;

		public Action Action;

		public DynamicArray<Item> Items = new DynamicArray<Item>();
	}

	private static Item m_item = new Item();

	private static bool m_paused;

	private static bool m_errorsSuppressed;

	public static void QueueItem(string name, Action action)
	{
		if (m_item == null)
		{
			throw new InvalidOperationException();
		}
		m_item.Items.Add(new Item
		{
			Parent = m_item,
			Name = name,
			Action = action
		});
	}

	public static bool LoadSomeItems()
	{
		double realTime = Time.RealTime;
		while (m_item != null && !m_paused)
		{
			try
			{
				double realTime2 = Time.RealTime;
				m_item.Action?.Invoke();
				double realTime3 = Time.RealTime;
				_ = realTime3 - realTime2;
				_ = 0.05;
				if (realTime3 - realTime2 > 3.0)
				{
					AnalyticsManager.LogError($"Excessive loading time of item '{m_item.Name}' {realTime3 - realTime2:0.000}s", new Exception());
				}
			}
			catch (Exception ex)
			{
				Log.Error("Loading error. Reason: " + ex.Message);
				if (!m_errorsSuppressed)
				{
					m_paused = true;
					DialogsManager.ShowDialog(ScreensManager.RootWidget, new MessageDialog("Loading Error", ExceptionManager.MakeFullErrorMessage(ex), "OK", "Suppress", delegate(MessageDialogButton b)
					{
						switch (b)
						{
						case MessageDialogButton.Button1:
							m_paused = false;
							break;
						case MessageDialogButton.Button2:
							m_errorsSuppressed = true;
							break;
						}
					}));
				}
			}
			m_item.Action = null;
			if (m_item.Items.Any())
			{
				m_item = m_item.Items.First();
			}
			else
			{
				if (m_item.Parent != null)
				{
					m_item.Parent.Items.Remove(m_item);
				}
				m_item = m_item.Parent;
			}
			if (Time.RealTime > realTime + 0.05000000074505806)
			{
				break;
			}
		}
		return m_item != null;
	}
}
