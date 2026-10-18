using System.Net;

namespace Engine.Serialization;

internal class IPEndPointSerializer : ISerializer<IPEndPoint>
{
	public void Serialize(InputArchive archive, ref IPEndPoint value)
	{
		IPAddress value2 = null;
		ushort value3 = 0;
		archive.Serialize("Address", ref value2);
		archive.Serialize("Port", ref value3);
		value = new IPEndPoint(value2, value3);
	}

	public void Serialize(OutputArchive archive, IPEndPoint value)
	{
		archive.Serialize("Address", value.Address);
		archive.Serialize("Port", (ushort)value.Port);
	}
}
