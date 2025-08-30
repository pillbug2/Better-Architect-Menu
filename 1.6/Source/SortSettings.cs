using Verse;

namespace BetterArchitect
{
    public enum SortBy { Default, Label, Beauty, Comfort, Value, WorkToBuild, Health, Cleanliness, SkillRequired, Flammability, CoverEffectiveness, MaxPowerOutput, PowerConsumption, RecreationPower, MoveSpeed, TotalStorageCapacity, DoorOpeningSpeed, WorkSpeedFactor }

    public class SortSettings : IExposable
    {
        public SortBy SortBy = SortBy.Default; public bool Ascending = true;
        public void ExposeData() { Scribe_Values.Look(ref SortBy, "sortBy", SortBy.Default); Scribe_Values.Look(ref Ascending, "ascending", true); }
    }
}
