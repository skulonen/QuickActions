using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Internal.Core;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class SourceView : ViewBase
{
	public SourceView()
	{
		InitializeComponent();
	}

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			var connection = MapMember.GetDataConnection();

			var xml = ArcGIS.Core.Internal.CIM.XmlUtil.ToXml(connection);
			var doc = XDocument.Parse(xml);
			TreeItems = BuildNodes(doc.Root);
		});
	}

	IEnumerable<SourceTreeItem> _treeItems;
	public IEnumerable<SourceTreeItem> TreeItems
	{
		get => _treeItems;
		set
		{
			_treeItems = value;
			NotifyPropertyChanged();
		}
	}

	IEnumerable<SourceTreeItem> BuildNodes(XElement element)
	{
		foreach (var node in element.Nodes())
		{
			if (node.NodeType == XmlNodeType.Text)
			{
				yield return new() { Value = ((XText)node).Value };
			}
			else if (node.NodeType == XmlNodeType.Element)
			{
				var childElement = (XElement)node;
				var childNodes = childElement.Nodes().ToList();
				if (childNodes.Count == 1 && childNodes[0].NodeType == XmlNodeType.Text)
				{
					var subItems = new List<SourceTreeItem>();
					if (childElement.Name.LocalName == "WorkspaceConnectionString")
					{
						var connectionString = childElement.Value;
						if (childElement.Value != null)
						{
							foreach (var connectionParameter in connectionString.Split(';'))
							{
								var separatorIndex = connectionParameter.IndexOf('=');
								var parameterName = connectionParameter[..separatorIndex];
								var parameterValue = connectionParameter[(separatorIndex + 1)..];
								subItems.Add(new()
								{
									Name = parameterName,
									Value = parameterValue
								});
							}
						}
					}

					yield return new()
					{
						Name = childElement.Name.LocalName,
						Value = childElement.Value,
						Items = subItems
					};
				}
				else
				{
					yield return new()
					{
						Name = childElement.Name.LocalName,
						Items = BuildNodes(childElement)
					};
				}
			}
		}
	}
}

public class SourceTreeItem
{
	public string Name { get; init; }
	public object Value { get; init; }
	public IEnumerable<SourceTreeItem> Items { get; init; }
	public bool HasValue => Value != null;
}
