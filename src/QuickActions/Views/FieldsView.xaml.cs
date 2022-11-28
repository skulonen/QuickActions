using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class FieldsView : ViewBase
{
	public enum View
	{
		Fields,
		Domain
	}

	public FieldsView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			Fields = ((IDisplayTable)MapMember).GetTable()
				.GetDefinition()
				.GetFields()
				.Select(field => new FieldInfo(field))
				.ToList();
		});
	}

	IReadOnlyCollection<FieldInfo> _fields;
	public IReadOnlyCollection<FieldInfo> Fields
	{
		get => _fields;
		set
		{
			_fields = value;
			NotifyPropertyChanged();
		}
	}

	FieldInfo _selectedField;
	public FieldInfo SelectedField
	{
		get => _selectedField;
		set
		{
			_selectedField = value;
			NotifyPropertyChanged();
		}
	}

	View _activeView = View.Fields;
	public View ActiveView
	{
		get => _activeView;
		set
		{
			_activeView = value;
			NotifyPropertyChanged();
		}
	}

	private void OnSelectDomain(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkContentElement { DataContext: FieldInfo fieldInfo })
		{
			SelectedField = fieldInfo;
			ActiveView = View.Domain;
		}
	}

	private void OnReturnToFields(object sender, RoutedEventArgs e)
	{
		ActiveView = View.Fields;
	}
}

public class FieldInfo
{
	public FieldInfo(Field field)
	{
		Name = field.Name;
		AliasName = field.AliasName;
		FieldType = field.FieldType.ToString();
		IsNullable = field.IsNullable;

		var domain = field.GetDomain();
		DomainName = domain?.GetName();

		if (domain is CodedValueDomain codedValueDomain)
		{
			IsCodedValueDomain = true;
			DomainValues = codedValueDomain.GetCodedValuePairs();
		}
	}

	public string Name { get; }
	public string AliasName { get; }
	public string FieldType { get; }
	public bool IsNullable { get; }
	public string DomainName { get; }
	public bool IsCodedValueDomain { get; }
	public SortedList<object, string> DomainValues { get; }
}
