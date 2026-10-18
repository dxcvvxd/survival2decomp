using System.Net;

namespace Engine.Serialization;

internal class IPAddressSerializer : ISerializer<IPAddress>
{
	public void Serialize(InputArchive archive, ref IPAddress value)
	{
		byte[] value2 = null;
		archive.Serialize(null, ref value2);
		value = new IPAddress(value2);
	}

	public void Serialize(OutputArchive archive, IPAddress value)
	{
		archive.Serialize(null, value.GetAddressBytes());
	}
}
