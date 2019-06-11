//
//  StringExtensions.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-11.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation

protocol Truthy {
    var isTruthy: Bool { get }
}

protocol FilePathLike {
    var isLocalFilePath: Bool { get }
}

extension Optional: Truthy where Wrapped: Truthy {
    var isTruthy: Bool {
        return self?.isTruthy ?? false
    }
}

extension Optional: FilePathLike where Wrapped: FilePathLike {
    var isLocalFilePath: Bool {
        return self?.isLocalFilePath ?? false
    }
}

extension String: Truthy, FilePathLike {
    var isTruthy: Bool {
        return (self.caseInsensitiveCompare("1")    == ComparisonResult.orderedSame) ||
               (self.caseInsensitiveCompare("yes")  == ComparisonResult.orderedSame) ||
               (self.caseInsensitiveCompare("true") == ComparisonResult.orderedSame)
    }

    var isLocalFilePath: Bool {
        let fullPath = NSString(string: self).expandingTildeInPath
        return fullPath.hasPrefix("/")
    }
}
