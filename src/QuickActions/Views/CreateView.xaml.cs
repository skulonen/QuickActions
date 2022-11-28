using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class CreateView : ViewBase
{
	public CreateView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			Templates = MapMember.GetTemplates()
				.OrderBy(template => template.Name)
				.Select(template => new TemplateContainer(template))
				.ToList();
		});
	}

	IReadOnlyCollection<TemplateContainer> _templates;
	public IReadOnlyCollection<TemplateContainer> Templates
	{
		get => _templates;
		set
		{
			_templates = value;
			NotifyPropertyChanged();
			NotifyPropertyChanged(nameof(NoTemplates));
		}
	}

	public bool NoTemplates => Templates?.Count == 0;

	private void OnSelectTemplate(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: TemplateContainer template })
		{
			template.Template.ActivateDefaultToolAsync();
		}
	}

	private void OnCreateTemplate(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			var template = MapMember.CreateTemplate(MapMember.Name);
			Templates = new[] { new TemplateContainer(template) };
		});
	}
}

public class TemplateContainer : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	public TemplateContainer(EditingTemplate template)
	{
		Template = template;

		template.GeneratePreviewAsync(16, 16).ContinueWith(task =>
		{
			Preview = task.Result;
			PropertyChanged?.Invoke(this, new(nameof(Preview)));
		});
	}

	public EditingTemplate Template { get; }

	public ImageSource Preview { get; private set; }
}
