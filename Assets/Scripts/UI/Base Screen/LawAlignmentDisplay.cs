using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;

public class LawAlignmentDisplay : MonoBehaviour {

    public MouseOverText mouseoverText;
    public Text t_LawName;
    public Image i_CC;
    public Image i_C;
    public Image i_M;
    public Image i_L;
    public Image i_LL;

    public Sprite smallPip;
    public Sprite largePip;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void setLaw(string def, Alignment align)
    {
        t_LawName.text = GameData.getData().lawList[def].name;
        Color alignColor = Color.white;

        switch (align)
        {
            case Alignment.ARCHCONSERVATIVE:
                alignColor = Color.red;
                i_CC.sprite = largePip;
                i_C.sprite = smallPip;
                i_M.sprite = smallPip;
                i_L.sprite = smallPip;
                i_LL.sprite = smallPip;
                break;
            case Alignment.CONSERVATIVE:
                alignColor = Color.magenta;
                i_CC.sprite = smallPip;
                i_C.sprite = largePip;
                i_M.sprite = smallPip;
                i_L.sprite = smallPip;
                i_LL.sprite = smallPip;
                break;
            case Alignment.MODERATE:
                alignColor = Color.yellow;
                i_CC.sprite = smallPip;
                i_C.sprite = smallPip;
                i_M.sprite = largePip;
                i_L.sprite = smallPip;
                i_LL.sprite = smallPip;
                break;
            case Alignment.LIBERAL:
                alignColor = Color.cyan;
                i_CC.sprite = smallPip;
                i_C.sprite = smallPip;
                i_M.sprite = smallPip;
                i_L.sprite = largePip;
                i_LL.sprite = smallPip;
                break;
            case Alignment.ELITE_LIBERAL:
                alignColor = Color.green;
                i_CC.sprite = smallPip;
                i_C.sprite = smallPip;
                i_M.sprite = smallPip;
                i_L.sprite = smallPip;
                i_LL.sprite = largePip;
                break;
        }

        mouseoverText.mouseOverText = GameData.getData().lawList[def].description[align];

        t_LawName.color = alignColor;
        i_CC.color = alignColor;
        i_CC.SetNativeSize();
        i_C.color = alignColor;
        i_C.SetNativeSize();
        i_M.color = alignColor;
        i_M.SetNativeSize();
        i_L.color = alignColor;
        i_L.SetNativeSize();
        i_LL.color = alignColor;
        i_LL.SetNativeSize();
    }
}
