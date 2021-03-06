using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using LCS.Engine.Data;

namespace LCS.Engine.Components.Creature
{
    public class Portrait : Component
    {
        private const int OFFSET_PIXEL_RANGE = 10;

        public static List<Color> skinTones = new List<Color>();
        public static List<Color> eyeColors = new List<Color>();
        public static List<Color> hairColors = new List<Color>();
        public static List<Color> dyedHairColors = new List<Color>();

        public Coordinate eyeOffset;
        public Coordinate noseOffset;
        public Coordinate mouthOffset;
        public Coordinate jawOffset;

        public Color eyeColor;
        public Color hairColor;
        public Color hairDyeColor;
        public Color skinColor;

        [SimpleSave]
        public string faceAcc;
        [SimpleSave]
        public int faceAccImageNum;
        [SimpleSave]
        public string eye;
        [SimpleSave]
        public int eyeImageNum;
        [SimpleSave]
        public string eyeAcc;
        [SimpleSave]
        public int eyeAccImageNum;
        [SimpleSave]
        public string eyebrow;
        [SimpleSave]
        public int eyebrowImageNum;
        [SimpleSave]
        public string eyebrowAcc;
        [SimpleSave]
        public int eyebrowAccImageNum;
        [SimpleSave]
        public string nose;
        [SimpleSave]
        public int noseImageNum;
        [SimpleSave]
        public string noseAcc;
        [SimpleSave]
        public int noseAccImageNum;
        [SimpleSave]
        public string mouth;
        [SimpleSave]
        public int mouthImageNum;
        [SimpleSave]
        public string mouthAcc;
        [SimpleSave]
        public int mouthAccImageNum;
        [SimpleSave]
        public string hair;
        [SimpleSave]
        public int hairImageNum;
        [SimpleSave]
        public string hairAcc;
        [SimpleSave]
        public int hairAccImageNum;
        [SimpleSave]
        public string jaw;
        [SimpleSave]
        public int jawImageNum;
        [SimpleSave]
        public string ear;
        [SimpleSave]
        public int earImageNum;
        [SimpleSave]
        public string earAcc;
        [SimpleSave]
        public int earAccImageNum;
        [SimpleSave]
        public string hairFacial;
        [SimpleSave]
        public int hairFacialImageNum;
        [SimpleSave]
        public string neckAcc;
        [SimpleSave]
        public int neckAccImageNum;
        [SimpleSave]
        public string scar = "";
        [SimpleSave]
        public int scarLevel;
        [SimpleSave]
        public int freshScar;
        [SimpleSave]
        public string fixedPortrait = "";

        public bool forceRegen = false;

        public override void save(XmlNode entityNode)
        {
            if (saveNode == null)
            {
                saveNode = entityNode.OwnerDocument.CreateElement("Portrait");
                entityNode.AppendChild(saveNode);
            }

            if (saveNode.ParentNode != entityNode)
                entityNode.AppendChild(saveNode);

            saveField(eyeOffset, "eyeOffset", saveNode);
            saveField(noseOffset, "noseOffset", saveNode);
            saveField(mouthOffset, "mouthOffset", saveNode);
            saveField(jawOffset, "jawOffset", saveNode);
            saveField(eyeColor, "eyeColor", saveNode);
            saveField(skinColor, "skinColor", saveNode);
            saveField(hairColor, "hairColor", saveNode);
            saveField(hairDyeColor, "hairDyeColor", saveNode);
            
            saveSimpleFields();
        }

        public override void load(XmlNode componentData, Dictionary<long, Entity> entityList)
        {
            //If this is a fixedPortrait entity all of these fields will be null
            if (componentData.SelectSingleNode("fixedPortrait").InnerText == "")
            {
                string[] eyeOffsetString = componentData.SelectSingleNode("eyeOffset").InnerText.Split(',');
                string[] noseOffsetString = componentData.SelectSingleNode("noseOffset").InnerText.Split(',');
                string[] mouthOffsetString = componentData.SelectSingleNode("mouthOffset").InnerText.Split(',');
                string[] jawOffsetString = componentData.SelectSingleNode("jawOffset").InnerText.Split(',');

                string[] eyeColorString = componentData.SelectSingleNode("eyeColor").InnerText.Split(',');
                string[] hairColorString = componentData.SelectSingleNode("hairColor").InnerText.Split(',');
                string[] hairDyeColorString;
                if (componentData.SelectSingleNode("hairDyeColor") != null)
                    hairDyeColorString = componentData.SelectSingleNode("hairDyeColor").InnerText.Split(',');
                else
                {
                    MasterController.GetMC().addDebugMessage("Hair Dye missing from creature " + owner.def + ", set randomly");
                    Color tempColor = dyedHairColors[MasterController.GetMC().LCSRandom(dyedHairColors.Count)];
                    hairDyeColorString = new string[] { tempColor.r + "", tempColor.g + "", tempColor.b + "", tempColor.a + "" };
                }
                string[] skinColorString = componentData.SelectSingleNode("skinColor").InnerText.Split(',');

                eyeOffset = new Coordinate(int.Parse(eyeOffsetString[0]), int.Parse(eyeOffsetString[1]));
                noseOffset = new Coordinate(int.Parse(noseOffsetString[0]), int.Parse(noseOffsetString[1]));
                mouthOffset = new Coordinate(int.Parse(mouthOffsetString[0]), int.Parse(mouthOffsetString[1]));
                jawOffset = new Coordinate(int.Parse(jawOffsetString[0]), int.Parse(jawOffsetString[1]));

                eyeColor = new Color(byte.Parse(eyeColorString[0]), byte.Parse(eyeColorString[1]), byte.Parse(eyeColorString[2]), byte.Parse(eyeColorString[3]));
                hairColor = new Color(byte.Parse(hairColorString[0]), byte.Parse(hairColorString[1]), byte.Parse(hairColorString[2]), byte.Parse(hairColorString[3]));
                skinColor = new Color(byte.Parse(skinColorString[0]), byte.Parse(skinColorString[1]), byte.Parse(skinColorString[2]), byte.Parse(skinColorString[3]));
                hairDyeColor = new Color(byte.Parse(hairDyeColorString[0]), byte.Parse(hairDyeColorString[1]), byte.Parse(hairDyeColorString[2]), byte.Parse(hairDyeColorString[3]));
            }
            loadSimpleFields(componentData, entityList);
        }

        public override void subscribe()
        {
            base.subscribe();
            MasterController.GetMC().nextDay += doDaily;
        }

        public override void unsubscribe()
        {
            base.unsubscribe();
            MasterController.GetMC().nextDay -= doDaily;
        }

        private void doDaily(object sender, EventArgs args)
        {
            if (freshScar > 0)
            {
                if(MasterController.GetMC().LCSRandom(3) == 0)
                    freshScar--;
                forceRegen = true;
            }
        }

        public void makeMyFace()
        {
            MasterController mc = MasterController.GetMC();

            GameData data = GameData.getData();

            if (data.speciesList[data.creatureDefList[owner.def].species].image[0] != "GEN")
            {
                fixedPortrait = data.speciesList[data.creatureDefList[owner.def].species].image[MasterController.GetMC().LCSRandom(data.speciesList[data.creatureDefList[owner.def].species].image.Count)];
                return;
            }

            if (getComponent<CreatureInfo>().genderConservative == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH)
                skinColor = skinTones[0];
            else
                skinColor = skinTones[mc.LCSRandom(skinTones.Count)];
            
            eyeColor = eyeColors[mc.LCSRandom(eyeColors.Count)];
            hairColor = hairColors[mc.LCSRandom(hairColors.Count)];

            int oldage = getComponent<Body>().getSpecies().oldage;
            int nearOldAge = oldage - (oldage / 6);

            //If they're really old, "age up" their hair color a bit
            if (getComponent<Age>().getAge() > nearOldAge)
            {
                for(int i=nearOldAge; i< getComponent<Age>().getAge(); i++)
                {
                    float yearMod = (oldage / 20f);
                    if (MasterController.GetMC().LCSRandom((int)(yearMod)) == 0)
                    {
                        hairColor = getComponent<Portrait>().hairColor.lerp(new Color(255, 255, 255), 0.2f);
                    }
                }
            }
            hairDyeColor = dyedHairColors[mc.LCSRandom(dyedHairColors.Count)];

            eyeOffset = new Coordinate(mc.LCSRandom(OFFSET_PIXEL_RANGE) - (OFFSET_PIXEL_RANGE/2), mc.LCSRandom(OFFSET_PIXEL_RANGE) - (OFFSET_PIXEL_RANGE / 2));
            noseOffset = new Coordinate(0, mc.LCSRandom(OFFSET_PIXEL_RANGE) - (OFFSET_PIXEL_RANGE / 2));
            mouthOffset = new Coordinate(0, mc.LCSRandom(OFFSET_PIXEL_RANGE) - (OFFSET_PIXEL_RANGE / 2));
            jawOffset = new Coordinate(0, mc.LCSRandom(OFFSET_PIXEL_RANGE/3) - (OFFSET_PIXEL_RANGE/3));

            Dictionary<string, int> validEyes = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validEyeAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validEyebrows = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validEyebrowAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validNoses = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validNoseAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validMouths = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validMouthAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validHair = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validHairAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validJaws = new Dictionary<string, int>();
            Dictionary<string, int> validFaceAcc = new Dictionary<string, int>();
            Dictionary<string, int> validEars = new Dictionary<string, int>();
            Dictionary<string, Dictionary<string, int>> validEarAcc = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, int> validFacialHair = new Dictionary<string, int>();
            Dictionary<string, int> validNeckAcc = new Dictionary<string, int>();
            List<string> scars = new List<string>();

            foreach (PortraitPartDef def in GameData.getData().portraitPartList.Values)
            {
                //If this is a face or neck accessory with the "PRISON" flag, it qualifies as valid on prisoners even if it will fail later down the line
                if(getComponent<CreatureInfo>().encounterName == "Prisoner" &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.PRISON) != 0)
                {
                    if(def.partClass == PortraitPartDef.PortraitPartClass.FACE_ACCESSORY)
                    {
                        validFaceAcc.Add(def.type, def.weight);
                        continue;
                    }

                    if(def.partClass == PortraitPartDef.PortraitPartClass.NECK_ACCESSORY)
                    {
                        validNeckAcc.Add(def.type, def.weight);
                        continue;
                    }
                }

                //Check flags to see if the entity qualifies for this part
                if (getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.MASCULINE) != 0)
                    continue;

                if ((getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.MALE ||
                    getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH) &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.FEMININE) != 0)
                    continue;

                if (def.partClass == PortraitPartDef.PortraitPartClass.HAIR_FACIAL &&
                    (getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE ||
                    getComponent<Age>().getAge() < (int)(oldage * (16f / Age.OLDAGE_HUMAN))))
                    continue;

                if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.NON_LIBERAL) != 0)
                    continue;

                if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.NON_CONSERVATIVE) != 0)
                    continue;

                if (getComponent<CreatureInfo>().alignment != Alignment.LIBERAL &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.LIBERAL_ONLY) != 0)
                    continue;

                if (getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.CONSERVATIVE_ONLY) != 0)
                    continue;
                
                if (getComponent<Age>().getAge() > (int)(oldage * (30f / Age.OLDAGE_HUMAN)) &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.YOUNG_ONLY) != 0)
                    continue;

                if (getComponent<Age>().getAge() < (int)(oldage * (50f / Age.OLDAGE_HUMAN)) &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.OLD_ONLY) != 0)
                    continue;

                if (getComponent<Age>().getAge() >= (int)(oldage * (50f / Age.OLDAGE_HUMAN)) &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.NOT_OLD) != 0)
                    continue;

                if (getComponent<Age>().getAge() <= (int)(oldage * (30f / Age.OLDAGE_HUMAN)) &&
                    (def.flags & PortraitPartDef.PortraitPartFlags.NOT_YOUNG) != 0)
                    continue;

                if (!extraflagcheck(def.extraFlags)) continue;

                //Load part into the appropriate list if it wasn't disqualified
                switch (def.partClass)
                {
                    case PortraitPartDef.PortraitPartClass.EYE:
                        validEyes.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.EYEBROW:
                        validEyebrows.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.NOSE:
                        validNoses.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.MOUTH:
                        validMouths.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.HAIR:
                        validHair.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.JAW:
                        validJaws.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.EAR:
                        validEars.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.HAIR_FACIAL:
                        validFacialHair.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.FACE_ACCESSORY:
                        validFaceAcc.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.NECK_ACCESSORY:
                        validNeckAcc.Add(def.type, def.weight); break;
                    case PortraitPartDef.PortraitPartClass.SCAR:
                        scars.Add(def.type); break;
                }

                Dictionary<string, int> validAccList = new Dictionary<string, int>();

                foreach (PortraitPartDef.PortraitAccessoryDef accDef in def.accessories.Values)
                {
                    if (checkAccessory(accDef))
                    {
                        validAccList.Add(accDef.type, accDef.weight);
                    }
                }

                switch (def.partClass)
                {
                    case PortraitPartDef.PortraitPartClass.EYE:
                        validEyeAcc.Add(def.type, validAccList); break;
                    case PortraitPartDef.PortraitPartClass.EYEBROW:
                        validEyebrowAcc.Add(def.type, validAccList); break;
                    case PortraitPartDef.PortraitPartClass.NOSE:
                        validNoseAcc.Add(def.type, validAccList); break;
                    case PortraitPartDef.PortraitPartClass.MOUTH:
                        validMouthAcc.Add(def.type, validAccList); break;
                    case PortraitPartDef.PortraitPartClass.HAIR:
                        validHairAcc.Add(def.type, validAccList); break;
                    case PortraitPartDef.PortraitPartClass.EAR:
                        validEarAcc.Add(def.type, validAccList); break;
                }
            }

            eye = mc.WeightedRandom(validEyes);
            eyeImageNum = mc.LCSRandom(GameData.getData().portraitPartList[eye].imageVariations.Count);

            eyebrow = mc.WeightedRandom(validEyebrows);
            eyebrowImageNum = mc.LCSRandom(GameData.getData().portraitPartList[eyebrow].imageVariations.Count);

            nose = mc.WeightedRandom(validNoses);
            noseImageNum = mc.LCSRandom(GameData.getData().portraitPartList[nose].imageVariations.Count);

            mouth = mc.WeightedRandom(validMouths);
            mouthImageNum = mc.LCSRandom(GameData.getData().portraitPartList[mouth].imageVariations.Count);

            hair = mc.WeightedRandom(validHair);
            hairImageNum = mc.LCSRandom(GameData.getData().portraitPartList[hair].imageVariations.Count);

            jaw = mc.WeightedRandom(validJaws);
            jawImageNum = mc.LCSRandom(GameData.getData().portraitPartList[jaw].imageVariations.Count);

            ear = mc.WeightedRandom(validEars);
            earImageNum = mc.LCSRandom(GameData.getData().portraitPartList[ear].imageVariations.Count);

            if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_FACIAL_HAIR")) && validFacialHair.Count > 0)
            {
                hairFacial = mc.WeightedRandom(validFacialHair);
                hairFacialImageNum = mc.LCSRandom(GameData.getData().portraitPartList[hairFacial].imageVariations.Count);
            }
            else
                hairFacial = "";

            faceAcc = "";
            neckAcc = "";
            earAcc = "";
            noseAcc = "";
            eyeAcc = "";
            eyebrowAcc = "";
            mouthAcc = "";
            hairAcc = "";

            int accessorycount = 0;
            int loopCount = 0;

            do
            {
                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_FACE_ACC")) && validFaceAcc.Count > 0 && faceAcc == "")
                {
                    faceAcc = mc.WeightedRandom(validFaceAcc);
                    faceAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[faceAcc].imageVariations.Count);
                    accessorycount++;
                    foreach(string s in GameData.getData().portraitPartList[faceAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_NECK_ACC")) && validNeckAcc.Count > 0 && neckAcc == "")
                {
                    neckAcc = mc.WeightedRandom(validNeckAcc);
                    neckAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[neckAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[neckAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_EAR_ACC")) && validEarAcc[ear].Count > 0)
                {
                    earAcc = mc.WeightedRandom(validEarAcc[ear]);
                    earAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[ear].accessories[earAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[ear].accessories[earAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_NOSE_ACC")) && validNoseAcc[nose].Count > 0)
                {
                    noseAcc = mc.WeightedRandom(validNoseAcc[nose]);
                    noseAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[nose].accessories[noseAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[nose].accessories[noseAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_EYE_ACC")) && validEyeAcc[eye].Count > 0)
                {
                    eyeAcc = mc.WeightedRandom(validEyeAcc[eye]);
                    eyeAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[eye].accessories[eyeAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[eye].accessories[eyeAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_EYEBROW_ACC")) && validEyebrowAcc[eyebrow].Count > 0)
                {
                    eyebrowAcc = mc.WeightedRandom(validEyebrowAcc[eyebrow]);
                    eyebrowAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[eyebrow].accessories[eyebrowAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[eyebrow].accessories[eyebrowAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_MOUTH_ACC")) && validMouthAcc[mouth].Count > 0)
                {
                    mouthAcc = mc.WeightedRandom(validMouthAcc[mouth]);
                    mouthAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[mouth].accessories[mouthAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[mouth].accessories[mouthAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if ((mc.LCSRandom(4) == 0 || getPortraitFlags().Contains("FORCE_HAIR_ACC")) && validHairAcc[hair].Count > 0)
                {
                    hairAcc = mc.WeightedRandom(validHairAcc[hair]);
                    hairAccImageNum = mc.LCSRandom(GameData.getData().portraitPartList[hair].accessories[hairAcc].imageVariations.Count);
                    accessorycount++;
                    foreach (string s in GameData.getData().portraitPartList[hair].accessories[hairAcc].extraFlags)
                    {
                        if (!s.StartsWith("~")) continue;
                        string itemname = s.TrimStart('~');
                        validEyeAcc[eye].Remove(itemname);
                        validEyebrowAcc[eyebrow].Remove(itemname);
                        validNoseAcc[nose].Remove(itemname);
                        validMouthAcc[mouth].Remove(itemname);
                        validHairAcc[hair].Remove(itemname);
                        validFaceAcc.Remove(itemname);
                        validEarAcc[ear].Remove(itemname);
                        validNeckAcc.Remove(itemname);
                    }
                }

                if((validFaceAcc.Count == 0 || faceAcc != "") &&
                    (validNeckAcc.Count == 0 || neckAcc != "") &&
                    (validEarAcc.Count == 0 || earAcc != "" ) &&
                    (validNoseAcc.Count == 0 || noseAcc != "") &&
                    (validEyeAcc.Count == 0 || eyeAcc != "") &&
                    (validEyebrowAcc.Count == 0 || eyebrowAcc != "") &&
                    (validMouthAcc.Count == 0 || mouthAcc != "") &&
                    (validHairAcc.Count == 0 || hairAcc != ""))
                {
                    break;
                }

                //Set a hard limit on the number of times this will loop just so it doesn't get stuck
                loopCount++;

            } while (accessorycount < GameData.getData().creatureDefList[owner.def].min_accessories && loopCount < 100);

            int scarCount = mc.LCSRandom(GameData.getData().creatureDefList[owner.def].scars);

            if (mc.LCSRandom(4) == 0 && scarCount > 0)
            {
                scar = scars[mc.LCSRandom(scars.Count)];
                scarLevel = scarCount - 1;
                if (scarLevel >= GameData.getData().portraitPartList[scar].imageVariations.Count)
                    scarLevel = GameData.getData().portraitPartList[scar].imageVariations.Count - 1;
            }
            else
                scar = "";
        }

        public void scarMe()
        {
            if (scar == "")
            {
                List<string> scars = new List<string>();

                foreach (PortraitPartDef def in GameData.getData().portraitPartList.Values)
                {
                    if (def.partClass == PortraitPartDef.PortraitPartClass.SCAR) scars.Add(def.type);
                }

                scar = scars[MasterController.GetMC().LCSRandom(scars.Count)];

                forceRegen = true;
                freshScar = 5;
            }
            else
            {
                if (scarLevel < GameData.getData().portraitPartList[scar].imageVariations.Count - 1)
                    scarLevel++;
                forceRegen = true;
                freshScar = 5;
            }
        }

        private bool checkAccessory(PortraitPartDef.PortraitAccessoryDef def)
        {
            int oldage = getComponent<Body>().getSpecies().oldage;

            //If this person is a prisoner then this accessory automatically qualifies if it's a prison accessory, even if it wouldn't otherwise
            if (getComponent<CreatureInfo>().encounterName == "Prisoner" &&
                (def.flags & PortraitPartDef.PortraitPartFlags.PRISON) != 0)
                return true;

            //Check flags to see if the entity qualifies for this part
            if (getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.FEMALE &&
                (def.flags & PortraitPartDef.PortraitPartFlags.MASCULINE) != 0)
                return false;

            if ((getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.MALE ||
                getComponent<CreatureInfo>().genderLiberal == CreatureInfo.CreatureGender.WHITEMALEPATRIARCH) &&
                (def.flags & PortraitPartDef.PortraitPartFlags.FEMININE) != 0)
                return false;

            if (getComponent<CreatureInfo>().alignment == Alignment.LIBERAL &&
                (def.flags & PortraitPartDef.PortraitPartFlags.NON_LIBERAL) != 0)
                return false;

            if (getComponent<CreatureInfo>().alignment == Alignment.CONSERVATIVE &&
                (def.flags & PortraitPartDef.PortraitPartFlags.NON_CONSERVATIVE) != 0)
                return false;

            if (getComponent<CreatureInfo>().alignment != Alignment.LIBERAL &&
                (def.flags & PortraitPartDef.PortraitPartFlags.LIBERAL_ONLY) != 0)
                return false;

            if (getComponent<CreatureInfo>().alignment != Alignment.CONSERVATIVE &&
                (def.flags & PortraitPartDef.PortraitPartFlags.CONSERVATIVE_ONLY) != 0)
                return false;

            if (getComponent<Age>().getAge() > (int)(oldage * (30f / Age.OLDAGE_HUMAN)) &&
                (def.flags & PortraitPartDef.PortraitPartFlags.YOUNG_ONLY) != 0)
                return false;

            if (getComponent<Age>().getAge() < (int)(oldage * (50f / Age.OLDAGE_HUMAN)) &&
                (def.flags & PortraitPartDef.PortraitPartFlags.OLD_ONLY) != 0)
                return false;

            if (getComponent<Age>().getAge() >= (int)(oldage * (50f / Age.OLDAGE_HUMAN)) &&
                (def.flags & PortraitPartDef.PortraitPartFlags.NOT_OLD) != 0)
                return false;

            if (getComponent<Age>().getAge() <= (int)(oldage * (30f / Age.OLDAGE_HUMAN)) &&
                (def.flags & PortraitPartDef.PortraitPartFlags.NOT_YOUNG) != 0)
                return false;

            return extraflagcheck(def.extraFlags);
        }

        private bool extraflagcheck(List<string> extraFlags)
        {
            bool extraflagcheck = false;

            if (extraFlags.Count == 0) extraflagcheck = true;
            else
            {
                bool negativeMatch = false;
                List<string> extraFlagList = new List<string>(extraFlags);
                extraFlagList.RemoveAll(s => s.StartsWith("~"));

                foreach (string s in extraFlags)
                {
                    if (!s.StartsWith("!")) continue;

                    extraFlagList.Remove(s);

                    if (getPortraitFlags().Contains(s.TrimStart('!')))
                    {
                        negativeMatch = true;
                        break;
                    }
                }

                if (!negativeMatch)
                {
                    if (extraFlagList.Count == 0) extraflagcheck = true;
                    else
                    {
                        foreach (string s in extraFlagList)
                        {
                            if (s.StartsWith("~")) continue;

                            if (getPortraitFlags().Contains(s))
                            {
                                extraflagcheck = true;
                                break;
                            }
                        }
                    }
                }
            }

            return extraflagcheck;
        }

        public Portrait copy()
        {
            Portrait p = new Portrait();

            if (fixedPortrait == "")
            {
                p.eyeOffset = new Coordinate(eyeOffset.x, eyeOffset.y);
                p.noseOffset = new Coordinate(noseOffset.x, noseOffset.y);
                p.mouthOffset = new Coordinate(mouthOffset.x, mouthOffset.y);
                p.jawOffset = new Coordinate(jawOffset.x, jawOffset.y);

                p.eyeColor = new Color(eyeColor.r, eyeColor.g, eyeColor.b, eyeColor.a);
                p.hairColor = new Color(hairColor.r, hairColor.g, hairColor.b, hairColor.a);
                p.skinColor = new Color(skinColor.r, skinColor.g, skinColor.b, skinColor.a);
                p.hairDyeColor = new Color(hairDyeColor.r, hairDyeColor.g, hairDyeColor.b, hairDyeColor.a);

                p.faceAcc = faceAcc;
                p.faceAccImageNum = faceAccImageNum;
                p.eye = eye;
                p.eyeImageNum = eyeImageNum;
                p.eyeAcc = eyeAcc;
                p.eyeAccImageNum = eyeAccImageNum;
                p.eyebrow = eyebrow;
                p.eyebrowImageNum = eyebrowImageNum;
                p.eyebrowAcc = eyebrowAcc;
                p.eyebrowAccImageNum = eyebrowImageNum;
                p.nose = nose;
                p.noseImageNum = noseImageNum;
                p.noseAcc = noseAcc;
                p.noseAccImageNum = noseAccImageNum;
                p.mouth = mouth;
                p.mouthImageNum = mouthImageNum;
                p.mouthAcc = mouthAcc;
                p.mouthAccImageNum = mouthAccImageNum;
                p.hair = hair;
                p.hairImageNum = hairImageNum;
                p.hairAcc = hairAcc;
                p.hairAccImageNum = hairAccImageNum;
                p.jaw = jaw;
                p.jawImageNum = jawImageNum;
                p.ear = ear;
                p.earImageNum = earImageNum;
                p.earAcc = earAcc;
                p.earAccImageNum = earImageNum;
                p.hairFacial = hairFacial;
                p.hairFacialImageNum = hairFacialImageNum;
                p.neckAcc = neckAcc;
                p.neckAccImageNum = neckAccImageNum;
                p.scar = scar;
                p.scarLevel = scarLevel;
                p.freshScar = freshScar;
            }
            else
            {
                p.fixedPortrait = fixedPortrait;
            }

            return p;
        }

        public List<string> getPortraitFlags()
        {
            return GameData.getData().creatureDefList[owner.def].portraitFlags;
        }

        public class Coordinate
        {
            public int x;
            public int y;

            public Coordinate(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public override string ToString()
            {
                return x + "," + y;
            }
        }

        public class Color
        {
            public byte r;
            public byte g;
            public byte b;
            public byte a;

            public Color(byte r, byte g, byte b, byte a = 255)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }

            public override string ToString()
            {
                return r + "," + g + "," + b + "," + a;
            }

            public Color lerp(Color towards, float value)
            {
                //Don't lerp with invalid values, it will just cause problems
                if (value > 1 || value < 0) return this;
                
                byte newR = (byte)((r * (1 - value)) + (towards.r * value));
                byte newG = (byte)((g * (1 - value)) + (towards.g * value));
                byte newB = (byte)((b * (1 - value)) + (towards.b * value));
                byte newA = (byte)((a * (1 - value)) + (towards.a * value));

                return new Color(newR, newG, newB, newA);
            }
        }
    }
}
