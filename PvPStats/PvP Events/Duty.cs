using System;
using System.Collections.Generic;
using System.Linq;
using PvPStats.Enums;
using Lumina.Excel.GeneratedSheets;

namespace PvPStats.PvP_Events;

public class Duty
{
    public TerritoryType TerritoryType { get; }
    public DateTime      StartTime     { get; }
    public DateTime      EndTime       { get; set; }
    public TimeSpan      Duration      => EndTime == DateTime.MinValue ? DateTime.Now - StartTime : EndTime - StartTime;
    public List<Run>     RunList       { get; }
    public AllianceType  AllianceType  { get; }

    public Duty(TerritoryType territoryType, AllianceType allianceType)
    {
        TerritoryType = territoryType;
        StartTime     = DateTime.Now;
        EndTime       = DateTime.MinValue;
        RunList       = new List<Run>();
        AllianceType  = allianceType;
    }

    public int TotalDeaths => RunList.Sum(run => run.DeathList.Count);
    public int TotalWipes  => int.Max(RunList.Count - 1, 0);

    public List<Death> AllDeaths
    {
        get { return RunList.Aggregate(new List<Death>(), (x, y) => x.Concat(y.DeathList).ToList()); }
    }
}
