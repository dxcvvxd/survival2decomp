using System.Xml.Linq;
using Engine;
using Engine.Media;

namespace Game;

public class EditPaletteDialog : Dialog
{
	private ContainerWidget m_listPanel;

	private ButtonWidget m_okButton;

	private ButtonWidget m_cancelButton;

	private LinkWidget[] m_labels = new LinkWidget[16];

	private BevelledButtonWidget[] m_rectangles = new BevelledButtonWidget[16];

	private ButtonWidget[] m_resetButtons = new ButtonWidget[16];

	private WorldPalette m_palette;

	private WorldPalette m_tmpPalette;

	public EditPaletteDialog(WorldPalette palette)
	{
		XElement node = ContentManager.Get<XElement>("Dialogs/EditPaletteDialog");
		LoadContents(this, node);
		m_listPanel = Children.Find<ContainerWidget>("EditPaletteDialog.ListPanel");
		m_okButton = Children.Find<ButtonWidget>("EditPaletteDialog.OK");
		m_cancelButton = Children.Find<ButtonWidget>("EditPaletteDialog.Cancel");
		for (int i = 0; i < 16; i++)
		{
			StackPanelWidget obj = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				Children = 
				{
					(Widget)new CanvasWidget
					{
						Size = new Vector2(32f, 60f),
						Children = { (Widget)new LabelWidget
						{
							Text = i + 1 + ".",
							Color = Color.Gray,
							HorizontalAlignment = WidgetAlignment.Far,
							VerticalAlignment = WidgetAlignment.Center,
							Font = ContentManager.Get<BitmapFont>("Fonts/Pericles24")
						} }
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(10f, 0f)
					}
				}
			};
			obj.Children.Add(m_labels[i] = new LinkWidget
			{
				Size = new Vector2(300f, -1f),
				VerticalAlignment = WidgetAlignment.Center,
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles24")
			});
			obj.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			obj.Children.Add(m_rectangles[i] = new BevelledButtonWidget
			{
				Size = new Vector2(float.PositiveInfinity, 60f),
				BevelSize = 1f,
				AmbientLight = 1f,
				DirectionalLight = 0.4f,
				VerticalAlignment = WidgetAlignment.Center
			});
			obj.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			obj.Children.Add(m_resetButtons[i] = new BevelledButtonWidget
			{
				Size = new Vector2(160f, 60f),
				VerticalAlignment = WidgetAlignment.Center,
				Text = "Reset"
			});
			obj.Children.Add(new CanvasWidget
			{
				Size = new Vector2(10f, 0f)
			});
			StackPanelWidget widget = obj;
			m_listPanel.Children.Add(widget);
		}
		m_palette = palette;
		m_tmpPalette = new WorldPalette();
		m_palette.CopyTo(m_tmpPalette);
	}

	public override void Update()
	{
		for (int i = 0; i < 16; i++)
		{
			m_labels[i].Text = m_tmpPalette.Names[i];
			m_rectangles[i].CenterColor = m_tmpPalette.Colors[i];
			m_resetButtons[i].IsEnabled = m_tmpPalette.Colors[i] != WorldPalette.DefaultColors[i] || m_tmpPalette.Names[i] != WorldPalette.DefaultNames[i];
		}
		for (int j = 0; j < 16; j++)
		{
			int n = j;
			if (m_labels[j].IsClicked)
			{
				DialogsManager.ShowDialog(this, new TextBoxDialog("Edit Color Name", m_labels[j].Text, 16, delegate(string s)
				{
					if (s != null)
					{
						if (WorldPalette.VerifyColorName(s))
						{
							m_tmpPalette.Names[n] = s;
						}
						else
						{
							DialogsManager.ShowDialog(this, new MessageDialog("Invalid name", null, "OK", null, null));
						}
					}
				}));
			}
			if (m_rectangles[j].IsClicked)
			{
				DialogsManager.ShowDialog(this, new EditColorDialog(m_tmpPalette.Colors[j], delegate(Color? color)
				{
					if (color.HasValue)
					{
						m_tmpPalette.Colors[n] = color.Value;
					}
				}));
			}
			if (m_resetButtons[j].IsClicked)
			{
				m_tmpPalette.Colors[j] = WorldPalette.DefaultColors[j];
				m_tmpPalette.Names[j] = WorldPalette.DefaultNames[j];
			}
		}
		if (m_okButton.IsClicked)
		{
			m_tmpPalette.CopyTo(m_palette);
			Dismiss();
		}
		if (base.Input.Cancel || m_cancelButton.IsClicked)
		{
			Dismiss();
		}
	}

	private void Dismiss()
	{
		DialogsManager.HideDialog(this);
	}
}
