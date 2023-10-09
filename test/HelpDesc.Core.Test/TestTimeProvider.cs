using System;
using HelpDesc.Core.Service;

namespace HelpDesc.Core.Test;

public class TestTimeProvider : ITimeProvider
{
    private DateTime? now;

    public DateTime Now()
    {
        return now ?? DateTime.Now;
    }

    public void SetNow(DateTime? now)
    {
        this.now = now;
    }
}