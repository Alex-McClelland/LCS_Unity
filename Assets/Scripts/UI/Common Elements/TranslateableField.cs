using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class TranslateableField : MonoBehaviour
{
    public string translationKey;

    void Awake()
    {
        if(translationKey != "")
        {
            Text text = GetComponent<Text>();
            if(text != null)
            {
                if (GameData.getData().translationList.ContainsKey(translationKey))
                {
                    text.text = GameData.getData().translationList[translationKey];
                }
                else
                {
                    MasterController.GetMC().addDebugMessage("Missing translation reference " + translationKey);
                }
            }
        }
    }
}
