using System;
using System.Collections.Generic;
using System.Xml;

namespace LCS.Engine.Data
{
    public class PortraitPartDef : DataDef
    {
        public class PortraitAccessoryDef
        {
            public PortraitAccessoryDef()
            {
                extraFlags = new List<string>();
                imageVariations = new List<ImageSet>();
            }

            public string type;
            public int weight = 20;
            public List<ImageSet> imageVariations;
            public PortraitPartFlags flags = 0;
            public List<string> extraFlags;
        }

        public enum PortraitPartClass
        {
            EYE,
            NOSE,
            MOUTH,
            EYEBROW,
            HAIR,
            JAW,
            EAR,
            HAIR_FACIAL,
            FACE_ACCESSORY,
            NECK_ACCESSORY,
            SCAR
        }

        [Flags]
        public enum PortraitPartFlags
        {
            NONE = 0,
            MASCULINE = 1,
            FEMININE = 2,
            LIBERAL_ONLY = 4,
            CONSERVATIVE_ONLY = 8,
            NON_LIBERAL = 16,
            NON_CONSERVATIVE = 32,
            YOUNG_ONLY = 64,
            OLD_ONLY = 128,
            NOT_YOUNG = 256,
            NOT_OLD = 512,
            PRISON = 1024,
            DYE = 2048
        }

        public class ImageSet
        {
            public UnityEngine.Sprite image;
            public UnityEngine.Sprite image2 = null;
            public UnityEngine.Sprite mask = null;
            public UnityEngine.Sprite mask2 = null;
        }

        public PortraitPartDef()
        {
            accessories = new Dictionary<string, PortraitAccessoryDef>();
            extraFlags = new List<string>();
            imageVariations = new List<ImageSet>();
        }
        
        public int weight = 20;
        public List<ImageSet> imageVariations;
        public PortraitPartClass partClass;
        public PortraitPartFlags flags = 0;
        public List<string> extraFlags;
        public Dictionary<string, PortraitAccessoryDef> accessories;

        public override void parseData(XmlNode node)
        {
            partClass = (PortraitPartClass)Enum.Parse(typeof(PortraitPartClass), node.SelectSingleNode("class").InnerText);

            foreach (XmlNode imageNode in node.SelectNodes("imageset"))
            {
                ImageSet images = new ImageSet();

                images.image = GameData.getData().portraitGraphicList[imageNode.SelectSingleNode("image").InnerText];
                if (imageNode.SelectSingleNode("image2") != null)
                    images.image2 = GameData.getData().portraitGraphicList[imageNode.SelectSingleNode("image2").InnerText];
                if (imageNode.SelectSingleNode("mask") != null)
                    images.mask = GameData.getData().portraitGraphicList[imageNode.SelectSingleNode("mask").InnerText];
                if (imageNode.SelectSingleNode("mask2") != null)
                    images.mask2 = GameData.getData().portraitGraphicList[imageNode.SelectSingleNode("mask2").InnerText];

                imageVariations.Add(images);
            }

            if (node.SelectSingleNode("weight") != null)
                weight = int.Parse(node.SelectSingleNode("weight").InnerText);

            if (node.SelectSingleNode("flags") != null)
            {
                foreach (XmlNode flagNode in node.SelectSingleNode("flags").ChildNodes)
                {
                    flags |= (PortraitPartFlags)Enum.Parse(typeof(PortraitPartFlags), flagNode.InnerText);
                }
            }

            if (node.SelectSingleNode("extra_flags") != null)
            {
                foreach (XmlNode flagNode in node.SelectSingleNode("extra_flags").ChildNodes)
                {
                    extraFlags.Add(flagNode.InnerText);
                }
            }

            if (node.SelectSingleNode("accessories") != null)
            {
                foreach (XmlNode accessoryNode in node.SelectSingleNode("accessories").ChildNodes)
                {
                    PortraitAccessoryDef aDef = new PortraitAccessoryDef();
                    aDef.type = accessoryNode.Attributes["idname"].Value;

                    foreach (XmlNode accessoryImageNode in accessoryNode.SelectNodes("imageset"))
                    {
                        ImageSet images = new ImageSet();

                        images.image = GameData.getData().portraitGraphicList[accessoryImageNode.SelectSingleNode("image").InnerText];
                        if (accessoryImageNode.SelectSingleNode("image2") != null)
                            images.image2 = GameData.getData().portraitGraphicList[accessoryImageNode.SelectSingleNode("image2").InnerText];
                        if (accessoryImageNode.SelectSingleNode("mask") != null)
                            images.mask = GameData.getData().portraitGraphicList[accessoryImageNode.SelectSingleNode("mask").InnerText];
                        if (accessoryImageNode.SelectSingleNode("mask2") != null)
                            images.mask2 = GameData.getData().portraitGraphicList[accessoryImageNode.SelectSingleNode("mask2").InnerText];

                        aDef.imageVariations.Add(images);
                    }

                    if (accessoryNode.SelectSingleNode("weight") != null)
                        aDef.weight = int.Parse(accessoryNode.SelectSingleNode("weight").InnerText);

                    if (accessoryNode.SelectSingleNode("flags") != null)
                    {
                        foreach (XmlNode flagNode in accessoryNode.SelectSingleNode("flags").ChildNodes)
                        {
                            aDef.flags |= (PortraitPartFlags)Enum.Parse(typeof(PortraitPartFlags), flagNode.InnerText);
                        }
                    }

                    if (accessoryNode.SelectSingleNode("extra_flags") != null)
                    {
                        foreach (XmlNode flagNode in accessoryNode.SelectSingleNode("extra_flags").ChildNodes)
                        {
                            aDef.extraFlags.Add(flagNode.InnerText);
                        }
                    }

                    accessories.Add(aDef.type, aDef);
                }
            }
        }
    }
}
