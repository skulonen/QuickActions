using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public class ViewBase : UserControl, INotifyPropertyChanged
{
	private bool _didLoad;

	public ViewBase()
	{
		Loaded += OnLoaded;
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void NotifyPropertyChanged([CallerMemberName] string name = null)
	{
		PropertyChanged?.Invoke(this, new(name));
	}

	void OnLoaded(object sender, RoutedEventArgs args)
	{
		Loaded -= OnLoaded;
		_didLoad = true;

		if (MapMember != null)
		{
			InitializeAsync();
		}
	}

	private MapMember _mapMember;
	public MapMember MapMember
	{
		get => _mapMember;
		set
		{
			_mapMember = value;
			NotifyPropertyChanged();

			if (_didLoad)
			{
				InitializeAsync();
			}
		}
	}

	protected virtual Task InitializeAsync()
	{
		return Task.CompletedTask;
	}
}
