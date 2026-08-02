using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace iPhoneRingsMaker.Helpers;

internal static class DebugExceptionExtensions
{
    public static void RethrowIfDebuggerAttached(this Exception exception)
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
#endif
    }
}
