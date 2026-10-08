using System;
using iPhoneRingsMaker.Contracts.Services;

using Windows.UI.StartScreen;

namespace iPhoneRingsMaker.Services;

internal sealed class JumpListService : IJumplistService
{
    public async Task InitializeAsync()
    {
        if (!JumpList.IsSupported())
        {
            return;
        }

        var jumpList = await JumpList.LoadCurrentAsync();
        jumpList.SystemGroupKind = JumpListSystemGroupKind.Recent;
        await jumpList.SaveAsync();
    }
}
