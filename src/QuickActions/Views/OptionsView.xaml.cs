using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class OptionsView : ViewBase
{
	public OptionsView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		var options = new List<OptionItem>();

		if (MapMember is Layer layer)
		{
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["FolderOpenState16"],
				Label = "Expanded",
				GetValue = () => layer.IsExpanded,
				SetValue = layer.SetExpanded
			});
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["GenericVisible16"],
				Label = "Visibility",
				GetValue = () => layer.IsVisible,
				SetValue = layer.SetVisibility
			});
		}

		if (MapMember is IDisplayTable displayTable)
		{
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["SelectionSelectUnselect16"],
				Label = "Selection",
				GetValue = () => displayTable.IsSelectable,
				SetValue = displayTable.SetSelectable
			});
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["EditingSketchTool16"],
				Label = "Editing",
				GetValue = () => displayTable.IsEditable,
				SetValue = displayTable.SetEditable
			});
		}

		if (MapMember is FeatureLayer featureLayer)
		{
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["ContentsWindowListBySnapping16"],
				Label = "Snapping",
				GetValue = () => featureLayer.IsSnappable,
				SetValue = featureLayer.SetSnappable
			});
			options.Add(new()
			{
				Icon = (ImageSource)Application.Current.Resources["MapRibbonLabeling16"],
				Label = "Labeling",
				GetValue = () => featureLayer.IsLabelVisible,
				SetValue = featureLayer.SetLabelVisibility
			});
		}

		Options = options;

		return Task.CompletedTask;
	}

	IReadOnlyCollection<OptionItem> _options;
	public IReadOnlyCollection<OptionItem> Options
	{
		get => _options;
		set
		{
			_options = value;
			NotifyPropertyChanged();
		}
	}

	private void OnRemove(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			if (MapMember is Layer layer)
			{
				MapMember.Map.RemoveLayer(layer);
			}
			else if (MapMember is StandaloneTable table)
			{
				MapMember.Map.RemoveStandaloneTable(table);
			}
		});
	}
}

public class OptionItem : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	public ImageSource Icon { get; init; }
	public string Label { get; init; }

	public Func<bool> GetValue { get; init; }
	public Action<bool> SetValue { get; init; }

	public bool Value
	{
		get => GetValue();
		set
		{
			QueuedTask.Run(() =>
			{
				SetValue(value);
				PropertyChanged?.Invoke(this, new(nameof(Value)));
			});
		}
	}
}
