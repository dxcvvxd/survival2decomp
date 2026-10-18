using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Engine.Graphics;

namespace Game;

public static class ScreensManager
{
	private class AnimationData
	{
		public Screen OldScreen;

		public Screen NewScreen;

		public float Factor;

		public float Speed;

		public object[] Parameters;
	}

	private static Dictionary<string, Screen> m_screens = new Dictionary<string, Screen>();

	private static AnimationData m_animationData;

	private static PrimitivesRenderer2D m_pr2 = new PrimitivesRenderer2D();

	private static PrimitivesRenderer3D m_pr3 = new PrimitivesRenderer3D();

	private static Random Random = new Random(0);

	private static RenderTarget2D m_uiRenderTarget;

	private static Vector3 m_vrQuadPosition;

	private static Matrix m_vrQuadMatrix;

	private static float DebugUiScale = 1f;

	public static ContainerWidget RootWidget { get; private set; }

	public static bool IsAnimating => m_animationData != null;

	public static Screen CurrentScreen { get; private set; }

	public static Screen PreviousScreen { get; private set; }

	public static T FindScreen<T>(string name) where T : Screen
	{
		m_screens.TryGetValue(name, out var value);
		return (T)value;
	}

	public static void AddScreen(string name, Screen screen)
	{
		m_screens.Add(name, screen);
	}

	public static void SwitchScreen(string name, params object[] parameters)
	{
		SwitchScreen(string.IsNullOrEmpty(name) ? null : FindScreen<Screen>(name), parameters);
	}

	public static void SwitchScreen(Screen screen, params object[] parameters)
	{
		if (m_animationData != null)
		{
			EndAnimation();
		}
		m_animationData = new AnimationData
		{
			NewScreen = screen,
			OldScreen = CurrentScreen,
			Parameters = parameters,
			Speed = ((CurrentScreen == null) ? float.MaxValue : 4f)
		};
		if (CurrentScreen != null)
		{
			RootWidget.IsUpdateEnabled = false;
			CurrentScreen.Input.Clear();
		}
		PreviousScreen = CurrentScreen;
		CurrentScreen = screen;
		UpdateAnimation();
		if (CurrentScreen != null)
		{
			Log.Verbose($"Entered screen \"{GetScreenName(CurrentScreen)}\"");
			AnalyticsManager.LogEvent($"[{GetScreenName(CurrentScreen)}] Entered screen", new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff")));
		}
	}

	public static void Initialize()
	{
		RootWidget = new CanvasWidget();
		RootWidget.WidgetsHierarchyInput = new WidgetInput();
		AddScreen("Loading", new LoadingScreen());
		LoadingManager.QueueItem("Initialize PlayerScreen", delegate
		{
			AddScreen("Player", new PlayerScreen());
		});
		LoadingManager.QueueItem("Initialize NagScreen", delegate
		{
			AddScreen("Nag", new NagScreen());
		});
		LoadingManager.QueueItem("Initialize MainMenuScreen", delegate
		{
			AddScreen("MainMenu", new MainMenuScreen());
		});
		LoadingManager.QueueItem("Initialize RecipaediaScreen", delegate
		{
			AddScreen("Recipaedia", new RecipaediaScreen());
		});
		LoadingManager.QueueItem("Initialize RecipaediaRecipesScreen", delegate
		{
			AddScreen("RecipaediaRecipes", new RecipaediaRecipesScreen());
		});
		LoadingManager.QueueItem("Initialize RecipaediaDescriptionScreen", delegate
		{
			AddScreen("RecipaediaDescription", new RecipaediaDescriptionScreen());
		});
		LoadingManager.QueueItem("Initialize BestiaryScreen", delegate
		{
			AddScreen("Bestiary", new BestiaryScreen());
		});
		LoadingManager.QueueItem("Initialize BestiaryDescriptionScreen", delegate
		{
			AddScreen("BestiaryDescription", new BestiaryDescriptionScreen());
		});
		LoadingManager.QueueItem("Initialize HelpScreen", delegate
		{
			AddScreen("Help", new HelpScreen());
		});
		LoadingManager.QueueItem("Initialize HelpTopicScreen", delegate
		{
			AddScreen("HelpTopic", new HelpTopicScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsScreen", delegate
		{
			AddScreen("Settings", new SettingsScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsPerformanceScreen", delegate
		{
			AddScreen("SettingsPerformance", new SettingsPerformanceScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsGraphicsScreen", delegate
		{
			AddScreen("SettingsGraphics", new SettingsGraphicsScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsUiScreen", delegate
		{
			AddScreen("SettingsUi", new SettingsUiScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsCompatibilityScreen", delegate
		{
			AddScreen("SettingsCompatibility", new SettingsCompatibilityScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsAudioScreen", delegate
		{
			AddScreen("SettingsAudio", new SettingsAudioScreen());
		});
		LoadingManager.QueueItem("Initialize SettingsControlsScreen", delegate
		{
			AddScreen("SettingsControls", new SettingsControlsScreen());
		});
		LoadingManager.QueueItem("Initialize PlayScreen", delegate
		{
			AddScreen("Play", new PlayScreen());
		});
		LoadingManager.QueueItem("Initialize NewWorldScreen", delegate
		{
			AddScreen("NewWorld", new NewWorldScreen());
		});
		LoadingManager.QueueItem("Initialize ModifyWorldScreen", delegate
		{
			AddScreen("ModifyWorld", new ModifyWorldScreen());
		});
		LoadingManager.QueueItem("Initialize WorldOptionsScreen", delegate
		{
			AddScreen("WorldOptions", new WorldOptionsScreen());
		});
		LoadingManager.QueueItem("Initialize GameLoadingScreen", delegate
		{
			AddScreen("GameLoading", new GameLoadingScreen());
		});
		LoadingManager.QueueItem("Initialize GameScreen", delegate
		{
			AddScreen("Game", new GameScreen());
		});
		LoadingManager.QueueItem("Initialize TrialEndedScreen", delegate
		{
			AddScreen("TrialEnded", new TrialEndedScreen());
		});
		LoadingManager.QueueItem("Initialize ContentScreen", delegate
		{
			AddScreen("Content", new ContentScreen());
		});
		LoadingManager.QueueItem("Initialize ExternalContentScreen", delegate
		{
			AddScreen("ExternalContent", new ExternalContentScreen());
		});
		LoadingManager.QueueItem("Initialize CommunityContentScreen", delegate
		{
			AddScreen("CommunityContent", new CommunityContentScreen());
		});
		LoadingManager.QueueItem("Initialize ManageContentScreen", delegate
		{
			AddScreen("ManageContent", new ManageContentScreen());
		});
		LoadingManager.QueueItem("Initialize PlayersScreen", delegate
		{
			AddScreen("Players", new PlayersScreen());
		});
		SwitchScreen("Loading");
	}

	public static void Update()
	{
		if (m_animationData != null)
		{
			UpdateAnimation();
		}
		if (VrManager.IsVrStarted)
		{
			AnimateVrQuad();
		}
		Widget.UpdateWidgetsHierarchy(RootWidget);
	}

	public static void Draw()
	{
		if (VrManager.IsVrStarted)
		{
			Point2 point = new Point2(Display.Viewport.Width, Display.Viewport.Height);
			if (MathUtils.Max(point.X, point.Y) == 0)
			{
				point = new Point2(1500, 1000);
			}
			while (MathUtils.Max(point.X, point.Y) < 1024)
			{
				point *= 2;
			}
			if (m_uiRenderTarget == null || m_uiRenderTarget.Width != point.X || m_uiRenderTarget.Height != point.Y)
			{
				Utilities.Dispose(ref m_uiRenderTarget);
				m_uiRenderTarget = new RenderTarget2D(point.X, point.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
			}
			RenderTarget2D renderTarget = Display.RenderTarget;
			try
			{
				Display.RenderTarget = m_uiRenderTarget;
				LayoutAndDrawWidgets();
				Display.RenderTarget = VrManager.VrRenderTarget;
				for (VrEye vrEye = VrEye.Left; vrEye <= VrEye.Right; vrEye++)
				{
					Display.Clear(Color.Black, 1f, 0);
					DrawVrBackground();
					DrawVrQuad();
					Matrix hmdMatrix = VrManager.HmdMatrix;
					Matrix matrix = Matrix.Invert(VrManager.GetEyeToHeadTransform(vrEye));
					Matrix matrix2 = Matrix.Invert(hmdMatrix);
					Matrix projectionMatrix = VrManager.GetProjectionMatrix(vrEye, 0.1f, 1024f);
					m_pr3.Flush(matrix2 * matrix * projectionMatrix);
					VrManager.SubmitEyeTexture(vrEye, VrManager.VrRenderTarget);
				}
			}
			finally
			{
				Display.RenderTarget = renderTarget;
			}
			m_pr2.TexturedBatch(m_uiRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.Opaque, SamplerState.PointClamp).QueueQuad(new Vector2(0f, 0f), new Vector2(m_uiRenderTarget.Width, m_uiRenderTarget.Height), 0f, new Vector2(0f, 0f), new Vector2(1f, 1f), Color.White);
			m_pr2.Flush();
		}
		else
		{
			Utilities.Dispose(ref m_uiRenderTarget);
			LayoutAndDrawWidgets();
		}
	}

	private static void UpdateAnimation()
	{
		float num = MathUtils.Min(Time.FrameDuration, 0.1f);
		float factor = m_animationData.Factor;
		m_animationData.Factor = MathUtils.Min(m_animationData.Factor + m_animationData.Speed * num, 1f);
		if (m_animationData.Factor < 0.5f)
		{
			if (m_animationData.OldScreen != null)
			{
				float num2 = 2f * (0.5f - m_animationData.Factor);
				float scale = 1f;
				m_animationData.OldScreen.ColorTransform = new Color(num2, num2, num2, num2);
				m_animationData.OldScreen.RenderTransform = Matrix.CreateTranslation((0f - m_animationData.OldScreen.ActualSize.X) / 2f, (0f - m_animationData.OldScreen.ActualSize.Y) / 2f, 0f) * Matrix.CreateScale(scale) * Matrix.CreateTranslation(m_animationData.OldScreen.ActualSize.X / 2f, m_animationData.OldScreen.ActualSize.Y / 2f, 0f);
			}
		}
		else if (factor < 0.5f)
		{
			if (m_animationData.OldScreen != null)
			{
				m_animationData.OldScreen.Leave();
				RootWidget.Children.Remove(m_animationData.OldScreen);
			}
			if (m_animationData.NewScreen != null)
			{
				RootWidget.Children.Insert(0, m_animationData.NewScreen);
				m_animationData.NewScreen.Enter(m_animationData.Parameters);
				m_animationData.NewScreen.ColorTransform = Color.Transparent;
				RootWidget.IsUpdateEnabled = true;
			}
		}
		else if (m_animationData.NewScreen != null)
		{
			float num3 = 2f * (m_animationData.Factor - 0.5f);
			float scale2 = 1f;
			m_animationData.NewScreen.ColorTransform = new Color(num3, num3, num3, num3);
			m_animationData.NewScreen.RenderTransform = Matrix.CreateTranslation((0f - m_animationData.NewScreen.ActualSize.X) / 2f, (0f - m_animationData.NewScreen.ActualSize.Y) / 2f, 0f) * Matrix.CreateScale(scale2) * Matrix.CreateTranslation(m_animationData.NewScreen.ActualSize.X / 2f, m_animationData.NewScreen.ActualSize.Y / 2f, 0f);
		}
		if (m_animationData.Factor >= 1f)
		{
			EndAnimation();
		}
	}

	private static void EndAnimation()
	{
		if (m_animationData.NewScreen != null)
		{
			m_animationData.NewScreen.ColorTransform = Color.White;
			m_animationData.NewScreen.RenderTransform = Matrix.CreateScale(1f);
		}
		m_animationData = null;
	}

	private static string GetScreenName(Screen screen)
	{
		string key = m_screens.FirstOrDefault((KeyValuePair<string, Screen> kvp) => kvp.Value == screen).Key;
		if (key == null)
		{
			return string.Empty;
		}
		return key;
	}

	private static void AnimateVrQuad()
	{
		if (Time.FrameIndex >= 5)
		{
			float num = 6f;
			Matrix hmdMatrix = VrManager.HmdMatrix;
			Vector3 vector = hmdMatrix.Translation + num * (Vector3.Normalize(hmdMatrix.Forward * new Vector3(1f, 0f, 1f)) + new Vector3(0f, 0.1f, 0f));
			if (m_vrQuadPosition == Vector3.Zero)
			{
				m_vrQuadPosition = vector;
			}
			if (Vector3.Distance(m_vrQuadPosition, vector) > 0f)
			{
				Vector3 vector2 = vector * new Vector3(1f, 0f, 1f) - m_vrQuadPosition * new Vector3(1f, 0f, 1f);
				Vector3 vector3 = vector * new Vector3(0f, 1f, 0f) - m_vrQuadPosition * new Vector3(0f, 1f, 0f);
				float num2 = vector2.Length();
				float num3 = vector3.Length();
				m_vrQuadPosition += vector2 * MathUtils.Min(0.75f * MathUtils.Pow(MathUtils.Max(num2 - 0.15f * num, 0f), 0.33f) * Time.FrameDuration, 1f);
				m_vrQuadPosition += vector3 * MathUtils.Min(1.5f * MathUtils.Pow(MathUtils.Max(num3 - 0.05f * num, 0f), 0.33f) * Time.FrameDuration, 1f);
			}
			Vector2 vector4 = new Vector2((float)m_uiRenderTarget.Width / (float)m_uiRenderTarget.Height, 1f);
			vector4 /= MathUtils.Max(vector4.X, vector4.Y);
			vector4 *= 7.5f;
			m_vrQuadMatrix.Forward = Vector3.Normalize(hmdMatrix.Translation - m_vrQuadPosition);
			m_vrQuadMatrix.Right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, m_vrQuadMatrix.Forward)) * vector4.X;
			m_vrQuadMatrix.Up = Vector3.Normalize(Vector3.Cross(m_vrQuadMatrix.Forward, m_vrQuadMatrix.Right)) * vector4.Y;
			m_vrQuadMatrix.Translation = m_vrQuadPosition - 0.5f * (m_vrQuadMatrix.Right + m_vrQuadMatrix.Up);
			RootWidget.WidgetsHierarchyInput.VrQuadMatrix = m_vrQuadMatrix;
		}
	}

	private static void DrawVrQuad()
	{
		QueueQuad(m_pr3.TexturedBatch(m_uiRenderTarget, useAlphaTest: false, 0, DepthStencilState.Default, RasterizerState.CullNoneScissor, BlendState.Opaque, SamplerState.LinearClamp), m_vrQuadMatrix.Translation, m_vrQuadMatrix.Right, m_vrQuadMatrix.Up, Color.White);
	}

	private static void DrawVrBackground()
	{
		Matrix hmdMatrix = VrManager.HmdMatrix;
		TexturedBatch3D batch = m_pr3.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Star"));
		Random.Seed(0);
		for (int i = 0; i < 1500; i++)
		{
			float f = MathUtils.Pow(Random.Float(0f, 1f), 6f);
			Color rGB = (MathUtils.Lerp(0.05f, 0.4f, f) * Color.White).RGB;
			int num = 6;
			Vector3 vector = Random.Vector3(500f);
			Vector3 vector2 = Vector3.Normalize(Vector3.Cross(vector, Vector3.UnitY)) * num;
			Vector3 up = Vector3.Normalize(Vector3.Cross(vector2, vector)) * num;
			QueueQuad(batch, vector + hmdMatrix.Translation, vector2, up, rGB);
		}
		TexturedBatch3D batch2 = m_pr3.TexturedBatch(ContentManager.Get<Texture2D>("Textures/Blocks"), useAlphaTest: true, 1, null, null, null, SamplerState.PointClamp);
		for (int j = -8; j <= 8; j++)
		{
			for (int k = -8; k <= 8; k++)
			{
				float num2 = 1f;
				float num3 = 1f;
				Vector3 vector3 = new Vector3(((float)j - 0.5f) * num2, 0f, ((float)k - 0.5f) * num2) + new Vector3(MathUtils.Round(hmdMatrix.Translation.X), 0f, MathUtils.Round(hmdMatrix.Translation.Z));
				float num4 = Vector3.Distance(vector3, hmdMatrix.Translation);
				float num5 = MathUtils.Lerp(1f, 0f, MathUtils.Saturate(num4 / 7f));
				if (num5 > 0f)
				{
					QueueQuad(batch2, vector3, new Vector3(num3, 0f, 0f), new Vector3(0f, 0f, num3), Color.Gray * num5, new Vector2(0.1875f, 0.25f), new Vector2(0.25f, 0.3125f));
				}
			}
		}
	}

	private static void LayoutAndDrawWidgets()
	{
		if (m_animationData != null)
		{
			Display.Clear(Color.Black, 1f, 0);
		}
		float num = 850f / MathUtils.Clamp(SettingsManager.UIScale, 0.5f, 1f) * DebugUiScale;
		Vector2 vector = new Vector2(Display.Viewport.Width, Display.Viewport.Height);
		float num2 = vector.X / num;
		Vector2 availableSize = new Vector2(num, num / vector.X * vector.Y);
		float num3 = num * 9f / 16f;
		if (vector.Y / num2 < num3)
		{
			num2 = vector.Y / num3;
			availableSize = new Vector2(num3 / vector.Y * vector.X, num3);
		}
		RootWidget.LayoutTransform = Matrix.CreateScale(num2, num2, 1f);
		if (SettingsManager.UpsideDownLayout)
		{
			RootWidget.LayoutTransform *= new Matrix(-1f, 0f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
		}
		Widget.LayoutWidgetsHierarchy(RootWidget, availableSize);
		Widget.DrawWidgetsHierarchy(RootWidget);
	}

	private static void QueueQuad(FlatBatch3D batch, Vector3 corner, Vector3 right, Vector3 up, Color color)
	{
		Vector3 p = corner + right;
		Vector3 p2 = corner + right + up;
		Vector3 p3 = corner + up;
		batch.QueueQuad(corner, p, p2, p3, color);
	}

	private static void QueueQuad(TexturedBatch3D batch, Vector3 center, Vector3 right, Vector3 up, Color color)
	{
		QueueQuad(batch, center, right, up, color, new Vector2(0f, 0f), new Vector2(1f, 1f));
	}

	private static void QueueQuad(TexturedBatch3D batch, Vector3 corner, Vector3 right, Vector3 up, Color color, Vector2 tc1, Vector2 tc2)
	{
		Vector3 p = corner + right;
		Vector3 p2 = corner + right + up;
		Vector3 p3 = corner + up;
		batch.QueueQuad(corner, p, p2, p3, new Vector2(tc1.X, tc2.Y), new Vector2(tc2.X, tc2.Y), new Vector2(tc2.X, tc1.Y), new Vector2(tc1.X, tc1.Y), color);
	}
}
