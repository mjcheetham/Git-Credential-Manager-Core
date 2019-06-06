// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Git.CredentialManager.Tests.Objects
{
    public class TestNativeUi : INativeUi
    {
        public Task<WebViewResult> ShowWebViewAsync(WebViewOptions options, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
