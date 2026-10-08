using System.Runtime.InteropServices;
using System.Text;

using Windows.Win32;

namespace iPhoneRingsMaker.Helpers;

public class RuntimeHelper
{
    public static bool IsMSIX
    {
        get
        {
            uint length = 0;

            return (uint)PInvoke.GetCurrentPackageFullName(ref length, null) != 15700U;
        }
    }
}
