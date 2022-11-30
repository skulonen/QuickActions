using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class FindView : ViewBase
{
	public enum View
	{
		Find,
		Attributes
	}

	public FindView()
	{
		InitializeComponent();

		Loaded += (_, _) =>
		{
			SearchTextBox.SelectAll();
			SearchTextBox.Focus();
		};
	}

	View _activeView = View.Find;
	public View ActiveView
	{
		get => _activeView;
		set
		{
			_activeView = value;
			NotifyPropertyChanged();
		}
	}

	string _searchTerm;
	public string SearchTerm
	{
		get => _searchTerm;
		set
		{
			_searchTerm = value;
			NotifyPropertyChanged();
		}
	}

	IEnumerable<SearchFieldOption> _searchFieldOptions;
	public IEnumerable<SearchFieldOption> SearchFieldOptions
	{
		get => _searchFieldOptions;
		set
		{
			_searchFieldOptions = value;
			NotifyPropertyChanged();
		}
	} 

	object _searchField = _allFields;
	public object SearchField
	{
		get => _searchField;
		set
		{
			_searchField = value;
			NotifyPropertyChanged();
		}
	}

	IReadOnlyCollection<SearchResult> _searchResults;
	public IReadOnlyCollection<SearchResult> SearchResults
	{
		get => _searchResults;
		set
		{
			_searchResults = value;
			NotifyPropertyChanged();
			NotifyPropertyChanged(nameof(HasResults));
		}
	}

	Inspector _inspector;
	public Inspector Inspector
	{
		get => _inspector;
		set
		{
			_inspector = value;
			NotifyPropertyChanged();
		}
	}

	UserControl _inspectorView;
	public UserControl InspectorView
	{
		get => _inspectorView;
		set
		{
			_inspectorView = value;
			NotifyPropertyChanged();
		}
	}

	public bool HasResults => SearchResults != null;

	static readonly object _allFields = new();

	protected override async Task InitializeAsync()
	{
		await QueuedTask.Run(() =>
		{
			var searchFieldOptions = new List<SearchFieldOption>
			{
				new() { Field = _allFields, Label = "All fields" }
			};
			foreach (var field in GetSearchableFields())
			{
				searchFieldOptions.Add(new() { Field = field, Label = field.Name });
			}
			SearchFieldOptions = searchFieldOptions;
		});
	}

	private IEnumerable<Field> GetSearchableFields()
	{
		var typeWhitelist = new HashSet<FieldType>
		{
			FieldType.Double,
			FieldType.GlobalID,
			FieldType.GUID,
			FieldType.Integer,
			FieldType.OID,
			FieldType.Single,
			FieldType.SmallInteger,
			FieldType.String
		};

		var nameBlacklist = new HashSet<string>
		{
			//"Shape__Length",
			//"Shape__Area"
		};

		return ((IDisplayTable)MapMember).GetTable()
			.GetDefinition()
			.GetFields()
			.Where(field => typeWhitelist.Contains(field.FieldType) && !nameBlacklist.Contains(field.Name));
	}

	private void OnKeyUp(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter) return;
		if (string.IsNullOrEmpty(SearchTerm)) return;

		QueuedTask.Run(() =>
		{
			if (SearchField == _allFields)
			{
				SearchResults = DoSearch(SearchTerm, GetSearchableFields().ToList());
			}
			else
			{
				SearchResults = DoSearch(SearchTerm, new[] { (Field)SearchField });
			}
		});
	}

	private IReadOnlyCollection<SearchResult> DoSearch(string searchTerm, IEnumerable<Field> searchFields)
	{
		var escapedSearchTerm = searchTerm.Replace("'", "''");
		var whereClauseParts = new List<string>();

		foreach (var field in searchFields)
		{
			string fieldSql;
			if (field.FieldType == FieldType.String)
			{
				fieldSql = field.Name;
			}
			else
			{
				// 100 characters should be enough to hold any number or GUID
				fieldSql = $"CAST({field.Name} AS VARCHAR(100))";
			}

			var formattedSearchTerm = escapedSearchTerm;
			if (field.FieldType == FieldType.Double || field.FieldType == FieldType.Single)
			{
				formattedSearchTerm = formattedSearchTerm.Replace(',', '.');
			}

			whereClauseParts.Add($"({fieldSql} LIKE '%{formattedSearchTerm}%')");
		}

		var whereClause = string.Join(" OR ", whereClauseParts);

		var cursor = ((IDisplayTable)MapMember).GetTable().Search(new()
		{
			WhereClause = whereClause
		}, false);

		var exactResults = new List<SearchResult>();
		var partialResults = new List<SearchResult>();
		while (cursor.MoveNext())
		{
			var row = cursor.Current;

			var exactMatches = new List<(Field Field, object Value)>();
			var partialMatches = new List<(Field Field, object Value)>();

			foreach (var field in searchFields)
			{
				var value = row[field.Name];
				if (value == null) continue;

				if (field.FieldType == FieldType.String)
				{
					if (value is string stringValue)
					{
						if (stringValue.Equals(searchTerm, StringComparison.InvariantCultureIgnoreCase))
						{
							exactMatches.Add((field, value));
						}
						else if (stringValue.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
						{
							partialMatches.Add((field, value));
						}
					}
				}
				else if (field.FieldType == FieldType.GlobalID || field.FieldType == FieldType.GUID)
				{
					if (
						value is Guid guidValue
						|| (
							value is string stringValue
							&& Guid.TryParse(stringValue, out guidValue)
						)
					)
					{
						if (
							Guid.TryParse(searchTerm, out var inputGuid)
							&& guidValue == inputGuid
						)
						{
							exactMatches.Add((field, value));
						}
						else
						{
							var guidValueString = guidValue.ToString("B");
							if (guidValueString.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
							{
								partialMatches.Add((field, value));
							}
						}
					}
				}
				else
				{
					// All other types are numeric
					var numberValue = Convert.ToDouble(value);
					if (
						(
							double.TryParse(searchTerm, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var inputnumber)
							|| double.TryParse(searchTerm, NumberStyles.Any, CultureInfo.CurrentCulture.NumberFormat, out inputnumber)
						)
						&& numberValue == inputnumber
					)
					{
						exactMatches.Add((field, value));
					}
					else
					{
						var numberValueString = numberValue.ToString(CultureInfo.InvariantCulture.NumberFormat);
						if (numberValueString.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
						{
							partialMatches.Add((field, value));
						}
						else
						{
							numberValueString = numberValue.ToString(CultureInfo.CurrentCulture.NumberFormat);
							if (numberValueString.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase))
							{
								partialMatches.Add((field, value));
							}
						}
					}
				}
			}

			var hasExtactMatch = exactMatches.Count > 0;
			var hasPartialMatch = partialMatches.Count > 0;
			if (!hasExtactMatch && !hasPartialMatch)
			{
				// This shouldn't happen...
				// It must mean that there's a difference between the ways SQL and C# determine matches.
				continue;
			}

			var (matchField, matchValue) = hasExtactMatch ? exactMatches[0] : partialMatches[0];
			var otherMatchFields = exactMatches.Select(match => match.Field.Name)
				.Concat(partialMatches.Select(match => match.Field.Name))
				.Where(fieldName => fieldName != matchField.Name)
				.Distinct()
				.ToList();

			var result = new SearchResult
			{
				Row = row,
				MatchField = matchField.Name,
				MatchValue = matchValue?.ToString(),
				OtherMatchFields = otherMatchFields
			};

			if (hasExtactMatch)
			{
				exactResults.Add(result);
			}
			else
			{
				partialResults.Add(result);
			}
		}

		var allResults = exactResults.Concat(partialResults).ToList();
		return allResults;
	}

	private void OnSelectResult(object sender, RoutedEventArgs e)
	{
		var result = (SearchResult)((FrameworkElement)sender).DataContext;
		QueuedTask.Run(() =>
		{
			MapMember.Map.SetSelection(SearchResultsToSelectionSet(new[] { result }));
		});
	}

	private void OnZoomToResult(object sender, RoutedEventArgs e)
	{
		if (MapMember is not BasicFeatureLayer layer) return;

		var result = (SearchResult)((FrameworkElement)sender).DataContext;
		QueuedTask.Run(() =>
		{
			var objectId = result.Row.GetObjectID();
			MapView.Active.ZoomTo(layer, objectId);
			MapView.Active.FlashFeature(layer, objectId);
		});
	}

	private void OnShowResultAttributes(object sender, RoutedEventArgs e)
	{
		ActiveView = View.Attributes;
		var result = (SearchResult)((FrameworkElement)sender).DataContext;
		QueuedTask.Run(() =>
		{
			var inspector = new Inspector { AllowEditing = ((IDisplayTable)MapMember).CanEditData() };
			inspector.Load(result.Row);
			new TaskFactory(QueuedTask.UIScheduler).StartNew(() =>
			{
				var (embeddableControl, inspectorView) = inspector.CreateEmbeddableControl();
				embeddableControl.OpenAsync();
				Inspector = inspector;
				InspectorView = inspectorView;
			});
		});
	}

	private void OnSelectAllResults(object sender, RoutedEventArgs e)
	{
		QueuedTask.Run(() =>
		{
			MapMember.Map.SetSelection(SearchResultsToSelectionSet(SearchResults));
		});
	}

	private void OnZoomToAllResults(object sender, RoutedEventArgs e)
	{
		if (MapMember is not BasicFeatureLayer layer) return;

		QueuedTask.Run(() =>
		{
			var selectionSet = SearchResultsToSelectionSet(SearchResults);
			MapView.Active.ZoomTo(selectionSet);
			MapView.Active.FlashFeature(selectionSet);
		});
	}

	private void OnReturnToResults(object sender, RoutedEventArgs e)
	{
		ActiveView = View.Find;
	}

	private void OnApplyEdits(object sender, RoutedEventArgs e)
	{
		Inspector.ApplyAsync();
	}

	SelectionSet SearchResultsToSelectionSet(IEnumerable<SearchResult> results)
	{
		return SelectionSet.FromDictionary(new Dictionary<MapMember, IList<long>>
		{
			{
				MapMember,
				results.Select(result => result.Row.GetObjectID()).ToList()
			}
		});
	}
}

public class SearchFieldOption
{
	public object Field { get; init; }
	public string Label { get; init; }
}

public class SearchResult
{
	public Row Row { get; init; }
	public string MatchField { get; init; }
	public string MatchValue { get; init; }
	public IReadOnlyCollection<string> OtherMatchFields { get; init; }

	public string MatchDescription => $"{MatchField}: {MatchValue}";
	public string OtherMatchFieldsDescription
	{
		get
		{
			if (OtherMatchFields.Count == 0)
			{
				return null;
			}
			return $"Other matches: {string.Join(", ", OtherMatchFields)}";
		}
	}
}
