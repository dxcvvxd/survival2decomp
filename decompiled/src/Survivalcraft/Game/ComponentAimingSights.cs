using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game;

public class ComponentAimingSights : Component, IUpdateable, IDrawable
{
	private ComponentPlayer m_componentPlayer;

	private readonly PrimitivesRenderer2D m_primitivesRenderer2D = new PrimitivesRenderer2D();

	private readonly PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

	private Vector3 m_sightsPosition;

	private Vector3 m_sightsDirection;

	private static int[] m_drawOrders = new int[1] { 2000 };

	public bool IsSightsVisible { get; private set; }

	public UpdateOrder UpdateOrder => UpdateOrder.Reset;

	public int[] DrawOrders => m_drawOrders;

	public void ShowAimingSights(Vector3 position, Vector3 direction)
	{
		IsSightsVisible = true;
		m_sightsPosition = position;
		m_sightsDirection = direction;
	}

	public void Update(float dt)
	{
		IsSightsVisible = false;
	}

	public void Draw(Camera camera, int drawOrder)
	{
		if (camera.GameWidget != m_componentPlayer.GameWidget)
		{
			return;
		}
		if (m_componentPlayer.ComponentHealth.Health > 0f && m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible)
		{
			// Linux desktop port: the aiming "sights" sprite is a mobile affordance. It is
			// placed in world space from the aim ray captured early in the frame (before the
			// camera applies this frame's rotation), so it wobbles relative to the view while
			// turning. With mouse/keyboard input the plain crosshair below is used instead.
			if (IsSightsVisible && m_componentPlayer.ComponentInput.IsControlledByTouch)
			{
				Texture2D texture = ContentManager.Get<Texture2D>("Textures/Gui/Sights");
				float num = ((!camera.Eye.HasValue) ? 8f : 2.5f);
				Vector3 vector = m_sightsPosition + m_sightsDirection * 50f;
				Vector3 vector2 = Vector3.Normalize(Vector3.Cross(m_sightsDirection, Vector3.UnitY));
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(m_sightsDirection, vector2));
				Vector3 p = vector + num * (-vector2 - vector3);
				Vector3 p2 = vector + num * (vector2 - vector3);
				Vector3 p3 = vector + num * (vector2 + vector3);
				Vector3 p4 = vector + num * (-vector2 + vector3);
				TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(texture, useAlphaTest: false, 0, DepthStencilState.None);
				int count = texturedBatch3D.TriangleVertices.Count;
				texturedBatch3D.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), Color.White);
				texturedBatch3D.TransformTriangles(camera.ViewMatrix, count);
			}
			// Linux desktop port: the crosshair is no longer suppressed while aiming (see above).
			if (!camera.Eye.HasValue && !camera.UsesMovementControls && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch))
			{
				Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Crosshair");
				float num2 = 1.25f;
				Vector3 vector4 = camera.ViewPosition + camera.ViewDirection * 50f;
				Vector3 vector5 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY));
				Vector3 vector6 = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, vector5));
				Vector3 p5 = vector4 + num2 * (-vector5 - vector6);
				Vector3 p6 = vector4 + num2 * (vector5 - vector6);
				Vector3 p7 = vector4 + num2 * (vector5 + vector6);
				Vector3 p8 = vector4 + num2 * (-vector5 + vector6);
				TexturedBatch3D texturedBatch3D2 = m_primitivesRenderer3D.TexturedBatch(subtexture.Texture, useAlphaTest: false, 0, DepthStencilState.None);
				int count2 = texturedBatch3D2.TriangleVertices.Count;
				texturedBatch3D2.QueueQuad(p5, p6, p7, p8, new Vector2(subtexture.TopLeft.X, subtexture.TopLeft.Y), new Vector2(subtexture.BottomRight.X, subtexture.TopLeft.Y), new Vector2(subtexture.BottomRight.X, subtexture.BottomRight.Y), new Vector2(subtexture.TopLeft.X, subtexture.BottomRight.Y), Color.White);
				texturedBatch3D2.TransformTriangles(camera.ViewMatrix, count2);
			}
		}
		m_primitivesRenderer2D.Flush();
		m_primitivesRenderer3D.Flush(camera.ProjectionMatrix);
	}

	protected override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
	{
		m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
	}
}
