//
//  Tracer.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-12.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

class Tracer {
    private var writers: [LineWriter] = []

    var traceSecrets: Bool = false

    func addWriter(writer: LineWriter) {
        self.writers.append(writer)
    }

    func writeLine(_ str: String) {
        for writer in self.writers {
            writer.writeLine(str)
        }
    }

    func writeLine(_ str: String, secrets: String...) {
        let secretStr: String

        if self.traceSecrets {
            secretStr = String.init(format: str, arguments: secrets)
        } else {
            secretStr = String.init(format: str, arguments: Array.init(repeating: "********", count: secrets.count))
        }

        self.writeLine(secretStr)
    }
}
