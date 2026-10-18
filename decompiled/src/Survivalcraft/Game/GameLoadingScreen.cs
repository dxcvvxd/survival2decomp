using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Game;

public class GameLoadingScreen : Screen
{
	private WorldInfo m_worldInfo;

	private string m_worldSnapshotName;

	private LabelWidget m_loadingLabel;

	private StateMachine m_stateMachine = new StateMachine();

	private bool m_upgradeCompleted;

	private Exception m_upgradeError;

	public GameLoadingScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/GameLoadingScreen");
		LoadContents(this, node);
		m_loadingLabel = Children.Find<LabelWidget>("LoadingLabel");
		m_stateMachine.AddState("WaitingForFadeIn", null, delegate
		{
			if (!ScreensManager.IsAnimating)
			{
				if (string.IsNullOrEmpty(m_worldSnapshotName))
				{
					m_stateMachine.TransitionTo("Upgrading");
				}
				else
				{
					m_stateMachine.TransitionTo("RestoringSnapshot");
				}
			}
		}, null);
		m_stateMachine.AddState("Upgrading", delegate
		{
			GameManager.DisposeProject();
			m_upgradeCompleted = false;
			m_upgradeError = null;
			Task.Run(delegate
			{
				try
				{
					GameManager.RepairAndUpgradeWorld(m_worldInfo);
					m_upgradeCompleted = true;
				}
				catch (Exception upgradeError)
				{
					m_upgradeError = upgradeError;
				}
			});
		}, delegate
		{
			if (m_upgradeCompleted)
			{
				m_stateMachine.TransitionTo("Loading");
			}
			else if (m_upgradeError != null)
			{
				throw m_upgradeError;
			}
		}, null);
		m_stateMachine.AddState("Loading", delegate
		{
			ProgressManager.UpdateProgress("Loading World", 0f);
		}, delegate
		{
			ContainerWidget gamesWidget = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
			GameManager.LoadProject(m_worldInfo, gamesWidget);
			ScreensManager.SwitchScreen("Game");
		}, null);
		m_stateMachine.AddState("RestoringSnapshot", delegate
		{
			ProgressManager.UpdateProgress("Restoring World", 0f);
		}, delegate
		{
			GameManager.DisposeProject();
			WorldsManager.RestoreWorldFromSnapshot(m_worldInfo.DirectoryName, m_worldSnapshotName);
			m_stateMachine.TransitionTo("Upgrading");
		}, null);
	}

	public override void Update()
	{
		try
		{
			m_stateMachine.Update();
			m_loadingLabel.Text = ProgressManager.OperationName;
		}
		catch (Exception e)
		{
			ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			DialogsManager.ShowDialog(null, new MessageDialog("Error loading world", ExceptionManager.MakeFullErrorMessage(e), "OK", null, null));
		}
	}

	public override void Enter(object[] parameters)
	{
		m_worldInfo = (WorldInfo)parameters[0];
		m_worldSnapshotName = (string)parameters[1];
		m_stateMachine.TransitionTo("WaitingForFadeIn");
		ProgressManager.UpdateProgress("Starting World", 0f);
	}
}
