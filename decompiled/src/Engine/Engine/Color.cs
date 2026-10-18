using System;

namespace Engine;

public struct Color : IEquatable<Color>
{
	public uint PackedValue;

	public static Color Black = new Color(0, 0, 0, 255);

	public static Color DarkGray = new Color(64, 64, 64, 255);

	public static Color Gray = new Color(128, 128, 128, 255);

	public static Color LightGray = new Color(192, 192, 192, 255);

	public static Color White = new Color(255, 255, 255, 255);

	public static Color Red = new Color(255, 0, 0, 255);

	public static Color Green = new Color(0, 255, 0, 255);

	public static Color Yellow = new Color(255, 255, 0, 255);

	public static Color Blue = new Color(0, 0, 255, 255);

	public static Color Magenta = new Color(255, 0, 255, 255);

	public static Color Cyan = new Color(0, 255, 255, 255);

	public static Color DarkRed = new Color(128, 0, 0, 255);

	public static Color DarkGreen = new Color(0, 128, 0, 255);

	public static Color DarkYellow = new Color(128, 128, 0, 255);

	public static Color DarkBlue = new Color(0, 0, 128, 255);

	public static Color DarkMagenta = new Color(128, 0, 128, 255);

	public static Color DarkCyan = new Color(0, 128, 128, 255);

	public static Color LightRed = new Color(255, 128, 128, 255);

	public static Color LightGreen = new Color(128, 255, 128, 255);

	public static Color LightYellow = new Color(255, 255, 128, 255);

	public static Color LightBlue = new Color(128, 128, 255, 255);

	public static Color LightMagenta = new Color(255, 128, 255, 255);

	public static Color LightCyan = new Color(128, 255, 255, 255);

	public static Color Orange = new Color(255, 128, 0, 255);

	public static Color Pink = new Color(255, 0, 128, 255);

	public static Color Chartreuse = new Color(128, 255, 0, 255);

	public static Color Violet = new Color(128, 0, 255, 255);

	public static Color MintGreen = new Color(0, 255, 128, 255);

	public static Color SkyBlue = new Color(0, 128, 255, 255);

	public static Color Brown = new Color(128, 64, 0, 255);

	public static Color Purple = new Color(128, 0, 64, 255);

	public static Color Olive = new Color(64, 128, 0, 255);

	public static Color Indigo = new Color(64, 0, 128, 255);

	public static Color MutedGreen = new Color(0, 128, 64, 255);

	public static Color InkBlue = new Color(0, 64, 128, 255);

	public static Color Transparent = default(Color);

	public byte R
	{
		get
		{
			return (byte)PackedValue;
		}
		set
		{
			PackedValue = (PackedValue & 0xFFFFFF00u) | value;
		}
	}

	public byte G
	{
		get
		{
			return (byte)(PackedValue >> 8);
		}
		set
		{
			PackedValue = (PackedValue & 0xFFFF00FFu) | (uint)(value << 8);
		}
	}

	public byte B
	{
		get
		{
			return (byte)(PackedValue >> 16);
		}
		set
		{
			PackedValue = (PackedValue & 0xFF00FFFFu) | (uint)(value << 16);
		}
	}

	public byte A
	{
		get
		{
			return (byte)(PackedValue >> 24);
		}
		set
		{
			PackedValue = (PackedValue & 0xFFFFFF) | (uint)(value << 24);
		}
	}

	public Color RGB
	{
		get
		{
			return new Color(PackedValue | 0xFF000000u);
		}
		set
		{
			PackedValue = (PackedValue & 0xFF000000u) | (value.PackedValue & 0xFFFFFF);
		}
	}

	public Color(uint packedValue)
	{
		PackedValue = packedValue;
	}

	public Color(byte r, byte g, byte b)
	{
		PackedValue = (uint)(-16777216 | (b << 16) | (g << 8) | r);
	}

	public Color(byte r, byte g, byte b, byte a)
	{
		PackedValue = (uint)((a << 24) | (b << 16) | (g << 8) | r);
	}

	public Color(int r, int g, int b, int a)
		: this((byte)MathUtils.Clamp(r, 0, 255), (byte)MathUtils.Clamp(g, 0, 255), (byte)MathUtils.Clamp(b, 0, 255), (byte)MathUtils.Clamp(a, 0, 255))
	{
	}

	public Color(int r, int g, int b)
		: this(r, g, b, 255)
	{
	}

	public Color(float r, float g, float b, float a)
		: this((byte)(MathUtils.Saturate(r) * 255f), (byte)(MathUtils.Saturate(g) * 255f), (byte)(MathUtils.Saturate(b) * 255f), (byte)(MathUtils.Saturate(a) * 255f))
	{
	}

	public Color(float r, float g, float b)
		: this((byte)(MathUtils.Saturate(r) * 255f), (byte)(MathUtils.Saturate(g) * 255f), (byte)(MathUtils.Saturate(b) * 255f), byte.MaxValue)
	{
	}

	public Color(Color rgb, byte a)
	{
		PackedValue = rgb.PackedValue;
		A = a;
	}

	public Color(Color rgb, int a)
		: this(rgb, (byte)MathUtils.Clamp(a, 0, 255))
	{
	}

	public Color(Color rgb, float a)
		: this(rgb, (byte)(MathUtils.Saturate(a) * 255f))
	{
	}

	public Color(Vector4 rgba)
		: this(rgba.X, rgba.Y, rgba.Z, rgba.W)
	{
	}

	public Color(Vector3 rgb)
		: this(rgb.X, rgb.Y, rgb.Z)
	{
	}

	public static implicit operator Color((byte R, byte G, byte B) v)
	{
		return new Color(v.R, v.G, v.B);
	}

	public static implicit operator Color((byte R, byte G, byte B, byte A) v)
	{
		return new Color(v.R, v.G, v.B, v.A);
	}

	public static implicit operator Color((int R, int G, int B) v)
	{
		return new Color(v.R, v.G, v.B);
	}

	public static implicit operator Color((int R, int G, int B, int A) v)
	{
		return new Color(v.R, v.G, v.B, v.A);
	}

	public static implicit operator Color((float R, float G, float B) v)
	{
		return new Color(v.R, v.G, v.B);
	}

	public static implicit operator Color((float R, float G, float B, float A) v)
	{
		return new Color(v.R, v.G, v.B, v.A);
	}

	public override int GetHashCode()
	{
		return (int)PackedValue;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Color))
		{
			return false;
		}
		return Equals((Color)obj);
	}

	public override string ToString()
	{
		return string.Format("{0},{1},{2},{3}", new object[4] { R, G, B, A });
	}

	public bool Equals(Color other)
	{
		return PackedValue == other.PackedValue;
	}

	public static Color Lerp(Color c1, Color c2, float f)
	{
		return new Color((int)MathUtils.Lerp((int)c1.R, (int)c2.R, f), (int)MathUtils.Lerp((int)c1.G, (int)c2.G, f), (int)MathUtils.Lerp((int)c1.B, (int)c2.B, f), (int)MathUtils.Lerp((int)c1.A, (int)c2.A, f));
	}

	public static Color LerpNotSaturated(Color c1, Color c2, float f)
	{
		return new Color((byte)MathUtils.Lerp((int)c1.R, (int)c2.R, f), (byte)MathUtils.Lerp((int)c1.G, (int)c2.G, f), (byte)MathUtils.Lerp((int)c1.B, (int)c2.B, f), (byte)MathUtils.Lerp((int)c1.A, (int)c2.A, f));
	}

	public static Color PremultiplyAlpha(Color c)
	{
		return new Color((byte)((float)(c.R * c.A) / 255f), (byte)((float)(c.G * c.A) / 255f), (byte)((float)(c.B * c.A) / 255f), c.A);
	}

	public static Vector4 PremultiplyAlpha(Vector4 c)
	{
		return new Vector4(c.X * c.W, c.Y * c.W, c.Z * c.W, c.W);
	}

	public static Color MultiplyAlphaOnly(Color c, float s)
	{
		return new Color(c.R, c.G, c.B, (byte)MathUtils.Clamp((float)(int)c.A * s, 0f, 255f));
	}

	public static Color MultiplyAlphaOnlyNotSaturated(Color c, float s)
	{
		return new Color(c.R, c.G, c.B, (byte)((float)(int)c.A * s));
	}

	public static Color MultiplyColorOnly(Color c, float s)
	{
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * s, 0f, 255f), c.A);
	}

	public static Color MultiplyColorOnlyNotSaturated(Color c, float s)
	{
		return new Color((byte)((float)(int)c.R * s), (byte)((float)(int)c.G * s), (byte)((float)(int)c.B * s), c.A);
	}

	public static Color MultiplyColorOnlyNotSaturated(Color c, Vector3 s)
	{
		return new Color((byte)((float)(int)c.R * s.X), (byte)((float)(int)c.G * s.Y), (byte)((float)(int)c.B * s.Z), c.A);
	}

	public static Color MultiplyNotSaturated(Color c, float s)
	{
		return new Color((byte)((float)(int)c.R * s), (byte)((float)(int)c.G * s), (byte)((float)(int)c.B * s), (byte)((float)(int)c.A * s));
	}

	public static Color MultiplyNotSaturated(Color c, Vector4 s)
	{
		return new Color((byte)((float)(int)c.R * s.X), (byte)((float)(int)c.G * s.Y), (byte)((float)(int)c.B * s.Z), (byte)((float)(int)c.A * s.W));
	}

	public static Vector3 RgbToHsv(Vector3 rgb)
	{
		float num = MathUtils.Min(rgb.X, rgb.Y, rgb.Z);
		float num2 = MathUtils.Max(rgb.X, rgb.Y, rgb.Z);
		float z = num2;
		float num3 = num2 - num;
		float y;
		float num4;
		if (num2 != 0f)
		{
			y = num3 / num2;
			num4 = ((num3 == 0f) ? 0f : ((rgb.X == num2) ? ((rgb.Y - rgb.Z) / num3) : ((rgb.Y != num2) ? (4f + (rgb.X - rgb.Y) / num3) : (2f + (rgb.Z - rgb.X) / num3))));
			num4 *= 60f;
			if (num4 < 0f)
			{
				num4 += 360f;
			}
			return new Vector3(num4, y, z);
		}
		y = 0f;
		num4 = -1f;
		return new Vector3(num4, y, z);
	}

	public static Vector3 HsvToRgb(Vector3 hsv)
	{
		if (hsv.Y == 0f)
		{
			return new Vector3(hsv.Z);
		}
		hsv.X /= 60f;
		int num = (int)MathUtils.Floor(hsv.X);
		float num2 = hsv.X - (float)num;
		float num3 = hsv.Z * (1f - hsv.Y);
		float num4 = hsv.Z * (1f - hsv.Y * num2);
		float num5 = hsv.Z * (1f - hsv.Y * (1f - num2));
		float x;
		float y;
		float z;
		switch (num)
		{
		case 0:
			x = hsv.Z;
			y = num5;
			z = num3;
			break;
		case 1:
			x = num4;
			y = hsv.Z;
			z = num3;
			break;
		case 2:
			x = num3;
			y = hsv.Z;
			z = num5;
			break;
		case 3:
			x = num3;
			y = num4;
			z = hsv.Z;
			break;
		case 4:
			x = num5;
			y = num3;
			z = hsv.Z;
			break;
		default:
			x = hsv.Z;
			y = num3;
			z = num4;
			break;
		}
		return new Vector3(x, y, z);
	}

	public static bool operator ==(Color c1, Color c2)
	{
		return c1.Equals(c2);
	}

	public static bool operator !=(Color c1, Color c2)
	{
		return !c1.Equals(c2);
	}

	public static Color operator *(Color c, float s)
	{
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * s, 0f, 255f));
	}

	public static Color operator *(float s, Color c)
	{
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * s, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * s, 0f, 255f));
	}

	public static Color operator *(Color c, Vector4 s)
	{
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * s.X, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * s.Y, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * s.Z, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * s.W, 0f, 255f));
	}

	public static Color operator *(Vector4 s, Color c)
	{
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * s.X, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * s.Y, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * s.Z, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * s.W, 0f, 255f));
	}

	public static Color operator /(Color c, float s)
	{
		float num = 1f / s;
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * num, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * num, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * num, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * num, 0f, 255f));
	}

	public static Color operator /(Color c, Vector4 s)
	{
		Vector4 vector = new Vector4(1f) / s;
		return new Color((byte)MathUtils.Clamp((float)(int)c.R * vector.X, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.G * vector.Y, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.B * vector.Z, 0f, 255f), (byte)MathUtils.Clamp((float)(int)c.A * vector.W, 0f, 255f));
	}

	public static Color operator +(Color c1, Color c2)
	{
		return new Color((byte)MathUtils.Min(c1.R + c2.R, 255), (byte)MathUtils.Min(c1.G + c2.G, 255), (byte)MathUtils.Min(c1.B + c2.B, 255), (byte)MathUtils.Min(c1.A + c2.A, 255));
	}

	public static Color operator -(Color c1, Color c2)
	{
		return new Color((byte)MathUtils.Max(c1.R - c2.R, 0), (byte)MathUtils.Max(c1.G - c2.G, 0), (byte)MathUtils.Max(c1.B - c2.B, 0), (byte)MathUtils.Max(c1.A - c2.A, 0));
	}

	public static Color operator *(Color c1, Color c2)
	{
		return new Color((byte)(c1.R * c2.R / 255), (byte)(c1.G * c2.G / 255), (byte)(c1.B * c2.B / 255), (byte)(c1.A * c2.A / 255));
	}
}
