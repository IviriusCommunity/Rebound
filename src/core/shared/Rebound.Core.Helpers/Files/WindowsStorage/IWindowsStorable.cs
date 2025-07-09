// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public interface IWindowsStorable : IDisposable
	{
		Windows.Win32.ComPtr<IShellItem> ThisPtr { get; }
	}
}
