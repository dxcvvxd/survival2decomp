using System;
using System.Collections.Generic;
using System.Globalization;
using Engine;
using Engine.Graphics;

namespace Game;

public class TextureAtlas
{
	private Texture2D m_texture;

	private Dictionary<string, Rectangle> m_rectangles = new Dictionary<string, Rectangle>();

	public Texture2D Texture => m_texture;

	public TextureAtlas(Texture2D texture, string atlasDefinition, string prefix)
	{
		m_texture = texture;
		string[] array = atlasDefinition.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (array2.Length < 5)
			{
				throw new InvalidOperationException("Invalid texture atlas definition.");
			}
			string key = prefix + array2[0];
			Rectangle value = new Rectangle
			{
				Left = int.Parse(array2[1], CultureInfo.InvariantCulture),
				Top = int.Parse(array2[2], CultureInfo.InvariantCulture),
				Width = int.Parse(array2[3], CultureInfo.InvariantCulture),
				Height = int.Parse(array2[4], CultureInfo.InvariantCulture)
			};
			m_rectangles.Add(key, value);
		}
	}

	public bool ContainsTexture(string textureName)
	{
		return m_rectangles.ContainsKey(textureName);
	}

	public Vector4? GetTextureCoordinates(string textureName)
	{
		if (m_rectangles.TryGetValue(textureName, out var value))
		{
			return new Vector4
			{
				X = (float)value.Left / (float)m_texture.Width,
				Y = (float)value.Top / (float)m_texture.Height,
				Z = (float)value.Right / (float)m_texture.Width,
				W = (float)value.Bottom / (float)m_texture.Height
			};
		}
		return null;
	}
}
