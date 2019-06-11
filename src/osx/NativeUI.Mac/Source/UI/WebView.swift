//
//  WebView.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-12.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation
import AppKit
import WebKit

class WebView: NSObject, NSWindowDelegate, WKNavigationDelegate {
    private let DEFAULT_WIDTH = 420
    private let DEFAULT_HEIGHT = 650

    private let appd: AppDelegate
    private let startUrl: String
    private let endUrl: String
    private let title: String?
    private let parentWindow: NSWindow?
    private var finalUrl: String?
    private var window: NSWindow?
    private var progress: NSProgressIndicator?

    init(startUrl: String, endUrl: String, title: String? = nil, parentWindow: NSWindow? = nil) {
        self.appd = AppDelegate()
        self.startUrl = startUrl
        self.endUrl = endUrl
        self.title = title
        self.parentWindow = parentWindow
    }

    func show() -> String? {
        self.window = self.createWindow()
        let contentView = self.window!.contentView!

        let webView = self.createWebView(frame: contentView.frame)
        contentView.addSubview(webView)

        self.progress = self.createProgressIndicator()
        contentView.addSubview(self.progress!)

        self.window!.makeKeyAndOrderFront(nil)
        webView.load(URLRequest(url: URL(string: self.startUrl)!))
        self.appd.run()

        return finalUrl
    }

    private func createWindow() -> NSWindow {
        let parentFrame = self.parentWindow?.frame ?? NSScreen.main!.frame
        let rect = CGRect(origin: CGPoint.zero,
                          size:   CGSize(width:  DEFAULT_WIDTH,
                                         height: DEFAULT_HEIGHT))
            .centerWithin(parentFrame)

        let window = NSWindow(contentRect: rect,
                              styleMask:   [.titled, .closable],
                              backing:     .buffered,
                              defer:       false)
        window.backgroundColor = .windowBackgroundColor
        window.setAccessibilityIdentifier("GCM_WINDOW")
        window.title = self.title ?? "Git Credential Manager"
        window.delegate = self
        window.contentView!.autoresizesSubviews = true

        return window
    }

    private func createWebView(frame: CGRect) -> WKWebView {
        let webView = WKWebView(frame: frame)
        webView.autoresizingMask = [.height, .width]
        webView.setAccessibilityIdentifier("GCM_WEBVIEW")
        webView.navigationDelegate = self

        return webView
    }
    
    private func createProgressIndicator() -> NSProgressIndicator {
        let rect = CGRect(x: (DEFAULT_WIDTH  / 2) - 16,
                          y: (DEFAULT_HEIGHT / 2) - 16,
                          width: 32,
                          height: 32)
        let progress = NSProgressIndicator(frame: rect)
        progress.style = .spinning
        progress.autoresizingMask = [.minXMargin, .maxXMargin, .minYMargin, .maxYMargin]
        progress.isHidden = false
        progress.startAnimation(nil)

        return progress
    }

    func windowWillClose(_ notification: Notification) {
        self.appd.stop()
    }

    func webView(_ webView: WKWebView, didFinish navigation: WKNavigation!) {
        progress!.isHidden = true
        progress!.stopAnimation(nil)
    }
    
    func webView(_ webView: WKWebView, decidePolicyFor navigationAction: WKNavigationAction, decisionHandler: @escaping (WKNavigationActionPolicy) -> Void) {
        // Ensure we're navigating to somewhere
        guard let requestUrl = navigationAction.request.url else {
            decisionHandler(.cancel)
            return
        }

        // Check if we're going to the end URL
        if (requestUrl.absoluteString.lowercased().starts(with: self.endUrl.lowercased())) {
            decisionHandler(.cancel)
            self.onComplete(url: requestUrl.absoluteString)
            return
        }

        // Only permit HTTPS URLs and "about:blank"
        if requestUrl.absoluteString.caseInsensitiveCompare("about:blank") != .orderedSame &&
           requestUrl.scheme?.caseInsensitiveCompare("https") != .some(.orderedSame) {
            decisionHandler(.cancel)
        }

        decisionHandler(.allow)
    }

    private func onComplete(url: String) {
        self.finalUrl = url
        self.window!.close()
    }
}
