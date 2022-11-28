using System.Threading.Tasks;
using System.Windows;

using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Controls;

namespace QuickActions.Views;

public partial class FilterView : ViewBase
{
	public FilterView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		QueryBuilderControl.ConfigureControl = new()
		{
			MapMember = MapMember,
			Expression = ((ITableDefinitionQueries)MapMember).DefinitionQuery
		};
		return Task.CompletedTask;
	}

	private void OnApply(object sender, RoutedEventArgs e)
	{
		var expression = QueryBuilderControl.Expression;
		QueuedTask.Run(() =>
		{
			((ITableDefinitionQueries)MapMember).SetDefinitionQuery(expression);
		});
	}
}
