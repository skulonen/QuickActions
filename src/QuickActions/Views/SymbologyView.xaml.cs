using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace QuickActions.Views;

public partial class SymbologyView : ViewBase
{
	readonly static HashSet<FieldType> _uniqueValuesFieldTypes = new()
	{
		FieldType.Double,
		FieldType.GUID,
		FieldType.Integer,
		FieldType.Single,
		FieldType.SmallInteger,
		FieldType.String
	};

	readonly static HashSet<FieldType> _classBreaksFieldTypes = new()
	{
		FieldType.Double,
		FieldType.Integer,
		FieldType.Single,
		FieldType.SmallInteger
	};

	public SymbologyView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			_featureLayer = (FeatureLayer)MapMember;

			var allFields = ((IDisplayTable)MapMember).GetTable()
				.GetDefinition()
				.GetFields();

			_uniqueValuesFieldOptions = allFields
				.Where(field => _uniqueValuesFieldTypes.Contains(field.FieldType))
				.Select(field => field.Name)
				.ToList();

			_classBreaksFieldOptions = allFields
				.Where(field => _classBreaksFieldTypes.Contains(field.FieldType))
				.Select(field => field.Name)
				.ToList();

			var renderer = _featureLayer.GetRenderer();
			if (renderer is CIMSimpleRenderer simpleRenderer)
			{
				_simpleRenderer = simpleRenderer;
				IsSimple = true;
			}
			if (renderer is CIMUniqueValueRenderer uniqueValueRenderer)
			{
				_uniqueValueRenderer = uniqueValueRenderer;
				IsUniqueValues = true;
			}
			if (renderer is CIMClassBreaksRenderer classBreaksRenderer)
			{
				_classBreaksRenderer = classBreaksRenderer;
				IsClassBreaks = true;
			}
		});
	}

	FeatureLayer _featureLayer;
	CIMSimpleRenderer _simpleRenderer;
	CIMUniqueValueRenderer _uniqueValueRenderer;
	CIMClassBreaksRenderer _classBreaksRenderer;
	IReadOnlyCollection<string> _uniqueValuesFieldOptions;
	IReadOnlyCollection<string> _classBreaksFieldOptions;

	private bool _isSimple;
	public bool IsSimple
	{
		get => _isSimple;
		set
		{
			_isSimple = value;
			NotifyPropertyChanged();

			if (value)
			{
				IsUniqueValues = false;
				IsClassBreaks = false;

				ClassificationField = null;
				ClassificationFieldOptions = Array.Empty<string>();
			}
		}
	}

	private bool _isUniqueValues;
	public bool IsUniqueValues
	{
		get => _isUniqueValues;
		set
		{
			_isUniqueValues = value;
			NotifyPropertyChanged();

			if (value)
			{
				IsSimple = false;
				IsClassBreaks = false;

				ClassificationField = _uniqueValueRenderer?.Fields?.FirstOrDefault() ?? _uniqueValuesFieldOptions.FirstOrDefault();
				ClassificationFieldOptions = _uniqueValuesFieldOptions;
			}
		}
	}

	private bool _isClassBreaks;
	public bool IsClassBreaks
	{
		get => _isClassBreaks;
		set
		{
			_isClassBreaks = value;
			NotifyPropertyChanged();

			if (value)
			{
				IsSimple = false;
				IsUniqueValues = false;

				ClassificationField = _classBreaksRenderer?.Field ?? _classBreaksFieldOptions.FirstOrDefault();
				ClassificationFieldOptions = _classBreaksFieldOptions;
			}
		}
	}

	private string _classificationField;
	public string ClassificationField
	{
		get => _classificationField;
		set
		{
			_classificationField = value;
			NotifyPropertyChanged();
		}
	}

	private IReadOnlyCollection<string> _classificationFieldOptions;
	public IReadOnlyCollection<string> ClassificationFieldOptions
	{
		get => _classificationFieldOptions;
		set
		{
			_classificationFieldOptions = value;
			NotifyPropertyChanged();
		}
	}

	private void OnApply(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			CIMRenderer newRenderer = null;
			if (_isSimple)
			{
				newRenderer = _simpleRenderer = (CIMSimpleRenderer)_featureLayer.CreateRenderer(new SimpleRendererDefinition());
			}
			else if (_isUniqueValues)
			{
				if (ClassificationField == null) return;
				newRenderer = _uniqueValueRenderer = (CIMUniqueValueRenderer)_featureLayer.CreateRenderer(new UniqueValueRendererDefinition
				{
					ValueFields = new List<string> { ClassificationField }
				});
			}
			else if (_isClassBreaks)
			{
				if (ClassificationField == null) return;
				newRenderer = _classBreaksRenderer = (CIMClassBreaksRenderer)_featureLayer.CreateRenderer(new GraduatedColorsRendererDefinition
				{
					ClassificationField = ClassificationField
				});
			}

			if (newRenderer != null)
			{
				_featureLayer.SetRenderer(newRenderer);
			}
		});
	}

	private void OnCopy(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			var json = _featureLayer.GetRenderer().ToJson();
			Clipboard.SetText(json);
		});
	}

	private void OnPaste(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			var json = Clipboard.GetText();
			try
			{
				var renderer = (CIMRenderer)CIMObject.FromJson(json);
				_featureLayer.SetRenderer(renderer);
			}
			catch
			{
				MessageBox.Show(
					"Clipboard does not contain a valid renderer definition.",
					null,
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);
				return;
			}
			_ = InitializeAsync();
		});
	}
}
