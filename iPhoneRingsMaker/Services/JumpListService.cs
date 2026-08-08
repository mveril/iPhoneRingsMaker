using System;
using System.Collections.Generic;
using System.Text;
using iPhoneRingsMaker.Contracts.Services;
using Windows.UI.StartScreen;

namespace iPhoneRingsMaker.Services;

internal class JumpListService : IJumplistService
{
    public async Task InitializeAsync()
    {
        var jumplist = await JumpList.LoadCurrentAsync().AsTask().ConfigureAwait(false);
        jumplist.SystemGroupKind = JumpListSystemGroupKind.Recent;
    }
}
