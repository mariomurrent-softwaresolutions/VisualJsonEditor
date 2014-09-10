﻿//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Visual JSON Editor">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>http://visualjsoneditor.codeplex.com/license</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using MyToolkit.Mvvm;
using MyToolkit.Networking;
using MyToolkit.UI;
using MyToolkit.Utilities;
using VisualJsonEditor.Domain;
using VisualJsonEditor.Utilities;
using VisualJsonEditor.ViewModels;
using Xceed.Wpf.AvalonDock;

namespace VisualJsonEditor.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ApplicationConfiguration _configuration;

        public MainWindow()
        {
            InitializeComponent();
            ViewModelHelper.RegisterViewModel(Model, this);

            RegisterFileOpenHandler();
            RegisterShortcuts();

            LoadConfiguration();
            CheckForApplicationUpdate();

            Closing += OnWindowClosing;
        }

        /// <summary>Gets the view model. </summary>
        public MainWindowModel Model { get { return (MainWindowModel)Resources["ViewModel"]; } }

        /// <summary>Gets the configuration file name. </summary>
        public string ConfigurationFileName
        {
            get { return "VisualJsonEditor/Config"; }
        }

        private void RegisterFileOpenHandler()
        {
            var fileHandler = new FileOpenHandler();
            fileHandler.FileOpen += (sender, args) => Model.OpenDocumentAsync(args.FileName);
            fileHandler.Initialize(this);
        }

        private void RegisterShortcuts()
        {
            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.N, ModifierKeys.Control),
                () => Model.CreateDocumentCommand.TryExecute());
            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.O, ModifierKeys.Control),
                () => Model.OpenDocumentCommand.TryExecute());
            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.S, ModifierKeys.Control),
                () => Model.SaveDocumentCommand.TryExecute(Model.SelectedDocument));

            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.W, ModifierKeys.Control),
                () => Model.CloseDocumentCommand.TryExecute(Model.SelectedDocument));

            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.Z, ModifierKeys.Control),
                () => Model.UndoCommand.TryExecute(Model.SelectedDocument));
            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.Y, ModifierKeys.Control),
                () => Model.RedoCommand.TryExecute(Model.SelectedDocument));

            ShortcutManager.RegisterShortcut(typeof(MainWindow), new KeyGesture(Key.U, ModifierKeys.Control),
                () => Model.ValidateDocumentCommand.TryExecute(Model.SelectedDocument));
        }

        private async void LoadConfiguration()
        {
            _configuration = JsonApplicationConfiguration.Load<ApplicationConfiguration>(ConfigurationFileName, true, true);

            Width = _configuration.WindowWidth;
            Height = _configuration.WindowHeight;
            WindowState = _configuration.WindowState;

            Model.Configuration = _configuration;

            if (_configuration.IsFirstStart)
            {
                _configuration.IsFirstStart = false;
                await Model.OpenDocumentAsync("Samples/Sample.json", true);
            }
        }

        private void SaveConfiguration()
        {
            _configuration.WindowWidth = Width;
            _configuration.WindowHeight = Height;
            _configuration.WindowState = WindowState;

            JsonApplicationConfiguration.Save(ConfigurationFileName, _configuration, true);
        }

        private void OnRibbonsLoaded(object sender, RoutedEventArgs e)
        {
            // HACK: Used to hide the quick access bar in the ribbon control
            //var child = VisualTreeHelper.GetChild((DependencyObject)sender, 0) as Grid;
            //if (child != null)
            //    child.RowDefinitions[0].Height = new GridLength(0);
        }

        private async void CheckForApplicationUpdate()
        {
            var updater = new ApplicationUpdater(GetType().Assembly, "http://rsuter.com/Projects/VisualJsonEditor/updates.xml");
            await updater.CheckForUpdate();
        }

        private async void OnDocumentClosing(object sender, DocumentClosingEventArgs args)
        {
            args.Cancel = true;
            await Model.CloseDocumentAsync((JsonDocument)args.Document.Content);
        }

        private async void OnWindowClosing(object sender, CancelEventArgs args)
        {
            args.Cancel = true;

            foreach (var document in Model.Documents.ToArray())
            {
                var result = await Model.CloseDocumentAsync(document);
                if (!result)
                    return;
            }

            Closing -= OnWindowClosing;
            SaveConfiguration();
            await Dispatcher.InvokeAsync(Close);
        }

        private void OnShowAboutWindow(object sender, RoutedEventArgs e)
        {
            var dlg = new AboutWindow();
            dlg.Owner = this;
            dlg.ShowDialog();
        }

        private void OnExitApplication(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
