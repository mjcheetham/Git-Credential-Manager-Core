//
//  DictionaryReader.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-11.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

protocol DictionaryReader {
    func readDictionary() -> [String:String]
}

class LineDictionaryReader: DictionaryReader {
    private let reader: LineReader

    init(lineReader: LineReader) {
        self.reader = lineReader
    }

    func readDictionary() -> [String:String] {
        var dict = [String:String]()

        while let line = self.reader.readLine(), !line.isEmpty {
            let split = line.split(separator: "=", maxSplits: 1)
            guard split.count == 2 else {
                // Malformated dictionary line
                continue
            }
            let key = String(split[0])
            let value = String(split[1])
            dict[key] = value
        }

        return dict
    }
}
