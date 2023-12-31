﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpDesk.Core.Test.Data;

public class TestingMockData
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    public static readonly int MaxMissingPolls = 3;
    public static readonly double MaxQueueCapacityMultiplier = 1.5;
    public static readonly int MaxConcurrency = 5;
    public static readonly TimeSpan SecondsBeforeSessionIsDead = PollInterval * (MaxMissingPolls + 1);

    public static List<SeniorityDescription> SeniorityDescriptions { get; set; } = new()
    {
        new SeniorityDescription { Name = JuniorSystemName, Capacity = 0.4, Priority = 1, Description = "Junior" },
        new SeniorityDescription { Name = MiddleSystemName, Capacity = 0.6, Priority = 2, Description = "Middle" },
        new SeniorityDescription { Name = SeniorSystemName, Capacity = 0.8, Priority = 3, Description = "Senior" },
        new SeniorityDescription { Name = TeamLeadSystemName, Capacity = 0.5, Priority = 4, Description = "Team-Lead" }
    };

    public const string JuniorSystemName = "jnr";
    public const string MiddleSystemName = "mdl";
    public const string SeniorSystemName = "snr";
    public const string TeamLeadSystemName = "tld";

    public static int JuniorCapacity =
        (int)Math.Floor(SeniorityDescriptions.First(x => x.Name == JuniorSystemName).Capacity * MaxConcurrency);

    public static int MiddleCapacity =
        (int)Math.Floor(SeniorityDescriptions.First(x => x.Name == MiddleSystemName).Capacity * MaxConcurrency);

    public static int SeniorCapacity =
        (int)Math.Floor(SeniorityDescriptions.First(x => x.Name == SeniorSystemName).Capacity * MaxConcurrency);

    public static int TeamLeadCapacity =
        (int)Math.Floor(SeniorityDescriptions.First(x => x.Name == TeamLeadSystemName).Capacity * MaxConcurrency);

    public static Dictionary<string, int> SeniorityToCapacity = new()
    {
        { JuniorSystemName, JuniorCapacity },
        { MiddleSystemName, MiddleCapacity },
        { SeniorSystemName, SeniorCapacity },
        { TeamLeadSystemName, TeamLeadCapacity },
    };

    public static List<Team> CoreTeams { get; set; } = new()
    {
        new Team
        {
            Name = "Team A",
            Stuff = new Dictionary<string, int>
            {
                { JuniorSystemName, 1 },
                { MiddleSystemName, 2 },
                { TeamLeadSystemName, 1 }
            },
            StartWork = TimeSpan.Parse("00:00:00"),
            EndWork = TimeSpan.Parse("08:00:00")
        },
        new Team
        {
            Name = "Team B",
            Stuff = new Dictionary<string, int>
            {
                { JuniorSystemName, 2 },
                { MiddleSystemName, 1 },
                { SeniorSystemName, 1 }
            },
            StartWork = TimeSpan.Parse("08:00:00"),
            EndWork = TimeSpan.Parse("16:00:00")
        },
        new Team
        {
            Name = "Team C",
            Stuff = new Dictionary<string, int>
            {
                { MiddleSystemName, 2 }
            },
            StartWork = TimeSpan.Parse("08:00:00"),
            EndWork = TimeSpan.Parse("24:00:00")
        }
    };

    public static Team OverflowTeam { get; set; } =
        new()
        {
            Name = "Overflow",
            Stuff = new Dictionary<string, int>
            {
                { JuniorSystemName, 6 }
            },
            StartWork = TimeSpan.Parse("00:00:00"),
            EndWork = TimeSpan.Parse("24:00:00")
        };
}