// Focused calendar/save and regional resolution checks; no full World scene or graphical performance claims.
using Godot;
using System;
using System.IO;
using System.Text.Json;
public partial class ValidateSeasonalClimate : Node
{
    // Exercise the shared clock signal, pause/scrub semantics, year wrap, save values and climate boundary math.
    // =========================================================
    public override void _Ready()
    {
        try
        {
            var seasons=new SeasonController(); seasons.Initialize(new SeasonMasterConfig { Automatic=true,YearLengthDays=120,StartingDay=119 }); AddChild(seasons);
            var clock=new GameClock { Config=new ClockMasterConfig { StartHour=23,CycleSeconds=600,Paused=false } }; AddChild(clock); clock.SetProcess(false); seasons.BindClock(clock);
            clock._Process(600); Check(Math.Abs(seasons.AbsoluteDay-120)<0.0001 && seasons.Year==2,"Calendar year did not wrap");
            clock.SetHour(12); Check(seasons.AbsoluteDay==120,"Hour scrub advanced calendar");
            clock.Config.Paused=true; clock._Process(600); Check(seasons.AbsoluteDay==120,"Paused clock advanced calendar");
            seasons.SetYearPosition(0.5f); Check(seasons.ManualOverride && seasons.YearPosition==0.5f,"Manual override failed");
            var saved=JsonSerializer.Deserialize<SavedCalendar>(JsonSerializer.Serialize(seasons.Capture())); saved.Validate();
            seasons.ClearOverride(); seasons.SetCalendarDay(55); seasons.Restore(saved);
            Check(seasons.AbsoluteDay==120 && !seasons.ManualOverride,"Calendar restore did not clear session preview");
            var config=new RegionalClimateMasterConfig(); config.Validate();
            var north=RegionalClimateMath.Resolve(new(-30,60,WorldClimateZone.Cold,0),0.5f,config);
            Check(north.PermanentCover==1 && north.SnowFraction==1,"Far north lost permanent summer pack");
            var temperateWinter=RegionalClimateMath.Resolve(new(8,65,WorldClimateZone.Temperate,0),0,config);
            var temperateSummer=RegionalClimateMath.Resolve(new(8,65,WorldClimateZone.Temperate,0),0.5f,config);
            Check(temperateWinter.SnowFraction>0.99f && temperateSummer.RainFraction>0.99f,"Temperate seasonal phase wrong");
            var south=RegionalClimateMath.Resolve(new(35,20,WorldClimateZone.Hot,0),0,config);
            Check(south.SnowFraction==0 && south.PermanentCover==0,"Hot south gained ordinary winter snow");
            var boundary=RegionalClimateMath.Resolve(new(12,50,WorldClimateZone.Temperate,0),0,config);
            Check(boundary.SnowFraction>0 && boundary.SnowFraction<1,"Precipitation boundary is abrupt");
            var disabled=RegionalClimateMath.Resolve(new(-30,60,WorldClimateZone.Cold,0),0,config,new BiomeWeatherResponse { AllowSnow=false });
            Check(disabled.SnowFraction==0 && disabled.CoverMultiplier==0,"Biome snow response ignored");
            var pack=new SavedRegionalClimate(30,1,true,false,new[] { new SavedSnowCell(0,0.5f,20) });
            JsonSerializer.Deserialize<SavedRegionalClimate>(JsonSerializer.Serialize(pack)).Validate(100);
            bool rejected=false;
            try { (pack with { Cells=new[] { new SavedSnowCell(0,0.5f,40) } }).Validate(100); } catch(InvalidDataException) { rejected=true; }
            Check(rejected,"Future snow timestamp accepted");
            GD.Print("SE4 BC3 CLIMATE PASS"); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // Fail with a concrete diagnostic; never label partial fixture execution a pass.
    // =========================================================
    private static void Check(bool value,string message) { if(!value) throw new Exception(message); }
}
