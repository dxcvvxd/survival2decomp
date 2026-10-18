using System;
using System.Linq;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Media;

namespace Game;

public class MessageWidget : StackPanelWidget
{
	private class Message
	{
		public LabelWidget LabelWidget;

		public double StartTime;

		public float Duration;

		public Color Color;

		public bool Blinking;

		public Message(string text, Color color, bool blinking)
		{
			LabelWidget = new LabelWidget
			{
				Text = text,
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles24"),
				HorizontalAlignment = WidgetAlignment.Center,
				TextAnchor = TextAnchor.Center,
				DropShadow = true,
				WordWrap = true
			};
			StartTime = Time.FrameStartTime;
			Duration = (blinking ? 6f : (4f + MathUtils.Min(1f * (float)text.Count((char c) => c == '\n'), 4f)));
			Color = color;
			Blinking = blinking;
		}

		public void Update()
		{
			float num;
			if (Blinking)
			{
				num = MathUtils.Saturate(1f * (float)(StartTime + (double)Duration - Time.FrameStartTime));
				if (Time.FrameStartTime - StartTime < 0.417)
				{
					num *= MathUtils.Lerp(0.25f, 1f, 0.5f * (1f - MathUtils.Cos((float)Math.PI * 12f * (float)(Time.FrameStartTime - StartTime))));
				}
			}
			else
			{
				num = MathUtils.Saturate(MathUtils.Min(3f * (float)(Time.FrameStartTime - StartTime), 1f * (float)(StartTime + (double)Duration - Time.FrameStartTime)));
			}
			LabelWidget.Color = Color * num;
		}
	}

	private const int MaxMessages = 3;

	private DynamicArray<Message> m_messages = new DynamicArray<Message>();

	public MessageWidget()
	{
		XElement node = ContentManager.Get<XElement>("Widgets/MessageWidget");
		LoadContents(this, node);
	}

	public void DisplayMessage(string text, Color color, bool blinking)
	{
		if (!string.IsNullOrEmpty(text))
		{
			AddMessage(new Message(text, color, blinking));
			RemoveOldMessages();
		}
	}

	public override void Update()
	{
		for (int num = m_messages.Count - 1; num >= 0; num--)
		{
			m_messages[num].Update();
		}
		RemoveOldMessages();
	}

	private void AddMessage(Message message)
	{
		m_messages.Add(message);
		Children.Add(message.LabelWidget);
	}

	private void RemoveMessage(Message message)
	{
		m_messages.Remove(message);
		Children.Remove(message.LabelWidget);
	}

	private void RemoveOldMessages()
	{
		for (int num = m_messages.Count - 1; num >= 0; num--)
		{
			Message message = m_messages[num];
			int num2 = m_messages.Count - num - 1;
			if (Time.FrameStartTime >= message.StartTime + (double)message.Duration || num2 >= 3 || (num2 > 0 && !message.Blinking))
			{
				RemoveMessage(message);
			}
		}
	}
}
