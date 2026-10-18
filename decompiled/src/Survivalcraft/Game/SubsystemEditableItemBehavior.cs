using System.Collections.Generic;
using Engine;
using Engine.Serialization;
using TemplatesDatabase;

namespace Game;

public abstract class SubsystemEditableItemBehavior<T> : SubsystemBlockBehavior where T : IEditableItemData, new()
{
	private SubsystemItemsScanner m_subsystemItemsScanner;

	private int m_contents;

	private Dictionary<int, T> m_itemsData = new Dictionary<int, T>();

	private Dictionary<Point3, T> m_blocksData = new Dictionary<Point3, T>();

	protected SubsystemEditableItemBehavior(int contents)
	{
		m_contents = contents;
	}

	public T GetBlockData(Point3 point)
	{
		m_blocksData.TryGetValue(point, out var value);
		return value;
	}

	public void SetBlockData(Point3 point, T t)
	{
		if (t != null)
		{
			m_blocksData[point] = t;
		}
		else
		{
			m_blocksData.Remove(point);
		}
	}

	public T GetItemData(int id)
	{
		m_itemsData.TryGetValue(id, out var value);
		return value;
	}

	public int StoreItemDataAtUniqueId(T t)
	{
		int num = FindFreeItemId();
		m_itemsData[num] = t;
		return num;
	}

	public override void OnItemPlaced(int x, int y, int z, ref BlockPlacementData placementData, int itemValue)
	{
		int id = Terrain.ExtractData(itemValue);
		T itemData = GetItemData(id);
		if (itemData != null)
		{
			m_blocksData[new Point3(x, y, z)] = (T)itemData.Copy();
		}
	}

	public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue)
	{
		T blockData = GetBlockData(new Point3(x, y, z));
		if (blockData != null)
		{
			int num = FindFreeItemId();
			m_itemsData.Add(num, (T)blockData.Copy());
			dropValue.Value = Terrain.ReplaceData(dropValue.Value, num);
		}
	}

	public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
	{
		m_blocksData.Remove(new Point3(x, y, z));
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		base.Load(valuesDictionary);
		m_subsystemItemsScanner = base.Project.FindSubsystem<SubsystemItemsScanner>(throwOnError: true);
		T val;
		foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Blocks"))
		{
			Point3 key = HumanReadableConverter.ConvertFromString<Point3>(item.Key);
			T value = new T();
			ref T reference = ref value;
			val = default(T);
			if (val == null)
			{
				val = reference;
				reference = ref val;
			}
			string data = (string)item.Value;
			reference.LoadString(data);
			m_blocksData[key] = value;
		}
		foreach (KeyValuePair<string, object> item2 in valuesDictionary.GetValue<ValuesDictionary>("Items"))
		{
			int key2 = HumanReadableConverter.ConvertFromString<int>(item2.Key);
			T value2 = new T();
			ref T reference2 = ref value2;
			val = default(T);
			if (val == null)
			{
				val = reference2;
				reference2 = ref val;
			}
			string data2 = (string)item2.Value;
			reference2.LoadString(data2);
			m_itemsData[key2] = value2;
		}
		m_subsystemItemsScanner.ItemsScanned += GarbageCollectItems;
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		base.Save(valuesDictionary);
		ValuesDictionary valuesDictionary2 = new ValuesDictionary();
		valuesDictionary.SetValue("Blocks", valuesDictionary2);
		foreach (KeyValuePair<Point3, T> blocksDatum in m_blocksData)
		{
			valuesDictionary2.SetValue(HumanReadableConverter.ConvertToString(blocksDatum.Key), blocksDatum.Value.SaveString());
		}
		ValuesDictionary valuesDictionary3 = new ValuesDictionary();
		valuesDictionary.SetValue("Items", valuesDictionary3);
		foreach (KeyValuePair<int, T> itemsDatum in m_itemsData)
		{
			valuesDictionary3.SetValue(HumanReadableConverter.ConvertToString(itemsDatum.Key), itemsDatum.Value.SaveString());
		}
	}

	private int FindFreeItemId()
	{
		for (int i = 1; i < 1000; i++)
		{
			if (!m_itemsData.ContainsKey(i))
			{
				return i;
			}
		}
		return 0;
	}

	private void GarbageCollectItems(ReadOnlyList<ScannedItemData> allExistingItems)
	{
		HashSet<int> hashSet = new HashSet<int>();
		foreach (ScannedItemData item in allExistingItems)
		{
			if (Terrain.ExtractContents(item.Value) == m_contents)
			{
				hashSet.Add(Terrain.ExtractData(item.Value));
			}
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, T> itemsDatum in m_itemsData)
		{
			if (!hashSet.Contains(itemsDatum.Key))
			{
				list.Add(itemsDatum.Key);
			}
		}
		foreach (int item2 in list)
		{
			m_itemsData.Remove(item2);
		}
	}
}
