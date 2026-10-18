using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentBlockEntity : Component
{
	public Point3 Coordinates { get; private set; }

	protected override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		Coordinates = valuesDictionary.GetValue<Point3>("Coordinates");
	}

	protected override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
	{
		valuesDictionary.SetValue("Coordinates", Coordinates);
	}
}
