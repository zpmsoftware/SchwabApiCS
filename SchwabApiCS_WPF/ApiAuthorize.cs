// <copyright file="ApiAuthorize.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>using Microsoft.Web.WebView2.Core;

using SchwabApiCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static SchwabApiCS.SchwabApi;
using System.Windows.Controls;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;

namespace SchwabApiCS_WPF
{
    public class ApiAuthorize
    {
        /// <summary>
        /// Schwab Authorization - opens web browser to https://api.schwabapi.com/v1/oauth/authorize?client_id
        /// and saves the response tokens in tokenDataFileName.
        /// Only need to call when SchwabTokens.NeedsReAuthorization is true.
        /// </summary>
        /// <param name="tokenDataFileName"></param>
        public static void Open(string tokenDataFileName)
        {
            var win = new ApiAuthorizeWindow(tokenDataFileName);
            win.ShowDialog(); // wait until window closes
        }

        private class ApiAuthorizeWindow : Window
        {
            private SchwabTokens schwabTokens;
            private WebView2 webView;

            public ApiAuthorizeWindow(string tokenDataFileName)
            {
                this.Title = "Schwab API Authorize";
                this.Height = 900;
                this.Width = 1100;
                this.Topmost = true;

                webView = new WebView2() { Name = "webView", Height = 900, Width = 1100 };
                var grid = new Grid();
                grid.Children.Add(webView);
                this.Content = grid;

                schwabTokens = new SchwabTokens(tokenDataFileName);
                webView.Source = schwabTokens.AuthorizeUri;
                webView.NavigationCompleted += NavCompleted;
            }

            /// <summary>
            /// Website re-authorization complete 
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="args"></param>
            private void NavCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
            {
                var wv = (Microsoft.Web.WebView2.Wpf.WebView2)sender;

                if (wv.Source.AbsoluteUri.StartsWith(schwabTokens.tokens.Redirect_uri)) // "https://127.0.0.1"
                {

                    if (wv.Source.AbsoluteUri.Contains("error=access_denied"))
                        throw new SchwabApiException("Reauthorization was not completed properly, restart and try again.");

                    var p = wv.Source.AbsoluteUri.IndexOf("code=") + 5;
                    var p2 = wv.Source.AbsoluteUri.IndexOf("&session");
                    var authCode = Uri.UnescapeDataString(wv.Source.AbsoluteUri.Substring(p, p2 - p));

                    var httpClient = new HttpClient();
                    var content = new StringContent("code=" + authCode + "&redirect_uri=" + schwabTokens.tokens.Redirect_uri + "&grant_type=authorization_code",
                    Encoding.UTF8, "application/x-www-form-urlencoded");
                    httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " +
                                    SchwabApi.Base64Encode(schwabTokens.tokens.AppKey + ":" + schwabTokens.tokens.Secret));

                    var response = httpClient.PostAsync(SchwabTokens.baseUrl + "/token", content).Result;
                    schwabTokens.SaveTokens(response, "NavCompleted");

                    webView.Visibility = Visibility.Collapsed;
                    webView.NavigationCompleted -= NavCompleted;
                    this.Close();
                }
            }
        }
    }
}
