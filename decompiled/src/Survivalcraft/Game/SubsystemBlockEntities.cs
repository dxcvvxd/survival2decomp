using System.Collections.Generic;
using Engine;
using GameEntitySystem;

namespace Game;

public class SubsystemBlockEntities : Subsystem
{
	private Dictionary<Point3, ComponentBlockEntity> m_blockEntities = new Dictionary<Point3, ComponentBlockEntity>();

	public ComponentBlockEntity GetBlockEntity(int x, int y, int z)
	{
		m_blockEntities.TryGetValue(new Point3(x, y, z), out var value);
		return value;
	}

	protected override void OnEntityAdded(Entity entity)
	{
		ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
		if (componentBlockEntity != null)
		{
			m_blockEntities.Add(componentBlockEntity.Coordinates, componentBlockEntity);
		}
	}

	protected override void OnEntityRemoved(Entity entity)
	{
		ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
		if (componentBlockEntity != null)
		{
			m_blockEntities.Remove(componentBlockEntity.Coordinates);
		}
	}
}
