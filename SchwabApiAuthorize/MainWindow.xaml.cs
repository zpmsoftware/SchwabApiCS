// <copyright file="MainWindow.xaml.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.Windows;
using SchwabApiCS;

namespace SchwabApiAuthorize
{
    // https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string[] CommandLineArgs = new string[0]; // set by app.xaml.cs

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                this.Visibility = Visibility.Hidden;

                if (CommandLineArgs.Length == 0)
                    throw new ArgumentException("tokenDataFileName is a required command line parameter ");

                var tokenDataFileName = CommandLineArgs[0];
                var schwabTokens = new SchwabTokens(tokenDataFileName); // gotta get the tokens First.

                SchwabApiCS_WPF.ApiAuthorize.Open(tokenDataFileName);
            }
            catch (Exception ex)
            {
                var msg = SchwabApi.ExceptionMessage(ex);
                MessageBox.Show(msg.Message, "SchwabApiAuthorize " + msg.Title);
            }
            Close();
        }
    }
}