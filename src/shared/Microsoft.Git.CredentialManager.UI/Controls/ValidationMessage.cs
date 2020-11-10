// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Microsoft.Git.CredentialManager.UI.ViewModels.Validation;

namespace Microsoft.Git.CredentialManager.UI.Controls
{
    public class ValidationMessage : UserControl
    {
        private const double DefaultTextChangeThrottle = 0.2;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(Validator))
            {
                ShowError = Validator.ValidationResult.Status == ValidationStatus.Invalid;
                IsVisible = ShowError;
                Text = Validator.ValidationResult.Message;

                // This might look like an event handler leak, but we're making sure Validator can
                // only be set once. If we ever want to allow it to be set more than once, we'll need
                // to make sure to unsubscribe this event.
                Validator.ValidationResultChanged += (s, vrce) =>
                {
                    ShowError = Validator.ValidationResult.Status == ValidationStatus.Invalid;
                    IsVisible = ShowError;
                    Text = Validator.ValidationResult.Message;
                };
            }

            base.OnPropertyChanged(e);
        }

        public static readonly AvaloniaProperty TextProperty = AvaloniaProperty.Register<ValidationMessage, string>(nameof(Text));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            private set { SetValue(TextProperty, value); }
        }

        public static readonly AvaloniaProperty ShowErrorProperty = AvaloniaProperty.Register<ValidationMessage, bool>(nameof(ShowError));

        public bool ShowError
        {
            get { return (bool)GetValue(ShowErrorProperty); }
            set { SetValue(ShowErrorProperty, value); }
        }

        public static readonly AvaloniaProperty TextChangeThrottleProperty = AvaloniaProperty.Register<ValidationMessage, double>(nameof(TextChangeThrottle), defaultValue: DefaultTextChangeThrottle);

        public double TextChangeThrottle
        {
            get { return (double)GetValue(TextChangeThrottleProperty); }
            set { SetValue(TextChangeThrottleProperty, value); }
        }

        public static readonly AvaloniaProperty ValidatorProperty = AvaloniaProperty.Register<ValidationMessage, PropertyValidator>(nameof(Validator));

        public PropertyValidator Validator
        {
            get { return (PropertyValidator)GetValue(ValidatorProperty); }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(ValidatorProperty));
                Debug.Assert(Validator == null, "Only set this property once for now. If we really need it to be set more than once, we need to make sure we're not leaking event handlers");
                SetValue(ValidatorProperty, value);
            }
        }

        public static readonly AvaloniaProperty FillProperty =
            AvaloniaProperty.Register<ValidationMessage, Brush>(nameof(Fill), new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)));

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly AvaloniaProperty ErrorAdornerTemplateProperty = AvaloniaProperty.Register<ValidationMessage, string>(nameof(ErrorAdornerTemplate), "validationTemplate");

        public string ErrorAdornerTemplate
        {
            get { return (string)GetValue(ErrorAdornerTemplateProperty); }
            set { SetValue(ErrorAdornerTemplateProperty, value); }
        }

        private bool IsAdornerEnabled()
        {
            return !string.IsNullOrEmpty(ErrorAdornerTemplate)
                && !ErrorAdornerTemplate.Equals("None", StringComparison.OrdinalIgnoreCase);
        }
    }
}
