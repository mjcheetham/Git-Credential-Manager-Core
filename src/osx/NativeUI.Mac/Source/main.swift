//
//  main.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-11.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

enum UIHelperError: Error{
    case usage(String)
}

let tracer = Tracer()
var exitCode: Int32 = 0

let traceEnvar        = ProcessInfo.processInfo.environment["GCM_TRACE"]
let traceSecretsEnvar = ProcessInfo.processInfo.environment["GCM_TRACE_SECRETS"]

if traceEnvar.isTruthy {
    // Add stderror writer
    tracer.addWriter(writer: FileLineWriter(handle: FileHandle.standardError))
} else if traceEnvar.isLocalFilePath {
    // Add file writer
    tracer.addWriter(writer: FileLineWriter(path: traceEnvar!))
}

if traceSecretsEnvar.isTruthy {
    // Set trace system to output secrets
    tracer.traceSecrets = true
}

tracer.writeLine("Starting Native UI for Mac...")

let stdinReader = FileLineReader(handle: FileHandle.standardInput)
let dictReader = LineDictionaryReader(lineReader: stdinReader)
let input = dictReader.readDictionary()

guard let startUrl = input["start"] else {
    throw UIHelperError.usage("Missing input 'start'")
}
guard let endUrl = input["end"] else {
    throw UIHelperError.usage("Missing input 'end'")
}
let title = input["title"]

let webView = WebView(startUrl: startUrl, endUrl: endUrl, title: title)
let finalUrl = webView.show()

let stdoutWriter = FileLineWriter(handle: FileHandle.standardOutput)
let dictWriter = LineDictionaryWriter(lineWriter: stdoutWriter)
var output: [String:String] = [:]

if let finalUrl = finalUrl {
    output["final"] = finalUrl
    exitCode = 0
} else {
    output["dismissed"] = "1"
    exitCode = 0
}

dictWriter.writeDictionary(output)
exit(exitCode)
