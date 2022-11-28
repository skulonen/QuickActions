using System;
using System.Threading.Tasks;
using System.Windows;

using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

using ActiproSoftware.Text.Implementation;

namespace QuickActions.Views;

public partial class DefinitionView : ViewBase
{
	public DefinitionView()
	{
		InitializeComponent();
	}

	protected override async Task InitializeAsync()
	{
		var definitionJson = await QueuedTask.Run(() =>
		{
			var jsonSettings = new JsonSerializationSettings
			{
				PrettyPrint = true
			};
			if (MapMember is Layer layer)
			{
				return layer.GetDefinition().ToJson(jsonSettings);
			}
			else if (MapMember is StandaloneTable table)
			{
				return table.GetDefinition().ToJson(jsonSettings);
			}
			return "";
		});

		// TODO explain
		var language = new ArcGIS.Desktop.Internal.Mapping.Controls.TextExpressionBuilder.JavaScriptParser().SyntaxLanguage;

		//var languageStream = typeof(Map).Assembly.GetManifestResourceStream("ArcGIS.Desktop.Mapping.Controls.TextExpressionBuilder.LangDef.JavaScript.langdef");
		//var language = new SyntaxLanguageDefinitionSerializer().LoadFromStream(languageStream);

		var doc = new EditorDocument
		{
			Language = language,
			TabSize = 2
		};
		doc.SetText(definitionJson);
		SyntaxEditor.Document = doc;
	}

	bool _hasError;
	public bool HasError
	{
		get => _hasError;
		set
		{
			_hasError = value;
			NotifyPropertyChanged();
		}
	}

	string _errorMessage;
	public string ErrorMessage
	{
		get => _errorMessage;
		set
		{
			_errorMessage = value;
			NotifyPropertyChanged();
		}
	}

	private void OnApply(object sender, RoutedEventArgs e)
	{
		HasError = false;
		var definitionJson = SyntaxEditor.Text;

		QueuedTask.Run(() =>
		{
			try
			{
				var definition = CIMObject.FromJson(definitionJson);
				if (MapMember is Layer layer)
				{
					layer.SetDefinition((CIMBaseLayer)definition);
				}
				else if (MapMember is StandaloneTable table)
				{
					table.SetDefinition((CIMStandaloneTable)definition);
				}
			}
			catch (Exception e)
			{
				ErrorMessage = e.Message;
				HasError = true;
			}
		});
	}
}
