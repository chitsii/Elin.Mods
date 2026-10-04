#if RUNTIME_TEST
using System;

public static class Pr8CreationOrder
{
    public static void CompleteBeforeSelection(Action create, Func<bool> ready, Action select)
    {
        create();
        if (!ready()) throw new InvalidOperationException("Native actor body/renderer creation incomplete; refusing PC selection.");
        select();
    }
}
#endif
