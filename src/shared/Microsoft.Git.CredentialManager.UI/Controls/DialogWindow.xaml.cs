// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Git.CredentialManager.UI.Controls
{
    public partial class DialogWindow : Window
    {
        private ContentControl _contentHolder;

        public bool DialogResult { get; set; }

        public DialogWindow()
        {
        }

        public DialogWindow(WindowViewModel viewModel, object content)
        {
            InitializeComponent();

            DataContext = viewModel;
            _contentHolder = this.FindControl<ContentControl>("ContentHolder");
            _contentHolder.Content = content;
            if (viewModel != null)
            {
                viewModel.Accepted += (sender, e) =>
                {
                    DialogResult = true;
                    Close();
                };

                viewModel.Canceled += (sender, e) =>
                {
                    DialogResult = false;
                    Close();
                };
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public WindowViewModel ViewModel => (WindowViewModel) DataContext;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => ViewModel.Cancel();

        private void Border_MouseDown(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
        }
    }
}
