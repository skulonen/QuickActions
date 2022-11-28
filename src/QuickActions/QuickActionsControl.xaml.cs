using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using ArcGIS.Desktop.Mapping;

namespace QuickActions;

public partial class QuickActionsControl : UserControl, INotifyPropertyChanged
{
	private MapMember _mapMember;
	public MapMember MapMember
	{
		get => _mapMember;
		set
		{
			_mapMember = value;
			PropertyChanged?.Invoke(this, new(nameof(MapMember)));

			var isConnected = MapMember.ConnectionStatus == ConnectionStatus.Connected;

			var views = new ObservableCollection<ViewListItem>();
			var optionsItem = new ViewListItem
			{
				Label = "Options",
				ImageSource = (ImageSource)Application.Current.Resources["GenericOptions32"],
				Content = new Views.OptionsView { MapMember = value }
			};
			SelectedView = optionsItem;
			views.Add(optionsItem);
			views.Add(new()
			{
				Label = "Source",
				ImageSource = (ImageSource)Application.Current.Resources["Geodatabase32"],
				Content = new Views.SourceView { MapMember = value }
			});
			if (MapMember is IDisplayTable && isConnected)
			{
				views.Add(new()
				{
					Label = "Find",
					ImageSource = (ImageSource)Application.Current.Resources["ZoomGeneric32"],
					Content = new Views.FindView { MapMember = value }
				});
			}
			if (MapMember is ITableDefinitionQueries && isConnected)
			{
				views.Add(new()
				{
					Label = "Filter",
					ImageSource = (ImageSource)Application.Current.Resources["Filter32"],
					Content = new Views.FilterView { MapMember = value }
				});
			}
			if (MapMember is FeatureLayer && isConnected)
			{
				views.Add(new()
				{
					Label = "Symbology",
					ImageSource = (ImageSource)Application.Current.Resources["GenericLayerSymbology32"],
					Content = new Views.SymbologyView { MapMember = value }
				});
			}
			if (MapMember is BasicFeatureLayer && isConnected)
			{
				views.Add(new()
				{
					Label = "Create",
					ImageSource = (ImageSource)Application.Current.Resources["EditingCreateFeaturesWindowShow32"],
					Content = new Views.CreateView { MapMember = value }
				});
			}
			if (MapMember is IDisplayTable && isConnected)
			{
				views.Add(new()
				{
					Label = "Fields",
					ImageSource = (ImageSource)Application.Current.Resources["TableFieldsView32"],
					Content = new Views.FieldsView { MapMember = value }
				});
				views.Add(new()
				{
					Label = "Service",
					ImageSource = (ImageSource)Application.Current.Resources["ServerArcGIS32"],
					Content = new Views.ServiceView { MapMember = value }
				});
			}
			views.Add(new()
			{
				Label = "Definition",
				ImageSource = (ImageSource)Application.Current.Resources["CogWheel32"],
				Content = new Views.DefinitionView { MapMember = value }
			});
			Views = views;
		}
	}

	private ContextMenu _originalMenu;
	public ContextMenu OriginalMenu
	{
		get => _originalMenu;
		set
		{
			_originalMenu = value;
			PropertyChanged?.Invoke(this, new(nameof(OriginalMenu)));
		}
	}


	private ObservableCollection<ViewListItem> _views;
	public ObservableCollection<ViewListItem> Views
	{
		get => _views;
		set
		{
			_views = value;
			PropertyChanged?.Invoke(this, new(nameof(Views)));
		}
	}

	private ViewListItem _selectedView;
	public ViewListItem SelectedView
	{
		get => _selectedView;
		set
		{
			_selectedView = value;
			PropertyChanged?.Invoke(this, new(nameof(SelectedView)));
		}
	}

	public QuickActionsControl()
	{
		InitializeComponent();
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

public class ViewListItem : INotifyPropertyChanged
{
	private ImageSource _imageSource;
	public ImageSource ImageSource
	{
		get => _imageSource;
		set
		{
			_imageSource = value;
			PropertyChanged?.Invoke(this, new(nameof(ImageSource)));
		}
	}

	private string _label;
	public string Label
	{
		get => _label;
		set
		{
			_label = value;
			PropertyChanged?.Invoke(this, new(nameof(Label)));
		}
	}

	private FrameworkElement _content;
	public FrameworkElement Content
	{
		get => _content;
		set
		{
			_content = value;
			PropertyChanged?.Invoke(this, new(nameof(Content)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}
