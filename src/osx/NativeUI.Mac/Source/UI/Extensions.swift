//
//  Extensions.swift
//  NativeUI.Mac
//
//  Created by Matthew John Cheetham on 2019-06-13.
//  Copyright © 2019 Microsoft Corporatiopn. All rights reserved.
//

import Foundation
import CoreGraphics

extension CGRect {
    func centerWithin(_ rect: CGRect) -> CGRect {
        let x = rect.minX + ((rect.width - self.width) / 2)
        let y = rect.minY + ((rect.height - self.height) / 2)

        return CGRect(x: x < 0 ? 0 : x,
                      y: y < 0 ? 0 : y,
                      width: self.width,
                      height: self.height)
    }
}
