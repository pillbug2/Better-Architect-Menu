using System.Collections.Generic;
using Verse;

namespace BetterArchitect
{
    public class DesignatorCategoryData
    {
        public readonly DesignationCategoryDef def;
        public readonly bool isMainCategory;
        public readonly List<Designator> allDesignators;
        public readonly List<Designator> buildables;
        public readonly List<Designator> orders;

        public DesignatorCategoryData(DesignationCategoryDef def, bool isMainCategory, List<Designator> allDesignators, List<Designator> buildables, List<Designator> orders)
        {
            this.def = def;
            this.isMainCategory = isMainCategory;
            this.allDesignators = allDesignators;
            this.buildables = buildables;
            this.orders = orders;
        }
    }
}
