namespace Engine;

public class FileLogSink : ILogSink
{
	private string m_path;

	private StreamLogSink m_streamLogSink;

	public FileLogSink(string path)
	{
		m_path = path;
	}

	public void Log(LogType logType, string message)
	{
		try
		{
			if (m_streamLogSink == null)
			{
				CreateStreamLogSink();
			}
			m_streamLogSink.Log(logType, message);
		}
		catch
		{
			try
			{
				if (!Storage.FileExists(m_path))
				{
					CreateStreamLogSink();
					m_streamLogSink.Log(logType, message);
				}
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		if (m_streamLogSink != null)
		{
			m_streamLogSink.Dispose();
		}
	}

	private void CreateStreamLogSink()
	{
		if (m_streamLogSink != null)
		{
			m_streamLogSink.Dispose();
		}
		m_streamLogSink = new StreamLogSink(Storage.OpenFile(m_path, OpenFileMode.CreateOrOpen));
	}
}
