using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace QuickActions.Views;

public partial class ServiceView : ViewBase
{
	static readonly Regex _urlRegex = new(
		"(?<serverUrl>.*)/rest/services/(?<serviceName>.*)/(?<serviceType>MapServer|FeatureServer|ImageServer)",
		RegexOptions.Compiled
	);

	static readonly HttpClient _client = new(new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
	});

	string _serviceUrl;
	string _adminUrl;
	string _managerUrl;

	public ServiceView()
	{
		InitializeComponent();
	}

	bool _isValid;
	public bool IsValid
	{
		get => _isValid;
		set
		{
			_isValid = value;
			NotifyPropertyChanged();
		}
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

	string _serviceName;
	public string ServiceName
	{
		get => _serviceName;
		set
		{
			_serviceName = value;
			NotifyPropertyChanged();
		}
	}

	string _serviceType;
	public string ServiceType
	{
		get => _serviceType;
		set
		{
			_serviceType = value;
			NotifyPropertyChanged();
		}
	}

	string _serviceState;
	public string ServiceState
	{
		get => _serviceState;
		set
		{
			_serviceState = value;
			NotifyPropertyChanged();
		}
	}

	bool _isProcessing;
	public bool IsProcessing
	{
		get => _isProcessing;
		set
		{
			_isProcessing = value;
			NotifyPropertyChanged();
			NotifyPropertyChanged(nameof(CanDoActions));
		}
	}

	public bool CanDoActions => _isValid && !IsProcessing;

	protected override Task InitializeAsync()
	{
		return QueuedTask.Run(() =>
		{
			var connector = ((IDisplayTable)MapMember).GetTable()
				.GetDatastore()
				.GetConnector();
			if (connector is not ServiceConnectionProperties connectionProperties)
			{
				SetError("Only service data sources are supported.");
				return;
			}

			_serviceUrl = connectionProperties.URL.ToString();
			var match = _urlRegex.Match(_serviceUrl);
			if (!match.Success)
			{
				SetError("Service URL is invalid.");
				return;
			}

			var serverUrl = match.Groups["serverUrl"].Value;
			ServiceName = match.Groups["serviceName"].Value;
			ServiceType = match.Groups["serviceType"].Value switch
			{
				"FeatureServer" => "MapServer",
				string other => other
			};

			_isValid = true;
			_adminUrl = $"{serverUrl}/admin/services/{ServiceName}.{ServiceType}";

			var serviceNameParts = ServiceName.Split('/');
			if (serviceNameParts.Length == 1)
			{
				_managerUrl = $"{serverUrl}/manager/service.html?name={ServiceName}.{ServiceType}";
			}
			else
			{
				_managerUrl = $"{serverUrl}/manager/service.html?name={serviceNameParts[1]}.{ServiceType}&folder={serviceNameParts[0]}";
			}

			IsValid = true;
			NotifyPropertyChanged(nameof(CanDoActions));
		});
	}

	void SetError(string errorMessage)
	{
		ErrorMessage = errorMessage;
		HasError = true;
	}

	(bool success, JsonDocument doc) HandleResponse(HttpResponseMessage response)
	{
		if (!response.IsSuccessStatusCode)
		{
			SetError($"Server returned status code {(int)response.StatusCode}");
			return (false, default);
		}

		var doc = JsonDocument.Parse(response.Content.ReadAsStream());

		if (doc.RootElement.TryGetProperty("error", out var errorProp))
		{
			if (errorProp.TryGetProperty("message", out var messageProp))
			{
				SetError(messageProp.GetString());
			}
			else
			{
				SetError("Server returned an error");
			}
			return (false, null);
		}
		else if (doc.RootElement.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "error")
		{
			if (doc.RootElement.TryGetProperty("messages", out var messagesProp))
			{
				var messages = messagesProp.EnumerateArray().Select(item => item.GetString());
				SetError(string.Join(Environment.NewLine, messages));
			}
			else
			{
				SetError("Server returned an error");
			}
			return (false, null);
		}

		return (true, doc);
	}

	async Task UpdateServiceState()
	{
		ServiceState = null;
		var token = ArcGISPortalManager.Current.GetActivePortal().GetToken();
		var response = await _client.GetAsync($"{_adminUrl}/status?f=json&token={token}");
		var (success, doc) = HandleResponse(response);
		if (!success) return;
		ServiceState = doc.RootElement.GetProperty("realTimeState").GetString();
	}

	void OnCheckState(object sender, RoutedEventArgs e)
	{
		Task.Run(async () =>
		{
			IsProcessing = true;
			try
			{
				await UpdateServiceState();
			}
			finally
			{
				IsProcessing = false;
			}
		});
	}

	void OnStart(object sender, RoutedEventArgs e)
	{
		Task.Run(async () =>
		{
			IsProcessing = true;
			try
			{
				var token = ArcGISPortalManager.Current.GetActivePortal().GetToken();
				var response = await _client.PostAsync($"{_adminUrl}/start?f=json&token={token}", null);
				var (success, _) = HandleResponse(response);
				if (success)
				{
					await UpdateServiceState();
				}
			}
			finally
			{
				IsProcessing = false;
			}
		});
	}

	void OnStop(object sender, RoutedEventArgs e)
	{
		Task.Run(async () =>
		{
			IsProcessing = true;
			try
			{
				var token = ArcGISPortalManager.Current.GetActivePortal().GetToken();
				var response = await _client.PostAsync($"{_adminUrl}/stop?f=json&token={token}", null);
				var (success, _) = HandleResponse(response);
				if (success)
				{
					await UpdateServiceState();
				}
			}
			finally
			{
				IsProcessing = false;
			}
		});
	}

	void OnRestart(object sender, RoutedEventArgs e)
	{
		Task.Run(async () =>
		{
			IsProcessing = true;
			try
			{
				var token = ArcGISPortalManager.Current.GetActivePortal().GetToken();
				var response = await _client.PostAsync($"{_adminUrl}/stop?f=json&token={token}", null);
				var (success, _) = HandleResponse(response);
				if (success)
				{
					response = await _client.PostAsync($"{_adminUrl}/start?f=json&token={token}", null);
					(success, _) = HandleResponse(response);
					if (success)
					{
						await UpdateServiceState();
					}
				}
			}
			finally
			{
				IsProcessing = false;
			}
		});
	}

	private void OnOpenRest(object sender, RoutedEventArgs e)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = _serviceUrl,
			UseShellExecute = true
		});
	}

	private void OnOpenManager(object sender, RoutedEventArgs e)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = _managerUrl,
			UseShellExecute = true
		});
	}

	private void OnOpenAdmin(object sender, RoutedEventArgs e)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = _adminUrl,
			UseShellExecute = true
		});
	}
}
