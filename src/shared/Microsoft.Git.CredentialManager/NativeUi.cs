// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager
{
    public interface INativeUi
    {
        Task<WebViewResult> ShowWebViewAsync(WebViewOptions options, CancellationToken cancellationToken);
    }

    public class WebViewOptions
    {
        public string WindowTitle   { get; set; }
        public string StartLocation { get; set; } = "about:blank";
        public string EndLocation   { get; set; }
    }

    public class WebViewResult
    {
        public bool UserDismissedDialog { get; set; }

        public string FinalLocation { get; set; }
    }

    public abstract class NativeUi : INativeUi
    {
        protected readonly ICommandContext Context;

        protected NativeUi(ICommandContext context)
        {
            EnsureArgument.NotNull(context, nameof(context));

            Context = context;
        }

        public async Task<WebViewResult> ShowWebViewAsync(WebViewOptions options, CancellationToken cancellationToken)
        {
            if (ProcessHelper.TryFindHelperExecutable(Context, Constants.BrowserHelperName, out string helperPath))
            {
                IDictionary<string, string> stdInput = CreateWebViewStdInput(options);
                IDictionary<string,string> stdOutput = await ProcessHelper.InvokeHelperAsync(helperPath, null, stdInput);

                return CreateWebViewResult(stdOutput);
            }

            throw new Exception($"Could not locate helper '{Constants.BrowserHelperName}'");
        }

        private WebViewResult CreateWebViewResult(IDictionary<string, string> stdOutput)
        {
            var result = new WebViewResult();

            if (stdOutput.TryGetValue("dismissed", out string dismissedStr) && bool.TryParse(dismissedStr, out bool userDismissed))
            {
                result.UserDismissedDialog = userDismissed;
            }

            if (stdOutput.TryGetValue("final", out string endLocationStr))
            {
                result.FinalLocation = endLocationStr;
            }

            return result;
        }

        private IDictionary<string, string> CreateWebViewStdInput(WebViewOptions options)
        {
            var dict = new Dictionary<string, string>();

            if (options.WindowTitle != null)
            {
                dict["title"] = options.WindowTitle;
            }

            if (options.StartLocation != null)
            {
                dict["start"] = options.StartLocation;
            }

            if (options.EndLocation != null)
            {
                dict["end"] = options.EndLocation;
            }

            return dict;
        }
    }
}
