using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LCS.Engine.UI.UIEvents;

namespace LCS.Engine.UI
{
    public interface UIController
    {
        Squad squadUI { get; set; }
        Squad enemyUI { get; set; }
        TitlePage titlePage { get; set; }
        BaseMode baseMode { get; set; }
        CharInfo charInfo { get; set; }
        OrganizationManagement organizationManagement { get; set; }
        SafeHouseManagement safeHouseManagement { get; set; }
        SiteMode siteMode { get; set; }
        Trial trial { get; set; }
        Chase chase { get; set; }
        NationMap nationMap { get; set; }
        Election election { get; set; }
        Law law { get; set; }
        Meeting meeting { get; set; }
        ShopUI shop { get; set; }
        HighScorePage highScore { get; set; }
        FounderQuestions founderQuestions { get; set; }
        NewsUI news { get; set; }
        FastAdvanceUI fastAdvance { get; set; }
        MartyrScreen martyrScreen { get; set; }

        void showPopup(string text, Action clickAction);
        void showOptionPopup(string text, List<PopupOption> options);
        void showYesNoPopup(string text, List<PopupOption> options);
        void showGuardianPopup(List<Entity> items);
        void addCurrentScreen(UIBase ui);
        void removeCurrentScreen(UIBase ui);
        bool isScreenActive(UIBase ui);
        void showUI();
        void hideUI();
        void closeUI();
        void refreshUI();
        void ExitGame(bool save = true);
        void GameOver();
        void updateDebugLog();

        void doInput(Action action);

        //Events
        void doSpeak(Speak args);
    }

    public struct PopupOption
    {
        public string text;
        public Action action;

        public PopupOption(string text, Action action)
        {
            this.text = text;
            this.action = action;
        }
    }
}
