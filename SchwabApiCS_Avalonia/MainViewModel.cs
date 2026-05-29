using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SchwabApiCS;
using static SchwabApiCS.SchwabApi;

namespace SchwabApiCS_Avalonia
{
    public partial class MainViewModel : ObservableObject
    {
        public event UrlChangedEventHandler? UrlChanged;
        public delegate void UrlChangedEventHandler();

        private SchwabTokens? _schwabTokens;
        private const string _defaultTokensFile = "SchwabTokens.json";

        [ObservableProperty]
        private string _authorizeUri = string.Empty;

        [ObservableProperty]
        private string _publicApiKey = "your appkey";

        [ObservableProperty]
        private string _privateApiKey = "your secret";

        [ObservableProperty]
        private string _redirectUrl = "https://127.0.0.1";

        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private string _authExpiration = DateTime.MinValue.ToString();

        private string? _defaultTokens = null;
        private string DefaultTokens
        {
            get
            {
                if (_defaultTokens == null)
                {
                    _defaultTokens = GetTokens();
                }
                return _defaultTokens;
            }
        }

        public MainViewModel()
        {
            if (!File.Exists(_defaultTokensFile))
            {
                UpdateTokensData(_defaultTokensFile, DefaultTokens);
            }
            else
            {
                LoadTokensData(_defaultTokensFile);
            }
        }

        [RelayCommand]
        private void RefreshPage()
        {
            UrlChanged?.Invoke();
        }

        [RelayCommand]
        private void UpdateSchwabTokens()
        {
            UpdateTokensData(_defaultTokensFile, GetTokens());
        }

        [RelayCommand]
        private void ShowTokensFile()
        {
            var fullPath = Path.GetFullPath(_defaultTokensFile);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", $"-R \"{fullPath}\"");
            else
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = Path.GetDirectoryName(fullPath)!,
                        UseShellExecute = true,
                    }
                );
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(AuthExpiration))
            {
                if (DateTime.Now < DateTime.Parse(AuthExpiration))
                {
                    IsAuthenticated = true;
                }
                else
                {
                    IsAuthenticated = false;
                }
            }
        }

        public void Navigated(string url, string frameName)
        {
            Debug.Assert(_schwabTokens != null);
            if (url.StartsWith(_schwabTokens.tokens.Redirect_uri))
            {
                if (url.Contains("error=access_denied"))
                    throw new SchwabApiException(
                        "Reauthorization was not completed properly, restart and try again."
                    );

                var p = url.IndexOf("code=") + 5;
                var p2 = url.IndexOf("&session");
                var authCode = Uri.UnescapeDataString(url.Substring(p, p2 - p));

                var httpClient = new HttpClient();
                var content = new StringContent(
                    "code="
                        + authCode
                        + "&redirect_uri="
                        + _schwabTokens.tokens.Redirect_uri
                        + "&grant_type=authorization_code",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                );
                httpClient.DefaultRequestHeaders.Add(
                    "Authorization",
                    "Basic "
                        + SchwabApi.Base64Encode(
                            _schwabTokens.tokens.AppKey + ":" + _schwabTokens.tokens.Secret
                        )
                );

                var response = httpClient
                    .PostAsync(SchwabTokens.baseUrl + "/token", content)
                    .Result;
                _schwabTokens.SaveTokens(response, "NavCompleted");

                AuthExpiration = _schwabTokens.tokens.RefreshTokenExpires.ToString();
            }
        }

        private void UpdateTokensData(string path, string json)
        {
            File.WriteAllText(path, json);
            _schwabTokens = new SchwabTokens(path);
            AuthorizeUri = _schwabTokens.AuthorizeUri.AbsoluteUri;
        }

        private void LoadTokensData(string path)
        {
            if (File.Exists(path))
            {
                _schwabTokens = new SchwabTokens(path);
                AuthExpiration = _schwabTokens.tokens.RefreshTokenExpires.ToString();
                AuthorizeUri = _schwabTokens.AuthorizeUri.AbsoluteUri;
            }
            else
            {
                UpdateTokensData(path, DefaultTokens);
            }
        }

        private string GetTokens()
        {
            return JsonSerializer.Serialize(
                new
                {
                    AccessToken = "",
                    RefreshToken = "",
                    AccessTokenExpires = DateTime.MinValue.ToString(),
                    RefreshTokenExpires = DateTime.MinValue.ToString(),
                    AppKey = PublicApiKey,
                    Secret = PrivateApiKey,
                    Redirect_uri = RedirectUrl,
                }
            );
        }
    }
}
