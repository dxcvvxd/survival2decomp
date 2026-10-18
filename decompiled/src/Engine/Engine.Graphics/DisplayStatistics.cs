namespace Engine.Graphics;

public struct DisplayStatistics
{
	public int FrameIndex;

	public long BufferDrawCallsCount;

	public long UserDrawCallsCount;

	public long BufferRenderedPrimitivesCount;

	public long UserRenderedPrimitivesCount;

	public long CalculateGpuMemoryUsage()
	{
		long num = 8 * Display.BackbufferSize.X * Display.BackbufferSize.Y;
		foreach (GraphicsResource resource in GraphicsResource.m_resources)
		{
			num += resource.GetGpuMemoryUsage();
		}
		return num;
	}
}
