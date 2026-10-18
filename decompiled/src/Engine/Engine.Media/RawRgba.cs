using System;
using System.IO;
using Engine.Serialization;

namespace Engine.Media;

public static class RawRgba
{
	public enum Format
	{
		RGBA8,
		RGB8,
		LA8,
		L8
	}

	public struct RawRgbaInfo
	{
		public int Width;

		public int Height;

		public Format Format;
	}

	private struct Header
	{
		public ushort Width;

		public ushort Height;

		public Format Format;
	}

	public static bool IsRawRgbaStream(Stream stream)
	{
		long position = stream.Position;
		bool result = ReadHeader(stream, out var _);
		stream.Position = position;
		return result;
	}

	public static RawRgbaInfo GetInfo(Stream stream)
	{
		if (!ReadHeader(stream, out var header))
		{
			throw new InvalidOperationException("Invalid RawRgba stream.");
		}
		return new RawRgbaInfo
		{
			Width = header.Width,
			Height = header.Height,
			Format = header.Format
		};
	}

	public static Image Load(Stream stream)
	{
		if (!ReadHeader(stream, out var header))
		{
			throw new InvalidOperationException("Invalid RawRgba stream.");
		}
		EngineBinaryReader engineBinaryReader = new EngineBinaryReader(stream);
		Image image = new Image(header.Width, header.Height);
		if (header.Format == Format.RGBA8)
		{
			byte[] array = engineBinaryReader.ReadBytes(4 * image.Pixels.Length);
			for (int i = 0; i < image.Pixels.Length; i++)
			{
				image.Pixels[i] = new Color(array[4 * i], array[4 * i + 1], array[4 * i + 2], array[4 * i + 3]);
			}
		}
		else if (header.Format == Format.RGB8)
		{
			byte[] array2 = engineBinaryReader.ReadBytes(3 * image.Pixels.Length);
			for (int j = 0; j < image.Pixels.Length; j++)
			{
				image.Pixels[j] = new Color(array2[3 * j], array2[3 * j + 1], array2[3 * j + 2]);
			}
		}
		else if (header.Format == Format.LA8)
		{
			byte[] array3 = engineBinaryReader.ReadBytes(2 * image.Pixels.Length);
			for (int k = 0; k < image.Pixels.Length; k++)
			{
				byte b = array3[2 * k];
				byte a = array3[2 * k + 1];
				image.Pixels[k] = new Color(b, b, b, a);
			}
		}
		else
		{
			if (header.Format != Format.L8)
			{
				throw new InvalidOperationException("Unsupported RawRgba pixel format.");
			}
			byte[] array4 = engineBinaryReader.ReadBytes(image.Pixels.Length);
			for (int l = 0; l < image.Pixels.Length; l++)
			{
				byte b2 = array4[l];
				image.Pixels[l] = new Color(b2, b2, b2);
			}
		}
		return image;
	}

	public static void Save(Image image, Stream stream, Format format)
	{
		if (image == null)
		{
			throw new ArgumentNullException("image");
		}
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		WriteHeader(stream, new Header
		{
			Width = (ushort)image.Width,
			Height = (ushort)image.Height,
			Format = format
		});
		EngineBinaryWriter engineBinaryWriter = new EngineBinaryWriter(stream);
		switch (format)
		{
		case Format.RGBA8:
		{
			for (int l = 0; l < image.Pixels.Length; l++)
			{
				engineBinaryWriter.Write(image.Pixels[l]);
			}
			break;
		}
		case Format.RGB8:
		{
			for (int j = 0; j < image.Pixels.Length; j++)
			{
				Color color2 = image.Pixels[j];
				engineBinaryWriter.Write(color2.R);
				engineBinaryWriter.Write(color2.G);
				engineBinaryWriter.Write(color2.B);
			}
			break;
		}
		case Format.LA8:
		{
			for (int k = 0; k < image.Pixels.Length; k++)
			{
				Color color3 = image.Pixels[k];
				byte value2 = (byte)((color3.R + color3.G + color3.B + 1) / 3);
				engineBinaryWriter.Write(value2);
				engineBinaryWriter.Write(color3.A);
			}
			break;
		}
		case Format.L8:
		{
			for (int i = 0; i < image.Pixels.Length; i++)
			{
				Color color = image.Pixels[i];
				byte value = (byte)((color.R + color.G + color.B + 1) / 3);
				engineBinaryWriter.Write(value);
			}
			break;
		}
		default:
			throw new InvalidOperationException("Unsupported RawRgba format.");
		}
	}

	private static bool ReadHeader(Stream stream, out Header header)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		int num = stream.ReadByte();
		int num2 = stream.ReadByte();
		int num3 = stream.ReadByte();
		header.Format = (Format)stream.ReadByte();
		if (num == 82 && num2 == 65 && num3 == 87 && header.Format >= Format.RGBA8 && header.Format <= Format.L8)
		{
			EngineBinaryReader engineBinaryReader = new EngineBinaryReader(stream);
			header.Width = engineBinaryReader.ReadUInt16();
			header.Height = engineBinaryReader.ReadUInt16();
			return true;
		}
		header = default(Header);
		return false;
	}

	private static void WriteHeader(Stream stream, Header header)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		EngineBinaryWriter engineBinaryWriter = new EngineBinaryWriter(stream);
		engineBinaryWriter.Write((byte)82);
		engineBinaryWriter.Write((byte)65);
		engineBinaryWriter.Write((byte)87);
		engineBinaryWriter.Write((byte)header.Format);
		engineBinaryWriter.Write(header.Width);
		engineBinaryWriter.Write(header.Height);
	}
}
