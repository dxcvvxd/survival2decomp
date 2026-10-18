using System;
using Engine;
using Engine.Graphics;

namespace Game;

public class ViewWidget : TouchInputWidget, IDragTargetWidget
{
	private SubsystemDrawing m_subsystemDrawing;

	private RenderTarget2D m_scalingRenderTarget;

	public GameWidget GameWidget { get; private set; }

	public Point2? ScalingRenderTargetSize
	{
		get
		{
			if (m_scalingRenderTarget == null)
			{
				return null;
			}
			return new Point2(m_scalingRenderTarget.Width, m_scalingRenderTarget.Height);
		}
	}

	internal override void ChangeParent(ContainerWidget parentWidget)
	{
		if (parentWidget is GameWidget)
		{
			GameWidget = (GameWidget)parentWidget;
			m_subsystemDrawing = GameWidget.SubsystemGameWidgets.Project.FindSubsystem<SubsystemDrawing>(throwOnError: true);
			base.ChangeParent(parentWidget);
			return;
		}
		throw new InvalidOperationException("ViewWidget must be a child of GameWidget.");
	}

	public override void MeasureOverride(Vector2 parentAvailableSize)
	{
		base.IsDrawRequired = true;
		base.MeasureOverride(parentAvailableSize);
	}

	public override void Draw(DrawContext dc)
	{
		if (GameWidget.PlayerData.ComponentPlayer != null && GameWidget.PlayerData.IsReadyForPlaying && !GameWidget.PlayerData.ComponentPlayer.ComponentInput.IsControlledByVr)
		{
			DrawToScreen(dc);
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		Utilities.Dispose(ref m_scalingRenderTarget);
	}

	public void DragOver(Widget dragWidget, object data)
	{
	}

	public void DragDrop(Widget dragWidget, object data)
	{
		if (data is InventoryDragData inventoryDragData && GameManager.Project != null)
		{
			SubsystemPickables subsystemPickables = GameManager.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			ComponentPlayer componentPlayer = GameWidget.PlayerData.ComponentPlayer;
			int slotValue = inventoryDragData.Inventory.GetSlotValue(inventoryDragData.SlotIndex);
			int count = ((componentPlayer != null && componentPlayer.ComponentInput.SplitSourceInventory == inventoryDragData.Inventory && componentPlayer.ComponentInput.SplitSourceSlotIndex == inventoryDragData.SlotIndex) ? 1 : ((inventoryDragData.DragMode != DragMode.SingleItem) ? inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex) : MathUtils.Min(inventoryDragData.Inventory.GetSlotCount(inventoryDragData.SlotIndex), 1)));
			int num = inventoryDragData.Inventory.RemoveSlotItems(inventoryDragData.SlotIndex, count);
			if (num > 0)
			{
				Vector2 vector = dragWidget.WidgetToScreen(dragWidget.ActualSize / 2f);
				Vector3 value = Vector3.Normalize(GameWidget.ActiveCamera.ScreenToWorld(new Vector3(vector.X, vector.Y, 1f), Matrix.Identity) - GameWidget.ActiveCamera.ViewPosition) * 12f;
				subsystemPickables.AddPickable(slotValue, num, GameWidget.ActiveCamera.ViewPosition, value, null);
			}
		}
	}

	private void SetupScalingRenderTarget()
	{
		float num = ((SettingsManager.ResolutionMode == ResolutionMode.Low) ? 0.5f : ((SettingsManager.ResolutionMode != ResolutionMode.Medium) ? 1f : 0.75f));
		float num2 = base.GlobalTransform.Right.Length();
		float num3 = base.GlobalTransform.Up.Length();
		Vector2 vector = new Vector2(base.ActualSize.X * num2, base.ActualSize.Y * num3);
		Point2 point = new Point2
		{
			X = (int)MathUtils.Round(vector.X * num),
			Y = (int)MathUtils.Round(vector.Y * num)
		};
		if ((num < 1f || base.GlobalColorTransform != Color.White) && point.X > 0 && point.Y > 0)
		{
			if (m_scalingRenderTarget == null || m_scalingRenderTarget.Width != point.X || m_scalingRenderTarget.Height != point.Y)
			{
				Utilities.Dispose(ref m_scalingRenderTarget);
				m_scalingRenderTarget = new RenderTarget2D(point.X, point.Y, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8);
			}
			Display.RenderTarget = m_scalingRenderTarget;
			Display.Clear(Color.Black, 1f, 0);
		}
		else
		{
			Utilities.Dispose(ref m_scalingRenderTarget);
		}
	}

	private void ApplyScalingRenderTarget(DrawContext dc)
	{
		if (m_scalingRenderTarget != null)
		{
			BlendState blendState = ((base.GlobalColorTransform.A < byte.MaxValue) ? BlendState.AlphaBlend : BlendState.Opaque);
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(m_scalingRenderTarget, useAlphaTest: false, 0, DepthStencilState.None, RasterizerState.CullNoneScissor, blendState, SamplerState.PointClamp);
			int count = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueQuad(Vector2.Zero, base.ActualSize, 0f, Vector2.Zero, Vector2.One, base.GlobalColorTransform);
			texturedBatch2D.TransformTriangles(base.GlobalTransform, count);
			dc.PrimitivesRenderer2D.Flush();
		}
	}

	private void DrawToScreen(DrawContext dc)
	{
		GameWidget.ActiveCamera.PrepareForDrawing(null);
		RenderTarget2D renderTarget = Display.RenderTarget;
		SetupScalingRenderTarget();
		try
		{
			m_subsystemDrawing.Draw(GameWidget.ActiveCamera);
		}
		finally
		{
			Display.RenderTarget = renderTarget;
		}
		ApplyScalingRenderTarget(dc);
	}
}
