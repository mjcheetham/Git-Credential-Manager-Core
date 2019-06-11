//
//  LineWriter.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-12.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

protocol LineWriter {
    func writeLine(_ str: String)
}

extension LineWriter {
    func WriteAllLines(lines: [String]) {
        for line in lines {
            self.writeLine(line)
        }
    }
}

class FileLineWriter: LineWriter {
    private let handle: FileHandle

    init(handle: FileHandle) {
        self.handle = handle
    }

    convenience init(path: String) {
        let fileManager = FileManager()
        if !fileManager.fileExists(atPath: path) {
            fileManager.createFile(atPath: path, contents: Data())
        }
        let handle = FileHandle(forWritingAtPath: path)!
        self.init(handle: handle)
    }

    func writeLine(_ str: String) {
        self.handle.write(str.data(using: .utf8)!)
        self.handle.write("\n".data(using: .utf8)!)
    }
}
