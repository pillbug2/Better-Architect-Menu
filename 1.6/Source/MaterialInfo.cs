using UnityEngine;
using Verse;

namespace BetterArchitect
{
    public class MaterialInfo
    {
        public readonly string label; public readonly Texture2D icon; public readonly Color color; public readonly ThingDef def;
        public MaterialInfo(string l, Texture2D i, Color c, ThingDef d) { label = l; icon = i; color = c; def = d; }
        public override bool Equals(object obj) => obj is MaterialInfo info && def == info.def;
        public override int GetHashCode() => def != null ? def.GetHashCode() : label.GetHashCode();
    }
}
