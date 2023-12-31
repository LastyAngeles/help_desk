﻿using Orleans;

namespace HelpDesk.Api.Model;

[GenerateSerializer]
public record AgentEvent(string AgentId)
{
    public AgentEvent()
        : this(string.Empty)
    {
    }

    [Id(0)] public string AgentId { get; set; } = AgentId;
}

[GenerateSerializer]
public record AgentIsDisposing(string AgentId) : AgentEvent(AgentId)
{
    public AgentIsDisposing()
        : this(string.Empty)
    {
    }
}

[GenerateSerializer]
public record AgentStatusChanged(string AgentId, AgentStatus Status) : AgentEvent(AgentId)
{
    public AgentStatusChanged()
        : this(string.Empty, default)
    {
    }

    [Id(1)] public AgentStatus Status { get; set; } = Status;
}