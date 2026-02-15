// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;

namespace Rebound.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReboundAppAttribute(string singleProcessTaskName) : Attribute
{
    public string SingleProcessTaskName { get; } = singleProcessTaskName;
}