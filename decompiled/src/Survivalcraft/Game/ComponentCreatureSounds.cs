using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentCreatureSounds : Component
{
	private SubsystemTime m_subsystemTime;

	private SubsystemAudio m_subsystemAudio;

	private SubsystemSoundMaterials m_subsystemSoundMaterials;

	private ComponentCreature m_componentCreature;

	private Random m_random = new Random();

	private string m_idleSound;

	private string m_rareIdleSound;

	private string m_painSound;

	private string m_moanSound;

	private string m_sneezeSound;

	private string m_coughSound;

	private string m_pukeSound;

	private string m_attackSound;

	private float m_idleSoundMinDistance;

	private float m_rareIdleSoundMinDistance;

	private float m_rareIdleSoundProbability;

	private float m_painSoundMinDistance;

	private float m_moanSoundMinDistance;

	private float m_sneezeSoundMinDistance;

	private float m_coughSoundMinDistance;

	private float m_pukeSoundMinDistance;

	private float m_attackSoundMinDistance;

	private double m_lastSoundTime = -1000.0;

	private double m_lastCoughingSoundTime = -1000.0;

	private double m_lastPukeSoundTime = -1000.0;

	public void PlayIdleSound(bool skipIfRecentlyPlayed)
	{
		int num;
		string text;
		if (!string.IsNullOrEmpty(m_rareIdleSound))
		{
			num = (m_random.Bool(m_rareIdleSoundProbability) ? 1 : 0);
			if (num != 0)
			{
				text = m_rareIdleSound;
				goto IL_0032;
			}
		}
		else
		{
			num = 0;
		}
		text = m_idleSound;
		goto IL_0032;
		IL_0032:
		string text2 = text;
		float minDistance = ((num != 0) ? m_rareIdleSoundMinDistance : m_idleSoundMinDistance);
		if (!string.IsNullOrEmpty(text2) && m_subsystemTime.GameTime > m_lastSoundTime + (double)(skipIfRecentlyPlayed ? 12f : 1f))
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(text2, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, minDistance, autoDelay: false);
		}
	}

	public void PlayPainSound()
	{
		if (!string.IsNullOrEmpty(m_painSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_painSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_painSoundMinDistance, autoDelay: false);
		}
	}

	public void PlayMoanSound()
	{
		if (!string.IsNullOrEmpty(m_moanSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_moanSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_moanSoundMinDistance, autoDelay: false);
		}
	}

	public void PlaySneezeSound()
	{
		if (!string.IsNullOrEmpty(m_sneezeSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_sneezeSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_sneezeSoundMinDistance, autoDelay: false);
		}
	}

	public void PlayCoughSound()
	{
		if (!string.IsNullOrEmpty(m_coughSound) && m_subsystemTime.GameTime > m_lastCoughingSoundTime + 1.0)
		{
			m_lastCoughingSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_coughSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_coughSoundMinDistance, autoDelay: false);
		}
	}

	public void PlayPukeSound()
	{
		if (!string.IsNullOrEmpty(m_pukeSound) && m_subsystemTime.GameTime > m_lastPukeSoundTime + 1.0)
		{
			m_lastPukeSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_pukeSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_pukeSoundMinDistance, autoDelay: false);
		}
	}

	public void PlayAttackSound()
	{
		if (!string.IsNullOrEmpty(m_attackSound) && m_subsystemTime.GameTime > m_lastSoundTime + 1.0)
		{
			m_lastSoundTime = m_subsystemTime.GameTime;
			m_subsystemAudio.PlayRandomSound(m_attackSound, 1f, m_random.Float(-0.1f, 0.1f), m_componentCreature.ComponentBody.Position, m_attackSoundMinDistance, autoDelay: false);
		}
	}

	public bool PlayFootstepSound(float loudnessMultiplier)
	{
		return m_subsystemSoundMaterials.PlayFootstepSound(m_componentCreature, loudnessMultiplier);
	}

	protected override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		m_subsystemSoundMaterials = base.Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
		m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
		m_idleSound = valuesDictionary.GetValue<string>("IdleSound");
		m_rareIdleSound = valuesDictionary.GetValue<string>("RareIdleSound");
		m_painSound = valuesDictionary.GetValue<string>("PainSound");
		m_moanSound = valuesDictionary.GetValue<string>("MoanSound");
		m_sneezeSound = valuesDictionary.GetValue<string>("SneezeSound");
		m_coughSound = valuesDictionary.GetValue<string>("CoughSound");
		m_pukeSound = valuesDictionary.GetValue<string>("PukeSound");
		m_attackSound = valuesDictionary.GetValue<string>("AttackSound");
		m_idleSoundMinDistance = valuesDictionary.GetValue<float>("IdleSoundMinDistance");
		m_rareIdleSoundMinDistance = valuesDictionary.GetValue<float>("RareIdleSoundMinDistance");
		m_rareIdleSoundProbability = valuesDictionary.GetValue<float>("RareIdleSoundProbability");
		m_painSoundMinDistance = valuesDictionary.GetValue<float>("PainSoundMinDistance");
		m_moanSoundMinDistance = valuesDictionary.GetValue<float>("MoanSoundMinDistance");
		m_sneezeSoundMinDistance = valuesDictionary.GetValue<float>("SneezeSoundMinDistance");
		m_coughSoundMinDistance = valuesDictionary.GetValue<float>("CoughSoundMinDistance");
		m_pukeSoundMinDistance = valuesDictionary.GetValue<float>("PukeSoundMinDistance");
		m_attackSoundMinDistance = valuesDictionary.GetValue<float>("AttackSoundMinDistance");
	}
}
