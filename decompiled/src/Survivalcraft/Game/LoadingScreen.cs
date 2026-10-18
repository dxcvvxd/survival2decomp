using System.Xml.Linq;
using Engine.Content;

namespace Game;

public class LoadingScreen : Screen
{
	private bool m_loadingStarted;

	public LoadingScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/LoadingScreen");
		LoadContents(this, node);
		LoadingManager.QueueItem("Initialize VrManager", delegate
		{
			VrManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize CommunityContentManager", delegate
		{
			CommunityContentManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize MotdManager", delegate
		{
			MotdManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize LightingManager", delegate
		{
			LightingManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize StringsManager", delegate
		{
			StringsManager.LoadStrings();
		});
		LoadingManager.QueueItem("Initialize TextureAtlasManager", delegate
		{
			TextureAtlasManager.LoadAtlases();
		});
		foreach (ContentInfo item in ContentManager.List())
		{
			ContentInfo localContentInfo = item;
			LoadingManager.QueueItem("Load " + localContentInfo.Name, delegate
			{
				ContentManager.Get(localContentInfo.Name);
			});
		}
		LoadingManager.QueueItem("Initialize DatabaseManager", delegate
		{
			DatabaseManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize WorldsManager", delegate
		{
			WorldsManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize BlocksTexturesManager", delegate
		{
			BlocksTexturesManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize CharacterSkinsManager", delegate
		{
			CharacterSkinsManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize FurniturePacksManager", delegate
		{
			FurniturePacksManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize BlocksManager", delegate
		{
			BlocksManager.Initialize();
		});
		LoadingManager.QueueItem("Initialize CraftingRecipesManager", delegate
		{
			CraftingRecipesManager.Initialize();
		});
	}

	public override void Leave()
	{
		ContentManager.Dispose("Textures/Gui/CandyRufusLogo");
		ContentManager.Dispose("Textures/Gui/EngineLogo");
	}

	public override void Update()
	{
		if (!m_loadingStarted)
		{
			m_loadingStarted = true;
		}
		else if (!LoadingManager.LoadSomeItems())
		{
			AudioManager.PlaySound("Audio/UI/ButtonClick", 0.5f, 0f, 0f);
			MusicManager.CurrentMix = MusicManager.Mix.Menu;
			ScreensManager.SwitchScreen("MainMenu");
		}
	}
}
