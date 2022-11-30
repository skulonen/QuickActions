using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class SourceView : ViewBase
{
	readonly static Dictionary<esriDatasetType, string> _datasetTypeDescriptions;
	readonly static Dictionary<WorkspaceFactory, string> _workspaceFactoryDescriptions;

	static SourceView()
	{
		// I don't think ArcGIS Pro SDK includes a way to get the friendly names of these enum values.
		// However, there should be a ArcGIS.Core.XML file that defines the XML comments related to them.
		// Use it to build directories for the friendly names.

		_datasetTypeDescriptions = new Dictionary<esriDatasetType, string>();
		_workspaceFactoryDescriptions = new Dictionary<WorkspaceFactory, string>();

		var appPath = Assembly.GetEntryAssembly().Location;
		var appDirectory = Path.GetDirectoryName(appPath);
		var xmlPath = Path.Combine(appDirectory, "ArcGIS.Core.XML");

		if (File.Exists(xmlPath))
		{
			var doc = XDocument.Load(xmlPath);
			var regex = new Regex(@"^F:ArcGIS\.Core\.CIM\.(?<type>esriDatasetType|WorkspaceFactory)\.(?<value>\w+)$", RegexOptions.Compiled);

			foreach (var member in doc.Root.Element("members").Elements("member"))
			{
				var memberName = member.Attribute("name").Value;
				var match = regex.Match(memberName);
				if (!match.Success) continue;

				var type = match.Groups["type"].Value;
				var value = match.Groups["value"].Value;
				var summary = member.Element("summary").Value.Trim().TrimEnd('.');

				if (type == nameof(esriDatasetType))
				{
					var enumValue = (esriDatasetType)Enum.Parse(typeof(esriDatasetType), value);
					_datasetTypeDescriptions[enumValue] = summary;
				}
				else if (type == nameof(WorkspaceFactory))
				{
					var enumValue = (WorkspaceFactory)Enum.Parse(typeof(WorkspaceFactory), value);
					_workspaceFactoryDescriptions[enumValue] = summary;
				}
			}
		}
	}

	public SourceView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			var commonItems = new List<KeyValuePair<string, object>>();
			var connectionStringItems = new List<KeyValuePair<string, object>>();

			var connection = MapMember.GetDataConnection();
			var connectionType = connection.GetType();
			commonItems.Add(new("Connection type", connectionType.Name));

			// The values that will be displayed in the UI are stored in the following properties.
			// They are defined in many CIMDataConnection subtypes, but unfortunately there's no common base class or interface that defines them.
			// Instead of listing all types that define these properties, just use reflection to check if they exist.

			if (connectionType.GetProperty("WorkspaceFactory") is PropertyInfo workspaceFactoryProp)
			{
				var workspaceFactory = (WorkspaceFactory)workspaceFactoryProp.GetValue(connection);
				_workspaceFactoryDescriptions.TryGetValue(workspaceFactory, out var workspaceFactoryDescription);
				commonItems.Add(new("Workspace type", workspaceFactoryDescription ?? workspaceFactory.ToString()));
			}

			if (connectionType.GetProperty("DatasetType") is PropertyInfo datasetTypeProp)
			{
				var datasetType = (esriDatasetType)datasetTypeProp.GetValue(connection);
				_datasetTypeDescriptions.TryGetValue(datasetType, out var datasetTypeDescription);
				commonItems.Add(new("Dataset type", datasetTypeDescription ?? datasetType.ToString()));
			}

			if (connectionType.GetProperty("Dataset") is PropertyInfo datasetProp)
			{
				var dataset = (string)datasetProp.GetValue(connection);
				commonItems.Add(new("Dataset", dataset));
			}

			if (connectionType.GetProperty("URL") is PropertyInfo urlProp)
			{
				var url = (string)urlProp.GetValue(connection);
				commonItems.Add(new("URL", url));
			}

			if (connectionType.GetProperty("WorkspaceConnectionString") is PropertyInfo connectionStringProp)
			{
				var connectionString = (string)connectionStringProp.GetValue(connection);
				foreach (var connectionParameter in connectionString.Split(';'))
				{
					var separatorIndex = connectionParameter.IndexOf('=');
					var parameterName = connectionParameter[..separatorIndex];
					var parameterValue = connectionParameter[(separatorIndex + 1)..];

					connectionStringItems.Add(new(parameterName, parameterValue));
				}
			}

			CommonItems = commonItems;
			ConnectionStringItems = connectionStringItems;
		});
	}

	IReadOnlyCollection<KeyValuePair<string, object>> _commonItems;
	public IReadOnlyCollection<KeyValuePair<string, object>> CommonItems
	{
		get => _commonItems;
		set
		{
			_commonItems = value;
			NotifyPropertyChanged();
		}
	}

	IReadOnlyCollection<KeyValuePair<string, object>> _connectionStringItems;
	public IReadOnlyCollection<KeyValuePair<string, object>> ConnectionStringItems
	{
		get => _connectionStringItems;
		set
		{
			_connectionStringItems = value;
			NotifyPropertyChanged();
		}
	}
}
