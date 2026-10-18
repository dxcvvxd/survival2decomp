using System;
using System.Net;

namespace Engine.Serialization;

internal class SystemNetTypesConverters
{
	[HumanReadableConverter(new Type[] { typeof(IPAddress) })]
	internal class IPAddressStringConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return value.ToString();
		}

		public object ConvertFromString(Type type, string data)
		{
			return IPAddress.Parse(data);
		}
	}

	[HumanReadableConverter(new Type[] { typeof(IPEndPoint) })]
	internal class IPEndPointStringConverter : IHumanReadableConverter
	{
		public string ConvertToString(object value)
		{
			return value.ToString();
		}

		public object ConvertFromString(Type type, string data)
		{
			int num = data.LastIndexOf(':');
			if (num < 0)
			{
				throw new InvalidOperationException("Invalid IPEndPoint format.");
			}
			IPAddress address = IPAddress.Parse(data.Substring(0, num));
			int port = int.Parse(data.Substring(num + 1));
			return new IPEndPoint(address, port);
		}
	}
}
