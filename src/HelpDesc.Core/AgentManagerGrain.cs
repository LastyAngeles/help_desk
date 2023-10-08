﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelpDesc.Api;
using HelpDesc.Api.Model;
using HelpDesc.Core.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace HelpDesc.Core;

public class AgentManagerGrain : Grain, IAgentManagerGrain
{
    private readonly ILogger<AgentManagerGrain> logger;
    private readonly TeamsConfig teamsConfig;

    //priority => agent
    private Dictionary<int, List<Agent>> AgentPool { get; } = new();

    //priority => last allocated id (for round robin)
    private Dictionary<int, string> PriorityRoundRobinMap { get; } = new();

    private double maxQueueCapacity;
    private double maxQueueCapacityMultiplier;

    public AgentManagerGrain(IOptions<TeamsConfig> teamConfigOptions,
        ILogger<AgentManagerGrain> logger)
    {
        this.logger = logger;
        teamsConfig = teamConfigOptions?.Value;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var currentTime = DateTime.Now.TimeOfDay;

        var currentTeam = AllocateCurrentTeam();
        if (currentTeam == null)
            return;

        var seniorityDescriptions = teamsConfig.SeniorityDescriptions;
        var stuff = currentTeam.Stuff;

        for (var i = 0; i < stuff.Count; i++)
        {
            var (senioritySystemName, membersCount) = stuff.ElementAt(i);

            var seniorityDescription = seniorityDescriptions.FirstOrDefault(x => x.Name == senioritySystemName);
            if (seniorityDescription == null)
            {
                logger.LogError(
                    "Team config was wrongly populated, since it has no matching seniority description for given system name: {SystemName}." +
                    "Priority can not be set correctly." +
                    "Agents for this entry would not be created.",
                    senioritySystemName);
                continue;
            }

            for (var j = 0; j < membersCount; j++)
            {
                var agentId = $"{currentTeam.Name}.{senioritySystemName}.{j}";
                var agentGrain = GrainFactory.GetGrain<AgentGrain>(agentId);
                var agentStatus = await agentGrain.GetStatus();

                var ret = AgentPool.GetOrAdd(seniorityDescription.Priority,
                    _ => new List<Agent>());
                ret.Add(new Agent(agentId, senioritySystemName, seniorityDescription.Priority, agentStatus));
                maxQueueCapacity++;
            }
        }

        maxQueueCapacityMultiplier = teamsConfig.MaximumQueueCapacityMultiplier;
        maxQueueCapacity *= maxQueueCapacityMultiplier;

        await base.OnActivateAsync(cancellationToken);

        Team AllocateCurrentTeam()
        {
            if (teamsConfig == null)
            {
                logger.LogError(
                    "Error in a process of agent manager initialization - team raster is empty. Grain id: {Id}",
                    this.GetPrimaryKeyString());
                return null;
            }

            var availableTeamsBasedOnShift = teamsConfig.CoreTeams
                .Where(x => SolutionHelper.IsTimeInRange(currentTime, x.StartWork, x.EndWork)).ToList();
            var chosenTeam = availableTeamsBasedOnShift.FirstOrDefault();

            if (chosenTeam == null)
            {
                var overallTimeFrames = teamsConfig.CoreTeams.Select(x => (x.Name, x.StartWork, x.EndWork));
                logger.LogError(
                    "Error in a process of agent manager initialization - there is not matching team based on current time frame." +
                    "Current time: {CurrentTime}; Overall time frames: {OverallTimeFrames}", currentTime,
                    overallTimeFrames);
                return null;
            }

            if (availableTeamsBasedOnShift.Count > 1)
            {
                var overlappedTeams = availableTeamsBasedOnShift.Where(x => x.Name != chosenTeam.Name);
                logger.LogWarning(
                    "Overlapping team hours detected. Current team would be chosen randomly. Chosen team: {ChosenTeam}; Overlapped teams: {OverlappedTeams}",
                    chosenTeam, overlappedTeams);
            }

            return chosenTeam;
        }
    }

    public async Task<Agent> AssignAgent(string sessionId)
    {
        foreach (var priority in AgentPool.Keys)
        {
            var availableAgents = AgentPool[priority].Where(x => x.Availability == Status.Free).ToList();

            if (!availableAgents.Any())
                continue;

            PriorityRoundRobinMap.TryGetValue(priority, out var lastAllocatedAgentId);

            var assignedAgent = availableAgents.Count == 1
                ? availableAgents.Single()
                : availableAgents.FirstOrDefault(x => x.Id != lastAllocatedAgentId) ?? availableAgents.First();

            // TODO: handle overload case (impossible state, but it needs to be respected) (Maxim Meshkov 2023-10-08)
            var updatedAgentStatus = await GrainFactory.GetGrain<AgentGrain>(assignedAgent.Id).AssignSession(sessionId);

            assignedAgent.Availability = updatedAgentStatus;

            PriorityRoundRobinMap[priority] = assignedAgent.Id;

            return assignedAgent;
        }

        //no available agents found
        return null;
    }

    public Task<double> GetMaxQueueCapacity() => Task.FromResult(maxQueueCapacity);

    public Task ChangeAgentStatus(string agentId, Status status)
    {
        var agent = AgentPool.FirstOrDefault(x => x.Value.Any(y => y.Id == agentId))
            .Value
            .FirstOrDefault(x => x.Id == agentId);

        if (agent == null)
        {
            logger.LogWarning(
                "Can not update agent status, because requested id can not be found. Requested id: {AgentId}", agentId);
            return Task.CompletedTask;
        }

        agent.Availability = status;
        return Task.CompletedTask;
    }
}