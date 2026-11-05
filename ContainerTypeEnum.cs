using System;
using System.Reflection;

namespace ExpandableChests
{
    public enum ContainerType
    {
        [ContainerTypeAttr(2, "Wood:10")] piece_chest_wood,
        [ContainerTypeAttr(3, "FineWood:10,Iron:2")] piece_chest,
        [ContainerTypeAttr(4, "Wood:10,Tar:2,BlackMetal:6")] piece_chest_blackmetal,
        [ContainerTypeAttr(6, "Grausten:10,TrophyCharredMelee:5,FlametalNew:2")] piece_chest_grausten,
        [ContainerTypeAttr(10, "Frostwood:10,Tar:2,BlackMetal:6")] piece_chest_warderobe,
        [ContainerTypeAttr(0, null)] Other
    }

    class ContainerTypeAttr: Attribute
    {
        internal ContainerTypeAttr(int defaultRows, string originalCost)
        {
            this.defaultRows = defaultRows;
            this.originalCost = originalCost;
        }

        public int defaultRows { get; private set; }
        public string originalCost { get; private set; }
    }

    public static class ContainerTypeFields
    {
        public static int GetDefaultRows(this ContainerType p)
        {
            ContainerTypeAttr attr = GetAttr(p);
            return attr.defaultRows;
        }
        
        public static string GetOriginalCost(this ContainerType p)
        {
            ContainerTypeAttr attr = GetAttr(p);
            return attr.originalCost;
        }
        
        private static ContainerTypeAttr GetAttr(ContainerType p)
        {
            return (ContainerTypeAttr)Attribute.GetCustomAttribute(ForValue(p), typeof(ContainerTypeAttr));
        }

        private static MemberInfo ForValue(ContainerType c)
        {
            return typeof(ContainerType).GetField(Enum.GetName(typeof(ContainerType), c));
        }
    }
}