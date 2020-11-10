// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Git.CredentialManager.UI.Controls;
using Microsoft.Git.CredentialManager.UI.ViewModels;

namespace Microsoft.Git.CredentialManager.UI
{
    public interface IGui
    {
        /// <summary>
        /// Present the user with a <see cref="Window"/>.
        /// </summary>
        /// <param name="windowCreator"><see cref="Window"/> factory.</param>
        /// <returns>
        /// Returns `<see langword="true"/>` if the user completed the dialog; otherwise `<see langword="false"/>`
        /// if the user canceled or abandoned the dialog.
        /// </returns>
        bool ShowWindow(Func<Window> windowCreator);

        /// <summary>
        /// Present the user with a <see cref="DialogWindow"/>.
        /// </summary>
        /// <returns>
        /// Returns `<see langword="true"/>` if the user completed the dialog and the view model is valid;
        /// otherwise `<see langword="false"/>` if the user canceled or abandoned the dialog, or the view
        /// model is invalid.
        /// </returns>
        /// <param name="viewModel">Window view model.</param>
        /// <param name="contentCreator">Window content factory.</param>
        bool ShowDialogWindow(WindowViewModel viewModel, Func<object> contentCreator);
    }

    public class AvaloniaGui : IGui
    {
        private readonly CancellationTokenSource _cts;
        private readonly IntPtr _parentHwnd = IntPtr.Zero;

        public AvaloniaGui(CancellationTokenSource cts)
        {
            _cts = cts;

            string envar = Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.GcmParentWindow);

            if (long.TryParse(envar, out long ptrInt))
            {
                _parentHwnd = new IntPtr(ptrInt);
            }
        }

        public bool ShowWindow(Func<Window> windowCreator)
        {
            // TODO: support parenting to handle
            Window window = windowCreator();
            window.Closed += (sender, args) => _cts.Cancel();
            window.Show();

            Dispatcher.UIThread.MainLoop(_cts.Token);

            // TODO: get window result
            return true;
        }

        public bool ShowDialogWindow(WindowViewModel viewModel, Func<object> contentCreator)
        {
            object content = contentCreator();

            // TODO: support parenting to handle
            Window window = new DialogWindow(viewModel, content);
            window.Closed += (sender, args) => _cts.Cancel();
            window.Show();

            Dispatcher.UIThread.MainLoop(_cts.Token);

            // TODO: get window result
            return viewModel.IsValid;
        }
    }
}
