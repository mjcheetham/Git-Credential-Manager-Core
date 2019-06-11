//
//  AppDelegate.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-13.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation
import AppKit

class AppDelegate: NSObject, NSApplicationDelegate {
    override init() {
        super.init()
        NSApplication.shared.delegate = self
        NSApplication.shared.mainMenu = self.createMainMenu()
        NSApplication.shared.setActivationPolicy(.regular)
    }

    func run() {
        NSApplication.shared.run()
    }

    func stop() {
        NSApplication.shared.stop(nil)

        // Send an event to the app, because "stop" only stops running the
        // delegate after processing an event.
        let event = NSEvent.otherEvent(with: .applicationDefined,
                                       location: CGPoint.zero,
                                       modifierFlags: .init(),
                                       timestamp: .zero,
                                       windowNumber: 0,
                                       context: nil,
                                       subtype: 0,
                                       data1: 0,
                                       data2: 0)!

        NSApplication.shared.postEvent(event, atStart: false)
    }
    
    private func createMainMenu() -> NSMenu {
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "Quit Git Credential Manager", action: #selector(NSApplication.terminate), keyEquivalent: "q")

        let mainMenu = NSMenu()
        let appItem = NSMenuItem()
        mainMenu.addItem(appItem)
        mainMenu.setSubmenu(appMenu, for: appItem)

        return mainMenu
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApplication.shared.activate(ignoringOtherApps: true)
    }

    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        // Stop the runloop and allow the rest of the application to continue to completion,
        // rather than allowing NSApplication.terminate to call "exit(..)"
        self.stop()
        return .terminateCancel
    }
}
