using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class TranslateableField : MonoBehaviour
{
    public string translationKey;
    public bool ignoreTranslation = false;

    void Awake()
    {
        setTranslation();
    }

    public void setTranslation()
    {
        if (ignoreTranslation) return;

        Text text = GetComponent<Text>();
        if (text != null) {
            if (translationKey != "")
            {
                if (GameData.getData().translationList.ContainsKey(translationKey))
                {
                    text.text = GameData.getData().translationList[translationKey];
                }
                else
                {
                    MasterController.GetMC().addDebugMessage("Missing translation reference " + translationKey);
                    text.text += " $$UNTRANSLATED$$";
                }
            }
            else if (text.text != "")
            {
                text.text += " $$UNTRANSLATED$$";
            }
        }
    }
}
