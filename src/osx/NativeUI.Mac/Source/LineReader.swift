//
//  LineReader.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-11.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

protocol LineReader {
    func readLine() -> String?
}

extension LineReader {
    func readAllLines() -> [String] {
        var lines: [String] = []
        while let line = self.readLine() {
            lines.append(line)
        }
        return lines
    }
}

class FileLineReader: LineReader {
    private let handle: FileHandle

    init(handle: FileHandle) {
        self.handle = handle
    }

    convenience init?(path: String) {
        guard let handle = FileHandle(forReadingAtPath: path) else {
            return nil
        }
        self.init(handle: handle)
    }

    func readLine() -> String? {
        var line: String = ""
        var lastChar: Character?

        // Read data one byte at a time as a UTF8 string until we hit the new line character
        while lastChar != "\n" {
            let data = self.handle.readData(ofLength: 1)
            if (data.isEmpty)
            {
                if (line.count > 0) {
                    // We reached the end of the file; break from the loop and return the line so far
                    break
                } else {
                    // We reached the end of the file; and we have no line (EOF); return nil
                    return nil
                }
            }

            let char: Character = Character(String(data: data, encoding: .utf8)!)
            if (char != "\n")
            {
                line.append(char)
            }
            lastChar = char
        }

        return line
    }
}
