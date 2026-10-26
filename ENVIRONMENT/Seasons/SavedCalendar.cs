// Detached calendar values; validation is safe on SAVE worker threads and never accesses scene objects.
using System;
using System.IO;
public sealed record SavedCalendar(double Day,int YearLength,bool Automatic,bool ManualOverride,float Position,bool FoliageEnabled,float Strength,float Variation)
{
    // Reject invalid or contradictory calendar fields before applying a checkpoint.
    // =========================================================
    public void Validate()
    {
        if(!double.IsFinite(Day) || Day<0 || Day>100000000 || YearLength<30 || YearLength>730
            || !float.IsFinite(Position) || Position<0 || Position>1 || !float.IsFinite(Strength) || Strength<0 || Strength>1
            || !float.IsFinite(Variation) || Variation<0 || Variation>45)
            throw new InvalidDataException("Invalid saved calendar.");
    }
}
