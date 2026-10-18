using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Engine;

namespace Game;

public class CommunityContentScreen : Screen
{
	private enum Order
	{
		ByRank,
		ByTime,
		ByDownloads,
		BySize,
		ByVersionNewest,
		ByVersionOldest
	}

	private ListPanelWidget m_listPanel;

	private LinkWidget m_moreLink;

	private LabelWidget m_orderLabel;

	private ButtonWidget m_changeOrderButton;

	private LabelWidget m_filterLabel;

	private ButtonWidget m_changeFilterButton;

	private TextBoxWidget m_searchTextBox;

	private LabelWidget m_searchLabel;

	private LinkWidget m_clearSearchLink;

	private ButtonWidget m_downloadButton;

	private ButtonWidget m_deleteButton;

	private ButtonWidget m_moreOptionsButton;

	private CancellableBusyDialog m_busyDialog = new CancellableBusyDialog("Retrieving Content", autoHideOnCancel: false);

	private object m_filter;

	private Order m_order;

	private string m_search;

	private double m_itemsCacheExpiryTime;

	private int m_populatingListCount;

	private Dictionary<string, IEnumerable<object>> m_itemsCache = new Dictionary<string, IEnumerable<object>>();

	public CommunityContentScreen()
	{
		XElement node = ContentManager.Get<XElement>("Screens/CommunityContentScreen");
		LoadContents(this, node);
		m_listPanel = Children.Find<ListPanelWidget>("List");
		m_orderLabel = Children.Find<LabelWidget>("Order");
		m_changeOrderButton = Children.Find<ButtonWidget>("ChangeOrder");
		m_filterLabel = Children.Find<LabelWidget>("Filter");
		m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
		m_searchTextBox = Children.Find<TextBoxWidget>("Search");
		m_searchLabel = Children.Find<LabelWidget>("SearchLabel");
		m_clearSearchLink = Children.Find<LinkWidget>("ClearSearchLink");
		m_downloadButton = Children.Find<ButtonWidget>("Download");
		m_deleteButton = Children.Find<ButtonWidget>("Delete");
		m_moreOptionsButton = Children.Find<ButtonWidget>("MoreOptions");
		m_listPanel.ItemWidgetFactory = delegate(object item)
		{
			if (item is CommunityContentEntry communityContentEntry)
			{
				XElement node2 = ContentManager.Get<XElement>("Widgets/CommunityContentItem");
				ContainerWidget obj = (ContainerWidget)Widget.LoadWidget(this, node2, null);
				obj.Children.Find<RectangleWidget>("CommunityContentItem.Icon").Subtexture = ExternalContentManager.GetEntryTypeIcon(communityContentEntry.Type);
				obj.Children.Find<LabelWidget>("CommunityContentItem.Text").Text = communityContentEntry.Name;
				obj.Children.Find<LabelWidget>("CommunityContentItem.Details").Text = $"{ExternalContentManager.GetEntryTypeDescription(communityContentEntry.Type)} {DataSizeFormatter.Format(communityContentEntry.Size)}";
				obj.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").Rating = communityContentEntry.RatingsAverage;
				obj.Children.Find<StarRatingWidget>("CommunityContentItem.Rating").IsVisible = communityContentEntry.RatingsAverage > 0f;
				obj.Children.Find<LabelWidget>("CommunityContentItem.ExtraText").Text = communityContentEntry.ExtraText;
				return obj;
			}
			XElement node3 = ContentManager.Get<XElement>("Widgets/CommunityContentItemMore");
			ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node3, null);
			m_moreLink = containerWidget.Children.Find<LinkWidget>("CommunityContentItemMore.Link");
			m_moreLink.Tag = item as string;
			return containerWidget;
		};
		m_searchTextBox.TextChanged += delegate
		{
			m_search = m_searchTextBox.Text.Replace("\n", "").Trim();
			PopulateList(null);
		};
		m_listPanel.SelectionChanged += delegate
		{
			if (m_listPanel.SelectedItem != null && !(m_listPanel.SelectedItem is CommunityContentEntry))
			{
				m_listPanel.SelectedItem = null;
			}
		};
	}

	public override void Enter(object[] parameters)
	{
		m_filter = string.Empty;
		m_order = Order.ByRank;
		m_search = string.Empty;
		PopulateList(null);
	}

	public override void Update()
	{
		CommunityContentEntry communityContentEntry = m_listPanel.SelectedItem as CommunityContentEntry;
		m_downloadButton.IsEnabled = communityContentEntry != null;
		m_deleteButton.IsEnabled = UserManager.ActiveUser != null && communityContentEntry != null && m_filter as string == UserManager.ActiveUser.UniqueId;
		m_orderLabel.Text = GetOrderDisplayName(m_order);
		m_filterLabel.Text = GetFilterDisplayName(m_filter);
		if (!m_searchTextBox.HasFocus)
		{
			m_searchTextBox.Text = m_search;
		}
		m_searchLabel.IsVisible = string.IsNullOrEmpty(m_searchTextBox.Text) && !m_searchTextBox.HasFocus;
		m_clearSearchLink.IsVisible = !string.IsNullOrEmpty(m_searchTextBox.Text) || !string.IsNullOrEmpty(m_search) || m_searchTextBox.HasFocus;
		if (m_populatingListCount > 0 && m_busyDialog.RootWidget != ScreensManager.RootWidget)
		{
			DialogsManager.ShowDialog(null, m_busyDialog);
		}
		if (m_populatingListCount <= 0 && m_busyDialog.RootWidget == ScreensManager.RootWidget)
		{
			DialogsManager.HideDialog(m_busyDialog);
		}
		if (m_changeOrderButton.IsClicked)
		{
			List<Order> items = EnumUtils.GetEnumValues(typeof(Order)).Cast<Order>().ToList();
			DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Sort Order", items, 60f, (object item) => GetOrderDisplayName((Order)item), delegate(object item)
			{
				m_order = (Order)item;
				PopulateList(null);
			}));
		}
		if (m_changeFilterButton.IsClicked)
		{
			List<object> list = new List<object>();
			list.Add(string.Empty);
			foreach (ExternalContentType item in from ExternalContentType t in EnumUtils.GetEnumValues(typeof(ExternalContentType))
				where ExternalContentManager.IsEntryTypeDownloadSupported(t)
				select t)
			{
				list.Add(item);
			}
			if (UserManager.ActiveUser != null)
			{
				list.Add(UserManager.ActiveUser.UniqueId);
			}
			if (m_listPanel.SelectedItem is CommunityContentEntry)
			{
				CommunityContentEntry communityContentEntry2 = (CommunityContentEntry)m_listPanel.SelectedItem;
				list.Add(communityContentEntry2.Url);
			}
			DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Items Filter", list, 60f, (object item) => GetFilterDisplayName(item), delegate(object item)
			{
				m_filter = item;
				PopulateList(null);
			}));
		}
		if (m_clearSearchLink.IsClicked)
		{
			m_search = string.Empty;
			PopulateList(null);
		}
		if (m_downloadButton.IsClicked && communityContentEntry != null)
		{
			DownloadEntry(communityContentEntry);
		}
		if (m_deleteButton.IsClicked && communityContentEntry != null)
		{
			DeleteEntry(communityContentEntry);
		}
		if (m_moreOptionsButton.IsClicked)
		{
			DialogsManager.ShowDialog(null, new MoreCommunityLinkDialog());
		}
		if (m_moreLink != null && m_moreLink.IsClicked)
		{
			PopulateList((string)m_moreLink.Tag);
		}
		if (base.Input.Back || Children.Find<BevelledButtonWidget>("TopBar.Back").IsClicked)
		{
			ScreensManager.SwitchScreen("Content");
		}
		if (base.Input.Hold.HasValue && base.Input.HoldTime > 2f && base.Input.Hold.Value.Y < 20f)
		{
			m_itemsCacheExpiryTime = 0.0;
			Task.Delay(250).Wait();
		}
	}

	private void PopulateList(string cursor)
	{
		string text = string.Empty;
		string text2 = string.Empty;
		if (m_filter is string)
		{
			string text3 = (string)m_filter;
			if (text3.StartsWith("http"))
			{
				text2 = text3;
			}
			else
			{
				text = text3;
			}
		}
		string text4 = ((m_filter is ExternalContentType) ? m_filter.ToString() : string.Empty);
		string text5 = string.Empty;
		if (SettingsManager.CommunityContentMode == CommunityContentMode.Strict)
		{
			text5 = "1";
		}
		if (SettingsManager.CommunityContentMode == CommunityContentMode.Normal)
		{
			text5 = "0";
		}
		string text6 = m_order.ToString();
		string cacheKey = text + "\n" + text2 + "\n" + text4 + "\n" + text5 + "\n" + m_search + "\n" + text6;
		m_moreLink = null;
		if (string.IsNullOrEmpty(cursor) && m_itemsCacheExpiryTime != 0.0 && Time.RealTime < m_itemsCacheExpiryTime && m_itemsCache.TryGetValue(cacheKey, out var value))
		{
			m_listPanel.ClearItems();
			m_listPanel.AddItems(value);
			m_listPanel.ScrollPosition = 0f;
			return;
		}
		object[] prefixItems = ((!string.IsNullOrEmpty(cursor)) ? m_listPanel.Items.Where((object i) => i is CommunityContentEntry).ToArray() : new object[0]);
		m_populatingListCount++;
		CommunityContentManager.List(cursor, text, text2, text4, text5, m_search, text6, m_busyDialog.Progress, delegate(List<CommunityContentEntry> list, string nextCursor)
		{
			m_populatingListCount--;
			m_listPanel.ClearItems();
			m_listPanel.AddItems(prefixItems);
			m_listPanel.AddItems(list);
			if (prefixItems.Length == 0)
			{
				m_listPanel.ScrollPosition = 0f;
			}
			if (list.Count > 0 && !string.IsNullOrEmpty(nextCursor))
			{
				m_listPanel.AddItem(nextCursor);
			}
			m_itemsCache[cacheKey] = new List<object>(m_listPanel.Items);
			m_itemsCacheExpiryTime = Time.RealTime + 120.0;
		}, delegate(Exception error)
		{
			m_populatingListCount--;
			DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
		});
	}

	private void DownloadEntry(CommunityContentEntry entry)
	{
		string userId = ((UserManager.ActiveUser != null) ? UserManager.ActiveUser.UniqueId : string.Empty);
		CancellableBusyDialog busyDialog = new CancellableBusyDialog($"Downloading {entry.Name}", autoHideOnCancel: false);
		DialogsManager.ShowDialog(null, busyDialog);
		CommunityContentManager.Download(entry.Url, entry.Name, entry.Type, userId, busyDialog.Progress, delegate
		{
			DialogsManager.HideDialog(busyDialog);
		}, delegate(Exception error)
		{
			DialogsManager.HideDialog(busyDialog);
			DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
		});
	}

	private void DeleteEntry(CommunityContentEntry entry)
	{
		if (UserManager.ActiveUser == null)
		{
			return;
		}
		DialogsManager.ShowDialog(null, new MessageDialog("Are you sure?", "The link will be deleted from the server.", "Yes", "No", delegate(MessageDialogButton button)
		{
			if (button == MessageDialogButton.Button1)
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog($"Deleting {entry.Name}", autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				CommunityContentManager.Delete(entry.Url, UserManager.ActiveUser.UniqueId, busyDialog.Progress, delegate
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog("Link Deleted", "It will stop appearing in the listings in a few minutes.", "OK", null, null));
				}, delegate(Exception error)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
				});
			}
		}));
	}

	private static string GetFilterDisplayName(object filter)
	{
		if (filter is string)
		{
			string text = (string)filter;
			if (text.StartsWith("http"))
			{
				return "Selected User's Items";
			}
			if (string.IsNullOrEmpty(text))
			{
				return "All Items";
			}
			return "Your Items";
		}
		if (filter is ExternalContentType)
		{
			return ExternalContentManager.GetEntryTypeDescription((ExternalContentType)filter);
		}
		throw new InvalidOperationException("Invalid filter.");
	}

	private static string GetOrderDisplayName(Order order)
	{
		return order switch
		{
			Order.ByRank => "Top Ranked", 
			Order.ByTime => "Most Recent", 
			Order.ByDownloads => "Most Downloaded", 
			Order.BySize => "Largest", 
			Order.ByVersionNewest => "Newest Version", 
			Order.ByVersionOldest => "Oldest Version", 
			_ => throw new InvalidOperationException("Invalid order."), 
		};
	}
}
