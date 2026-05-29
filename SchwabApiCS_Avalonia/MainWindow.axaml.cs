using System;
using Avalonia.Controls;

namespace SchwabApiCS_Avalonia
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DataContext is MainViewModel vm)
            {
                // Navigate to the initial authorize URI
                NavigateWebView(vm.AuthorizeUri);

                // Re-navigate whenever the ViewModel requests a page refresh
                vm.UrlChanged += () => NavigateWebView(vm.AuthorizeUri);
            }
        }

        private void NavigateWebView(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;
            var wv = this.FindControl<NativeWebView>("webview");
            if (wv != null && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                wv.Source = uri;
        }

        private void WebView_NavigationCompleted(
            object? sender,
            WebViewNavigationCompletedEventArgs e
        )
        {
            if (
                sender is NativeWebView wv
                && DataContext is MainViewModel vm
                && wv.Source is not null
            )
                vm.Navigated(wv.Source.ToString(), string.Empty);
        }
    }
}
