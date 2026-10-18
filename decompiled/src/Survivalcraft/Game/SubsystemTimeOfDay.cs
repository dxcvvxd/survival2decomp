using System;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class SubsystemTimeOfDay : Subsystem
{
	private SubsystemGameInfo m_subsystemGameInfo;

	private SubsystemSeasons m_subsystemSeasons;

	public bool TimeOfDayEnabled = true;

	public const float DayDuration = 1200f;

	public float DawnStart { get; private set; }

	public float DayStart { get; private set; }

	public float DuskStart { get; private set; }

	public float NightStart { get; private set; }

	public float DayInterval => IntervalUtils.Interval(DayStart, DuskStart);

	public float DuskInterval => IntervalUtils.Interval(DuskStart, NightStart);

	public float NightInterval => IntervalUtils.Interval(NightStart, DawnStart);

	public float DawnInterval => IntervalUtils.Interval(DawnStart, DayStart);

	public float Midday => IntervalUtils.Midpoint(DayStart, DuskStart);

	public float Middusk => IntervalUtils.Midpoint(DuskStart, NightStart);

	public float Midnight => IntervalUtils.Midpoint(NightStart, DawnStart);

	public float Middawn => IntervalUtils.Midpoint(DawnStart, DayStart);

	public float TimeOfDay
	{
		get
		{
			if (TimeOfDayEnabled)
			{
				if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Changing)
				{
					return CalculateTimeOfDay(m_subsystemGameInfo.TotalElapsedGameTime);
				}
				if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Day)
				{
					return Midday;
				}
				if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Night)
				{
					return Midnight;
				}
				if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunrise)
				{
					return Middawn;
				}
				if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunset)
				{
					return Middusk;
				}
				return Midday;
			}
			return Midday;
		}
	}

	public double Day => CalculateDay(m_subsystemGameInfo.TotalElapsedGameTime);

	public double TimeOfDayOffset { get; set; }

	public double CalculateDay(double totalElapsedGameTime)
	{
		return (totalElapsedGameTime + (TimeOfDayOffset + (double)DayStart) * 1200.0) / 1200.0;
	}

	public float CalculateTimeOfDay(double totalElapsedGameTime)
	{
		double num = CalculateDay(totalElapsedGameTime);
		return (float)(num - MathUtils.Floor(num));
	}

	protected override void Load(ValuesDictionary valuesDictionary)
	{
		m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		m_subsystemSeasons = base.Project.FindSubsystem<SubsystemSeasons>(throwOnError: true);
		TimeOfDayOffset = valuesDictionary.GetValue<double>("TimeOfDayOffset");
		UpdateStarts();
	}

	protected override void Save(ValuesDictionary valuesDictionary)
	{
		valuesDictionary.SetValue("TimeOfDayOffset", TimeOfDayOffset);
	}

	private void UpdateStarts()
	{
		float num = IntervalUtils.Midpoint(SubsystemSeasons.SummerStart, SubsystemSeasons.AutumnStart);
		float num2 = MathUtils.Remainder(m_subsystemGameInfo.WorldSettings.TimeOfYear - num, 1f);
		float num3 = MathUtils.Lerp(0.2f, 0.4f, 0.5f + 0.5f * MathUtils.Cos((float)Math.PI * 2f * num2));
		float num4 = 0.4f;
		float num5 = (1f - (num3 + num4)) / 2f;
		DayStart = 0.3f;
		DawnStart = IntervalUtils.Add(DayStart, 0f - num5);
		DuskStart = IntervalUtils.Add(DayStart, num3);
		NightStart = IntervalUtils.Add(DuskStart, num5);
	}
}
