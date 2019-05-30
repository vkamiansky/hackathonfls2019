using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AliceInventory.Logic.AliceResponseRender
{
    public enum ResponseFormat
    {
        GreetingRequested,
        Declined,
        Added,
        AddCanceled,
        Deleted,
        DeleteCanceled,
        ClearRequested,
        Cleared,
        ListRead,
        EmptyListRead,
        MailSent,
        HelpRequested,
        Error,
        ExitRequested,
    }
}