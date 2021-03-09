using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using UnityEngine;
using LCS.Engine.Data;

namespace LCS.Engine
{
    public class GameData
    {
        public static string XMLPath;
        public static string MapPath;
        public static string GraphicsPath;
        public static string SavePath;

        public Dictionary<string, AttributeDef> attributeList;
        public Dictionary<string, SkillDef> skillList;
        public Dictionary<string, OrganDef> organList;
        public Dictionary<string, BodyPartDef> bodyPartList;
        public Dictionary<string, CreatureDef> creatureDefList;
        public Dictionary<string, List<string>> nameLists;
        public Dictionary<string, SpeciesDef> speciesList;
        public Dictionary<string, LawDef> lawList;
        public Dictionary<string, CrimeDef> crimeList;
        public Dictionary<string, string> globalVarsList;
        public Dictionary<string, ItemDef> itemList;
        public Dictionary<string, AttackDef> attackList;
        public Dictionary<string, NationDef> nationList;
        public Dictionary<string, ViewDef> viewList;
        public Dictionary<string, LocationDef> locationList;
        public Dictionary<string, XmlDocument> mapList;
        public Dictionary<string, List<string>> executionTypeList;
        public Dictionary<string, Sprite> itemGraphicList;
        public Dictionary<string, NewsActionDef> newsActionList;
        public Dictionary<string, NewsTypeDef> newsTypeList;
        public Dictionary<string, LocationGenDef> locationGenList;
        public Dictionary<string, PortraitPartDef> portraitPartList;
        public Dictionary<string, Sprite> portraitGraphicList;

        public Dictionary<string, string> translationList;

        private static GameData data = null;

        private GameData()
        {
            XMLPath = Application.streamingAssetsPath + "/XML";
            MapPath = Application.streamingAssetsPath + "/Maps";
            GraphicsPath = Application.streamingAssetsPath + "/Graphics";
            SavePath = Application.persistentDataPath;

            attributeList = new Dictionary<string, AttributeDef>();
            skillList = new Dictionary<string, SkillDef>();
            organList = new Dictionary<string, OrganDef>();
            bodyPartList = new Dictionary<string, BodyPartDef>();
            creatureDefList = new Dictionary<string, CreatureDef>();
            nameLists = new Dictionary<string, List<string>>();
            lawList = new Dictionary<string, LawDef>();
            crimeList = new Dictionary<string, CrimeDef>();
            viewList = new Dictionary<string, ViewDef>();
            globalVarsList = new Dictionary<string, string>();
            itemList = new Dictionary<string, ItemDef>();
            speciesList = new Dictionary<string, SpeciesDef>();
            attackList = new Dictionary<string, AttackDef>();
            nationList = new Dictionary<string, NationDef>();
            locationList = new Dictionary<string, LocationDef>();
            mapList = new Dictionary<string, XmlDocument>();
            executionTypeList = new Dictionary<string, List<string>>();
            itemGraphicList = new Dictionary<string, Sprite>();
            newsActionList = new Dictionary<string, NewsActionDef>();
            newsTypeList = new Dictionary<string, NewsTypeDef>();
            locationGenList = new Dictionary<string, LocationGenDef>();
            portraitPartList = new Dictionary<string, PortraitPartDef>();
            portraitGraphicList = new Dictionary<string, Sprite>();
            translationList = new Dictionary<string, string>();

            populateDictionaries();
        }

        public static GameData getData()
        {
            if(data == null)
            {
                data = new GameData();
            }

            return data;
        }

        public void loadDefinitions()
        {
            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            loadNameLists();
            loadMapList();
            loadGraphicList();

            loadAttributeList();
            loadSkillList();
            loadOrganList();
            loadBodyPartList();
            loadAttackList();
            loadItemList();
            loadSpeciesList();
            loadLawList();
            loadCrimeList();
            loadViewList();
            loadGlobalVarsList();
            loadExecutionList();
            loadCreatureDefs();
            loadLocationDefs();
            loadNationList();
            loadNewsDefs();
            loadLocationGenDefs();
            loadPortraitDefs();
            loadTranslations();
        }

        private XmlDocument livingDoc;
        private XmlDocument saveDoc;
        public void saveGame()
        {
            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (livingDoc == null)
            {
                livingDoc = new XmlDocument();
                XmlNode root = livingDoc.CreateElement("game");
                livingDoc.AppendChild(root);
            }
            
            MasterController.GetMC().addDebugMessage("Saving Game");
            MasterController.GetMC().save(livingDoc);

            saveDoc = (XmlDocument)livingDoc.CloneNode(true);
        }

        public void loadGame()
        {
            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            XmlReader reader = XmlReader.Create(SavePath + "/Save.sav");
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            reader.Close();

            MasterController.GetMC().load(doc);
        }

        public void saveToDisk()
        {
            if (saveDoc == null) return;

            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            XmlWriter writer = XmlWriter.Create(SavePath + "/Save.sav", writerSettings);
            saveDoc.Save(writer);
            writer.Close();
        }

        public void saveHighScore(string fate, string slogan, Components.World.HighScore score)
        {
            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            XmlDocument hsDoc = new XmlDocument();
            if(File.Exists(SavePath + "/Score.sav"))
            {
                XmlReader reader = XmlReader.Create(SavePath + "/Score.sav");
                hsDoc.Load(reader);
                reader.Close();
            }
            else
            {
                hsDoc.AppendChild(hsDoc.CreateElement("root"));                
            }

            XmlNode scoreNode = hsDoc.CreateElement("score");
            XmlNode sloganNode = hsDoc.CreateElement("slogan");
            sloganNode.InnerText = slogan;
            scoreNode.AppendChild(sloganNode);
            XmlNode fateNode = hsDoc.CreateElement("fate");
            fateNode.InnerText = fate;
            scoreNode.AppendChild(fateNode);
            XmlNode recruitNode = hsDoc.CreateElement("recruits");
            recruitNode.InnerText = "" + score.recruits;
            scoreNode.AppendChild(recruitNode);
            XmlNode martyrNode = hsDoc.CreateElement("martyrs");
            martyrNode.InnerText = "" + score.martyrs;
            scoreNode.AppendChild(martyrNode);
            XmlNode killNode = hsDoc.CreateElement("kills");
            killNode.InnerText = "" + score.kills;
            scoreNode.AppendChild(killNode);
            XmlNode kidnapNode = hsDoc.CreateElement("kidnappings");
            kidnapNode.InnerText = "" + score.kidnappings;
            scoreNode.AppendChild(kidnapNode);
            XmlNode taxNode = hsDoc.CreateElement("moneyTaxed");
            taxNode.InnerText = "" + score.moneyTaxed;
            scoreNode.AppendChild(taxNode);
            XmlNode spendNode = hsDoc.CreateElement("moneySpent");
            spendNode.InnerText = "" + score.moneySpent;
            scoreNode.AppendChild(spendNode);
            XmlNode flagBuyNode = hsDoc.CreateElement("flagsBought");
            flagBuyNode.InnerText = "" + score.flagsBought;
            scoreNode.AppendChild(flagBuyNode);
            XmlNode flagBurnNode = hsDoc.CreateElement("flagsBurned");
            flagBurnNode.InnerText = "" + score.flagsBurned;
            scoreNode.AppendChild(flagBurnNode);

            hsDoc.DocumentElement.InsertBefore(scoreNode, hsDoc.DocumentElement.FirstChild);

            XmlWriterSettings writerSettings = new XmlWriterSettings();
            writerSettings.Indent = true;
            XmlWriter writer = XmlWriter.Create(SavePath + "/Score.sav", writerSettings);
            hsDoc.Save(writer);
            writer.Close();
        }

        public XmlDocument loadHighScores()
        {
            //Set culture invariant to not cause problems with float/double parsing
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            XmlDocument hsDoc = new XmlDocument();
            if (File.Exists(SavePath + "/Score.sav"))
            {
                XmlReader reader = XmlReader.Create(SavePath + "/Score.sav");
                hsDoc.Load(reader);
                reader.Close();
            }

            return hsDoc;
        }

        public void clearSave()
        {
            File.Delete(SavePath + "/Save.sav");
            livingDoc = null;
            saveDoc = null;
        }

        //Fill the data dictionaries with stubs so that cross-references can be set up as specific definitions are being loaded in
        private void populateDictionaries()
        {
            try {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BaseDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Attributes/Skills");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;
                //Attributes
                foreach (XmlNode node in root.SelectSingleNode("creatureAttributes").ChildNodes)
                {
                    AttributeDef def = new AttributeDef();
                    def.type = node.Attributes["idname"].Value;
                    attributeList.Add(def.type, def);
                }

                //Skills
                foreach (XmlNode node in root.SelectSingleNode("skills").ChildNodes)
                {
                    SkillDef def = new SkillDef();
                    def.type = node.Attributes["idname"].Value;                    
                    skillList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading basedefs: BaseDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BodyDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading body defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Organs
                foreach (XmlNode node in root.SelectSingleNode("organs").ChildNodes)
                {
                    OrganDef def = new OrganDef();
                    def.type = node.Attributes["idname"].Value;
                    organList.Add(def.type, def);
                }

                //Body Parts
                foreach (XmlNode node in root.SelectSingleNode("bodyParts").ChildNodes)
                {
                    BodyPartDef def = new BodyPartDef();
                    def.type = node.Attributes["idname"].Value;
                    bodyPartList.Add(def.type, def);
                }

                //Species
                foreach (XmlNode node in root.SelectSingleNode("species").ChildNodes)
                {
                    SpeciesDef species = new SpeciesDef();
                    species.type = node.Attributes["idname"].Value;
                    speciesList.Add(species.type, species);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading bodydefs: BodyDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/AttackDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Attacks");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Attacks
                foreach (XmlNode node in root.ChildNodes)
                {
                    AttackDef def = new AttackDef();
                    def.type = node.Attributes["idname"].Value;
                    attackList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading attackdefs: AttackDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/ItemDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Items");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Items
                foreach (XmlNode node in root.ChildNodes)
                {
                    ItemDef def = new ItemDef();
                    def.type = node.Attributes["idname"].Value;
                    itemList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading itemdefs: ItemDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/CreatureDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Creature Defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Creatures
                foreach (XmlNode node in root.ChildNodes)
                {
                    CreatureDef creature = new CreatureDef();
                    creature.type = node.Attributes["idname"].Value;
                    creatureDefList.Add(creature.type, creature);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading creaturedefs: CreatureDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LawDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Laws/Views/Crimes");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Laws
                foreach (XmlNode node in root.SelectSingleNode("laws").ChildNodes)
                {
                    LawDef def = new LawDef();
                    def.type = node.Attributes["idname"].Value;
                    lawList.Add(def.type, def);
                }

                //Views
                foreach (XmlNode node in root.SelectSingleNode("views").ChildNodes)
                {
                    ViewDef def = new ViewDef();
                    def.type = node.Attributes["idname"].Value;
                    viewList.Add(def.type, def);
                }

                //Crimes
                foreach (XmlNode node in root.SelectSingleNode("crimes").ChildNodes)
                {
                    CrimeDef def = new CrimeDef();
                    def.type = node.Attributes["idname"].Value;
                    crimeList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading lawdefs: LawDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/NationDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Nations");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Nations
                foreach (XmlNode node in root.ChildNodes)
                {
                    NationDef def = new NationDef();
                    def.type = node.Attributes["idname"].Value;
                    nationList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading nations: NationDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LocationDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Locations");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Locations
                foreach (XmlNode node in root.ChildNodes)
                {
                    LocationDef def = new LocationDef();
                    def.type = node.Attributes["idname"].Value;
                    locationList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading locations: LocationDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/NewsDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading News");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //News Actions
                foreach (XmlNode node in root.SelectSingleNode("siteactions").ChildNodes)
                {
                    NewsActionDef def = new NewsActionDef();
                    def.type = node.Attributes["idname"].Value;
                    newsActionList.Add(def.type, def);
                }

                //News Types
                foreach (XmlNode node in root.SelectSingleNode("storytypes").ChildNodes)
                {
                    NewsTypeDef def = new NewsTypeDef();
                    def.type = node.Attributes["idname"].Value;
                    newsTypeList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading newsdefs: NewsDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LocationGenDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Location Gen Defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Location Gen defs
                foreach (XmlNode node in root.ChildNodes)
                {
                    LocationGenDef def = new LocationGenDef();
                    def.type = node.Attributes["idname"].Value;
                    locationGenList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading locationGenDefs: LocationGenDefs.xml not found");
            }

            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/PortraitDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Pre-loading Portrait Defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                //Location Gen defs
                foreach (XmlNode node in root.ChildNodes)
                {
                    PortraitPartDef def = new PortraitPartDef();
                    def.type = node.Attributes["idname"].Value;
                    portraitPartList.Add(def.type, def);
                }

                reader.Close();
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading PortraitDefs: PortraitDefs.xml not found");
            }
        }

        private bool loadAttributeList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BaseDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Attributes");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("creatureAttributes").ChildNodes)
                {
                    AttributeDef def = attributeList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading attributes: BaseDefs.xml not found");
                return false;
            }
        }

        private bool loadSkillList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BaseDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Skills");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("skills").ChildNodes)
                {
                    SkillDef def = skillList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading skill: BaseDefs.xml not found");
                return false;
            }
        }

        private bool loadOrganList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BodyDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Organs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("organs").ChildNodes)
                {
                    OrganDef def = organList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading organs: BodyDefs.xml not found");
                return false;
            }
        }

        private bool loadBodyPartList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BodyDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Body Parts");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("bodyParts").ChildNodes)
                {
                    BodyPartDef def = bodyPartList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading body parts: BodyDefs.xml not found");
                return false;
            }
        }

        private bool loadSpeciesList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BodyDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Species");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("species").ChildNodes)
                {
                    SpeciesDef species = speciesList[node.Attributes["idname"].Value];
                    species.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading species: BodyDefs.xml not found");
                return false;
            }
        }

        private bool loadAttackList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/AttackDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Attacks");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    AttackDef def = attackList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading attackdefs: AttackDefs.xml not found");
                return false;
            }
        }

        private bool loadItemList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/ItemDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Items");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    ItemDef def = itemList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading itemdefs: ItemDefs.xml not found");
                return false;
            }
        }

        private bool loadCreatureDefs()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/CreatureDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Creature Defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    CreatureDef creature = creatureDefList[node.Attributes["idname"].Value];
                    creature.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading creaturedefs: CreatureDefs.xml not found");
                return false;
            }
        }

        private bool loadNameLists()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/NameList.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Names");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                XmlNode firstNames = root.SelectSingleNode("first_names");

                List<string> maleFirstNames = new List<string>();
                foreach (XmlNode name in firstNames.SelectSingleNode("male").ChildNodes)
                {
                    maleFirstNames.Add(name.InnerText);
                }
                nameLists.Add("first_name_male", maleFirstNames);

                List<string> femaleFirstNames = new List<string>();
                foreach (XmlNode name in firstNames.SelectSingleNode("female").ChildNodes)
                {
                    femaleFirstNames.Add(name.InnerText);
                }
                nameLists.Add("first_name_female", femaleFirstNames);

                List<string> neutralFirstNames = new List<string>();
                foreach (XmlNode name in firstNames.SelectSingleNode("neutral").ChildNodes)
                {
                    neutralFirstNames.Add(name.InnerText);
                }
                nameLists.Add("first_name_neutral", neutralFirstNames);

                List<string> patriarchFirstNames = new List<string>();
                foreach (XmlNode name in firstNames.SelectSingleNode("patriarch").ChildNodes)
                {
                    patriarchFirstNames.Add(name.InnerText);
                }
                nameLists.Add("first_name_patriarch", patriarchFirstNames);

                XmlNode surnames = root.SelectSingleNode("surnames");

                List<string> regularSurnames = new List<string>();
                foreach (XmlNode name in surnames.SelectSingleNode("regular").ChildNodes)
                {
                    regularSurnames.Add(name.InnerText);
                }
                nameLists.Add("surname_regular", regularSurnames);

                List<string> archconservativeSurnames = new List<string>();
                foreach (XmlNode name in surnames.SelectSingleNode("archconservative").ChildNodes)
                {
                    archconservativeSurnames.Add(name.InnerText);
                }
                nameLists.Add("surname_archconservative", archconservativeSurnames);

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading name list: NameList.xml not found");
                return false;
            }
        }

        private bool loadLawList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LawDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Laws");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach(XmlNode node in root.SelectSingleNode("laws").ChildNodes)
                {
                    LawDef def = lawList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading laws: LawDefs.xml not found");
                return false;
            }
        }

        private bool loadViewList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LawDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Views");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("views").ChildNodes)
                {
                    ViewDef def = viewList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading views: LawDefs.xml not found");
                return false;
            }
        }

        private bool loadCrimeList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LawDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Crimes");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("crimes").ChildNodes)
                {
                    CrimeDef def = crimeList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading crimes: LawDefs.xml not found");
                return false;
            }
        }

        private bool loadExecutionList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LawDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Execution Strings");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("executionmethods").ChildNodes)
                {
                    List<string> strings = new List<string>();

                    foreach(XmlNode innerNode in node.ChildNodes)
                    {
                        strings.Add(innerNode.InnerText);
                    }

                    executionTypeList.Add(node.Name, strings);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading execution strings: LawDefs.xml not found");
                return false;
            }
        }

        private bool loadNationList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/NationDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Nations");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    NationDef def = nationList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading nations: NationDefs.xml not found");
                return false;
            }
        }

        private bool loadGlobalVarsList()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/BaseDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Globals");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("vars").ChildNodes)
                {
                    globalVarsList.Add(node.Name, node.InnerText);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading globals: BaseDefs.xml not found");
                return false;
            }
        }

        private bool loadLocationDefs()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LocationDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Locations");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    LocationDef def = locationList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading locations: LocationDefs.xml not found");
                return false;
            }
        }

        private bool loadNewsDefs()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/NewsDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading News");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.SelectSingleNode("siteactions").ChildNodes)
                {
                    NewsActionDef def = newsActionList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                foreach (XmlNode node in root.SelectSingleNode("storytypes").ChildNodes)
                {
                    NewsTypeDef def = newsTypeList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading newsdefs: NewsDefs.xml not found");
                return false;
            }
        }

        private bool loadGraphicList()
        {
            //Load item icons
            foreach(string fullpath in Directory.GetFiles(GraphicsPath + "/Items", "*.png"))
            {
                string filename = Path.GetFileName(fullpath);

                Texture2D imageTexture = new Texture2D(1, 1, TextureFormat.RGBA32,false);
                imageTexture.LoadImage(File.ReadAllBytes(fullpath));
                imageTexture.filterMode = FilterMode.Point;
                Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), Vector2.zero);

                itemGraphicList[filename] = sprite;
            }

            foreach (string fullpath in Directory.GetFiles(GraphicsPath + "/Portraits", "*.png"))
            {
                string filename = Path.GetFileName(fullpath);

                Texture2D imageTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                imageTexture.LoadImage(File.ReadAllBytes(fullpath));
                imageTexture.filterMode = FilterMode.Point;
                Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), new Vector2(0.5f, 0.5f));

                portraitGraphicList[filename] = sprite;
            }

            return true;
        }

        private bool loadMapList()
        {
            foreach (string filename in Directory.GetFiles(MapPath, "*.tmx"))
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(filename, readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Map " + Path.GetFileName(filename));
                doc.Load(reader);

                mapList.Add(Path.GetFileName(filename), doc);
                reader.Close();
            }

            return true;
        }

        private bool loadLocationGenDefs()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/LocationGenDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Location Gen Defs");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    LocationGenDef def = locationGenList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading locationGenDefs: LocationGenDefs.xml not found");
                return false;
            }
        }

        private bool loadPortraitDefs()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/PortraitDefs.xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Portrait Parts");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    PortraitPartDef def = portraitPartList[node.Attributes["idname"].Value];
                    def.parseData(node);
                }

                reader.Close();
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading portraitdefs: PortraitDefs.xml not found");
                return false;
            }
        }

        private bool loadTranslations()
        {
            try
            {
                XmlReaderSettings readerSettings = new XmlReaderSettings();
                readerSettings.IgnoreComments = true;
                XmlReader reader = XmlReader.Create(XMLPath + "/Translation/" + globalVarsList["LANGUAGE"] + ".xml", readerSettings);

                XmlDocument doc = new XmlDocument();
                MasterController.GetMC().addDebugMessage("Loading Translations");
                doc.Load(reader);

                XmlNode root = doc.DocumentElement;

                foreach (XmlNode node in root.ChildNodes)
                {
                    translationList.Add(node.Attributes["key"].Value, node.Attributes["value"].Value);
                }

                reader.Close();
                return true;
            }
            catch (FileNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading translations: " + globalVarsList["LANGUAGE"] + ".xml not found");
                return false;
            }
            catch (KeyNotFoundException)
            {
                MasterController.GetMC().addErrorMessage("Error loading translations: LANGUAGE global variable not defined in BaseDefs.xml");
                return false;
            }
        }
    }
}
