using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace UI.Extensions
{
    internal static class SettlementNameplateBookIcon
    {
        internal const string Xml = "<Widget IsVisible=\"@IsInRange\"> <Children> <Widget WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"14\" SuggestedHeight=\"9\" PositionXOffset=\"-20\" PositionYOffset=\"@BookIconYOffset\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Center\" Sprite=\"lt_book_map_icon\" AlphaFactor=\"0.9\" IsEnabled=\"false\" IsVisible=\"@HasScholar\"/> </Children> </Widget>";

        internal static XmlDocument Create()
        {
            XmlDocument document = new();
            document.LoadXml(Xml);
            return document;
        }
    }

    [PrefabExtension("SettlementNameplateItemSmall", "descendant::ListPanel[@Id='EventsListPanel']")]
    internal class SettlementNameplateItemSmallExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;
        private readonly XmlDocument document = SettlementNameplateBookIcon.Create();

        [PrefabExtensionXmlDocument]
        public XmlDocument GetPrefabExtension() => document;
    }

    [PrefabExtension("SettlementNameplateItemMedium", "descendant::ListPanel[@Id='EventsListPanel']")]
    internal class SettlementNameplateItemMediumExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;
        private readonly XmlDocument document = SettlementNameplateBookIcon.Create();

        [PrefabExtensionXmlDocument]
        public XmlDocument GetPrefabExtension() => document;
    }

    [PrefabExtension("SettlementNameplateItemLarge", "descendant::ListPanel[@Id='EventsListPanel']")]
    internal class SettlementNameplateItemLargeExtension : PrefabExtensionInsertPatch
    {
        public override InsertType Type => InsertType.Append;
        private readonly XmlDocument document = SettlementNameplateBookIcon.Create();

        [PrefabExtensionXmlDocument]
        public XmlDocument GetPrefabExtension() => document;
    }
}