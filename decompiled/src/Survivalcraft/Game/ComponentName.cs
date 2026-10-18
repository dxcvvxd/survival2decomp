using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentName : Component
{
	private string m_name;

	public string Name => m_name;

	protected override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_name = valuesDictionary.GetValue<string>("Name");
	}
}
