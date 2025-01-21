using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Rebound.Helpers.Modding;

enum ReboundAppIntegrity
{
    Installed,
    Corrupt,
    NotInstalled
}

internal interface IReboundApp
{
    public void Install();

    public void Uninstall();

    public ReboundAppIntegrity GetIntegrity();
}