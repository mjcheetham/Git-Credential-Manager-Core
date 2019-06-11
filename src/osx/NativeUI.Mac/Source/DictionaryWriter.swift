//
//  DictionaryWriter.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-12.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

protocol DictionaryWriter {
    func writeDictionary(_ dict: [String:String])
}

class LineDictionaryWriter: DictionaryWriter {
    private let writer: LineWriter

    init(lineWriter: LineWriter) {
        self.writer = lineWriter
    }

    func writeDictionary(_ dict: [String:String]) {
        for (key, value) in dict {
            self.writer.writeLine("\(key)=\(value)")
        }
    }
}
