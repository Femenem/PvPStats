using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using PvPStats.Enums;
using PvPStats.Extensions;
using PvPStats.Services;
using Lumina.Excel.GeneratedSheets;

namespace PvPStats.PvP_Events;

public class PvPManager : IDisposable
{
    private readonly ChatGui _chatGui;
    private readonly PvPEventService _pvpEventService;
    private readonly PlayerCharacterState _playerCharacterState;

    private Duty? _currentDuty;
    private Run? _currentRun;

    public bool DutyActive { get; private set; }
    public List<Duty> DutyList { get; private set; }
    public bool AnyDutiesStarted { get; private set; }

    public Duty? GetMostRecentDuty()
    {
        if (_currentDuty is not null)
            return _currentDuty;
        if (DutyList.Count > 0)
            return DutyList[^1];
        return null;
    }

    public Run? GetMostRecentRun()
    {
        if (_currentRun is not null)
            return _currentRun;
        var runList = GetMostRecentDuty()?.RunList;
        return runList?.Count > 0 ? runList[^1] : null;
    }

    private readonly Configuration _configuration;

    public PvPManager(Configuration configuration)
    {
        _chatGui = Service.ChatGui;
        _pvpEventService = Service.PvPEventService;
        _playerCharacterState = Service.PlayerCharacterState;
        _configuration = configuration;
        DutyActive = false;
        DutyList = new List<Duty>();
        _currentDuty = null;
        _currentRun = null;

        _playerCharacterState.OnPlayerDeath += AddDeath;
        _pvpEventService.DutyStarted += StartDuty;
        _pvpEventService.DutyWiped += EndRun;
        _pvpEventService.DutyRecommenced += StartNewRun;
        _pvpEventService.DutyEnded += EndDuty;
    }

    public void Dispose()
    {
        _playerCharacterState.OnPlayerDeath -= AddDeath;
        _pvpEventService.DutyStarted -= StartDuty;
        _pvpEventService.DutyWiped -= EndRun;
        _pvpEventService.DutyRecommenced -= StartNewRun;
        _pvpEventService.DutyEnded -= EndDuty;
    }

    private void StartDuty(PvPStartedEventArgs eventArgs)
    {
        DutyActive = true;
        AnyDutiesStarted = true;
        _currentDuty = new Duty(eventArgs.TerritoryType, eventArgs.IntendedUse.GetAllianceType());

        StartNewRun();
    }

    private void EndDuty(PvPEndedEventArgs eventArgs)
    {
        DutyActive = false;
        if (_currentDuty is null) return;

        _currentDuty.EndTime = DateTime.Now;

        if (eventArgs.Completed)
        {
            EndRun();
        }
        else
        {
            if (_currentRun is not null)
            {
                _currentDuty.RunList.Remove(_currentRun);
                _currentRun = null;
            }
        }

        var dutyDuration = _currentDuty.EndTime - _currentDuty.StartTime;

        _chatGui.Print(InfoMessage("Time in Duty: ", $"{dutyDuration.MinutesAndSeconds()}"));
        if (_currentDuty.RunList.Count > 1 || !_configuration.SuppressEmptyValues)
        {
            var finalRun = _currentDuty.RunList[^1];
            var finalRunDuration = finalRun.EndTime - finalRun.StartTime;

            _chatGui.Print(InfoMessage("Final Run Duration: ", $"{finalRunDuration.MinutesAndSeconds()}"));
            _chatGui.Print(InfoMessage("Wipes: ", $"{_currentDuty.TotalWipes}"));
        }

        var deathList = _currentDuty.AllDeaths;
        switch (_currentDuty.AllianceType)
        {
            case AllianceType.ThreeParty:
                foreach (Alliance alliance in Enum.GetValues(typeof(Alliance)))
                {
                    if (alliance is Alliance.D or Alliance.E or Alliance.F or Alliance.None)
                        continue;
                    var count = deathList.Count(x => x.Alliance == alliance);
                    _chatGui.Print(InfoMessage($"{alliance} deaths: ", count.ToString(), count == 0));
                }

                break;
            case AllianceType.SixParty:
                foreach (Alliance alliance in Enum.GetValues(typeof(Alliance)))
                {
                    if (alliance is Alliance.None)
                        continue;
                    var count = deathList.Count(x => x.Alliance == alliance);
                    _chatGui.Print(InfoMessage($"{alliance} deaths: ", count.ToString(), count == 0));
                }

                break;
            default:
                var totalDeaths = deathList.Count;
                if (totalDeaths > 0 || !_configuration.SuppressEmptyValues)
                    _chatGui.Print(InfoMessage("Party Deaths: ", $"{totalDeaths}"));
                break;
        }

        DutyList.Add(_currentDuty);
    }

    private void AddDeath(PlayerDeathEventArgs eventArgs)
    {
        _currentRun?.DeathList.Add(new Death(eventArgs.ObjectId,
                                             eventArgs.PlayerName,
                                             DateTime.Now, eventArgs.Alliance));
    }

    private void EndRun() { _currentRun!.EndTime = DateTime.Now; }

    private void StartNewRun()
    {
        _currentRun = new Run();
        _currentDuty!.RunList.Add(_currentRun);
    }

    private SeString InfoMessage(string label, string info, bool highlightInfo = false)
    {
        var seStringBuilder = new SeStringBuilder();

        if (_configuration.IncludeDutyTrackerLabel)
        {
            seStringBuilder.AddUiForeground("[PvPTracker] ", 35).AddUiForegroundOff();
        }

        seStringBuilder.AddUiForeground(label, 62).AddUiForegroundOff();

        if (highlightInfo)
            seStringBuilder.AddUiGlow(info, 45).AddUiGlowOff();
        else
            seStringBuilder.AddUiForeground(info, 45).AddUiForegroundOff();

        return seStringBuilder.Build();
    }
}
