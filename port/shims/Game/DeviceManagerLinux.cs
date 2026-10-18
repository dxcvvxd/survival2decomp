using System;
using System.Runtime.InteropServices;

namespace Game;

public static class DeviceManager
{
	public static string DeviceModel => Environment.MachineName;

	public static string OperatingSystemVersion => RuntimeInformation.OSDescription;
}
