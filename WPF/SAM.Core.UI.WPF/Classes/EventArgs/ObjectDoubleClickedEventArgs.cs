// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace SAM.Core.UI.WPF
{
    public class ObjectDoubleClickedEventArgs : EventArgs
    {
        public ModelVisual3D ModelVisual3D { get; }
        public MouseButtonEventArgs MouseButtonEventArgs { get; }

        public ObjectDoubleClickedEventArgs(MouseButtonEventArgs mouseButtonEventArgs, ModelVisual3D modelVisual3D)
        {
            MouseButtonEventArgs = mouseButtonEventArgs;
            ModelVisual3D = modelVisual3D;
        }
    }
}
