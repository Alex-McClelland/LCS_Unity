using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;
using LCS.Engine.Data;

public class PortraitImage : MonoBehaviour {

    public Image fixedPortrait;
    public GameObject faceAnchor;

    public Image faceBase;
    public Image faceAcc;
    public Image neck;
    public Image neckAcc;
    public Image jaw;

    public Image ear_L;
    public Image earAcc_L;
    public Image ear_R;
    public Image earAcc_R;

    public Image hair;
    public Image hairAcc;
    public Image hair_back;
    public Image hair_backAcc;

    public Image eye_L;
    public Image eyeAcc_L;
    public Image eyeMask_L;
    public Image eyeIris_L;
    public Image eyebrow_L;
    public Image eyebrowAcc_L;

    public Image eye_R;
    public Image eyeAcc_R;
    public Image eyeMask_R;
    public Image eyeIris_R;
    public Image eyebrow_R;
    public Image eyebrowAcc_R;

    public Image nose;
    public Image noseAcc;

    public Image mouth;
    public Image mouthAcc;
    public Image mouthBuffer;

    public Image facial_hair_mustache;
    public Image facial_hair_beard;
    public Image beardMask;

    public Image scars;

    public List<GameObject> ageLines;

    public GameObject eyeBandage_L;
    public GameObject eyeBandage_R;
    public Sprite destroyedEye_recent;
    public Sprite destroyedNose;
    public Sprite destroyedNose_recent;

    public const string i_blank = "BLANK.png";

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void buildPortrait(Entity character)
    {
        GameData data = GameData.getData();
        Portrait p = character.getComponent<Portrait>();

        if (p.fixedPortrait != null && p.fixedPortrait != "")
        {
            fixedPortrait.sprite = data.portraitGraphicList[p.fixedPortrait];
            faceAnchor.SetActive(false);
            return;
        }
        else
        {
            fixedPortrait.sprite = data.portraitGraphicList[i_blank];
            faceAnchor.SetActive(true);
        }

        Dictionary<string, string> damagedOrgans = new Dictionary<string, string>();

        foreach (Body.BodyPart part in character.getComponent<Body>().BodyParts)
        {
            foreach (Body.Organ o in part.Organs)
            {
                if (o.Health != Body.Organ.Damage.FINE)
                    damagedOrgans.Add(o.Name, o.Health.ToString());
            }
        }

         buildPortrait(p, p.getComponent<Age>().getAge() >= p.getComponent<Body>().getSpecies().oldage - p.getComponent<Body>().getSpecies().oldage / 6, damagedOrgans);
    }

    public void buildPortrait(Portrait p, bool old, Dictionary<string, string> damagedOrgans)
    {
        GameData data = GameData.getData();

        if (p.fixedPortrait != null && p.fixedPortrait != "")
        {
            fixedPortrait.sprite = data.portraitGraphicList[p.fixedPortrait];
            faceAnchor.SetActive(false);
            return;
        }
        else
        {
            fixedPortrait.sprite = data.portraitGraphicList[i_blank];
            faceAnchor.SetActive(true);
        }

        //If someone ages into this range during the game, this will only actually kick in if something forces the portrait to regenerate, but this is fine
        if (old)
        {
            foreach (GameObject g in ageLines)
                g.SetActive(true);
        }
        else
        {
            foreach (GameObject g in ageLines)
                g.SetActive(false);
        }

        foreach (Image i in GetComponentsInChildren<Image>())
        {
            i.color = Color.white;
        }

        faceBase.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (p.faceAcc != "")
            faceAcc.sprite = data.portraitPartList[p.faceAcc].imageVariations[p.faceAccImageNum].image;
        else
            faceAcc.sprite = data.portraitGraphicList[i_blank];
        neck.color = Color.Lerp(new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a), Color.black, 0.1f);
        if (p.neckAcc != "")
            neckAcc.sprite = data.portraitPartList[p.neckAcc].imageVariations[p.neckAccImageNum].image;
        else
            neckAcc.sprite = data.portraitGraphicList[i_blank];
        jaw.sprite = data.portraitPartList[p.jaw].imageVariations[p.jawImageNum].image;
        jaw.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        jaw.transform.localPosition = new Vector3(p.jawOffset.x * 0.01f, p.jawOffset.y * 0.01f);
        beardMask.sprite = data.portraitPartList[p.jaw].imageVariations[p.jawImageNum].mask;
        beardMask.transform.localPosition = new Vector3(p.jawOffset.x * 0.01f, p.jawOffset.y * 0.01f);

        ear_L.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image;
        ear_L.SetNativeSize();
        ear_L.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (data.portraitPartList[p.ear].imageVariations[p.earImageNum].image2 != null)
            ear_R.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image2;
        else
            ear_R.sprite = data.portraitPartList[p.ear].imageVariations[p.earImageNum].image;
        ear_R.SetNativeSize();
        ear_R.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        if (p.earAcc != "")
        {
            earAcc_L.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image;
            earAcc_L.SetNativeSize();
            if (data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image2 != null)
                earAcc_R.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image2;
            else
                earAcc_R.sprite = data.portraitPartList[p.ear].accessories[p.earAcc].imageVariations[p.earAccImageNum].image;
            earAcc_R.SetNativeSize();
        }
        else
        {
            earAcc_L.sprite = data.portraitGraphicList[i_blank];
            earAcc_R.sprite = data.portraitGraphicList[i_blank];
        }

        nose.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        nose.transform.localPosition = new Vector3(p.noseOffset.x, p.noseOffset.y);
        nose.sprite = data.portraitPartList[p.nose].imageVariations[p.noseImageNum].image;
        nose.SetNativeSize();
        if (p.noseAcc != "")
            noseAcc.sprite = data.portraitPartList[p.nose].accessories[p.noseAcc].imageVariations[p.noseAccImageNum].image;
        else
            noseAcc.sprite = data.portraitGraphicList[i_blank];
        noseAcc.SetNativeSize();

        //Left and Right are reversed here, because the variable names refer to location on screen while the organ names refer to the character's perspective
        if (damagedOrgans.ContainsKey("Right Eye"))
        {
            if (damagedOrgans["Right Eye"] == "DESTROYED_RECENT")
            {
                eye_L.sprite = destroyedEye_recent;
                eye_L.SetNativeSize();
                eyeIris_L.gameObject.SetActive(false);
                eyeBandage_L.gameObject.SetActive(false);
            }
            else
            {
                eye_L.sprite = data.portraitGraphicList[i_blank];
                eyeIris_L.gameObject.SetActive(false);
                eyeBandage_L.gameObject.SetActive(true);
            }
        }
        else
        {
            eyeIris_L.gameObject.SetActive(true);
            eyeBandage_L.gameObject.SetActive(false);
            eye_L.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image;
            eye_L.SetNativeSize();
            eyeMask_L.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].mask;
            eyeMask_L.SetNativeSize();
        }

        if (damagedOrgans.ContainsKey("Left Eye"))
        {
            if (damagedOrgans["Left Eye"] == "DESTROYED_RECENT")
            {
                eye_R.sprite = destroyedEye_recent;
                eye_R.SetNativeSize();
                eyeIris_R.gameObject.SetActive(false);
                eyeBandage_R.gameObject.SetActive(false);
            }
            else
            {
                eye_R.sprite = data.portraitGraphicList[i_blank];
                eyeIris_R.gameObject.SetActive(false);
                eyeBandage_R.gameObject.SetActive(true);
            }
        }
        else
        {
            eyeIris_R.gameObject.SetActive(true);
            eyeBandage_R.gameObject.SetActive(false);
            if (data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image2 != null)
                eye_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image2;
            else
                eye_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].image;
            eye_R.SetNativeSize();
            eyeMask_R.sprite = data.portraitPartList[p.eye].imageVariations[p.eyeImageNum].mask;
            eyeMask_R.SetNativeSize();
        }

        eye_L.transform.localPosition = new Vector3(-p.eyeOffset.x, p.eyeOffset.y);
        eye_R.transform.localPosition = new Vector3(p.eyeOffset.x, p.eyeOffset.y);
        eyeIris_L.color = new Color32(p.eyeColor.r, p.eyeColor.g, p.eyeColor.b, p.eyeColor.a);
        eyeIris_R.color = new Color32(p.eyeColor.r, p.eyeColor.g, p.eyeColor.b, p.eyeColor.a);

        if (p.eyeAcc != "")
        {
            eyeAcc_L.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image;
            eyeAcc_L.SetNativeSize();
            if (data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image2 != null)
                eyeAcc_R.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image2;
            else
                eyeAcc_R.sprite = data.portraitPartList[p.eye].accessories[p.eyeAcc].imageVariations[p.eyeAccImageNum].image;
            eyeAcc_R.SetNativeSize();
        }
        else
        {
            eyeAcc_L.sprite = data.portraitGraphicList[i_blank];
            eyeAcc_R.sprite = data.portraitGraphicList[i_blank];
        }

        eyebrow_L.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image;
        eyebrow_L.SetNativeSize();
        eyebrow_L.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image2 != null)
            eyebrow_R.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image2;
        else
            eyebrow_R.sprite = data.portraitPartList[p.eyebrow].imageVariations[p.eyebrowImageNum].image;
        eyebrow_R.SetNativeSize();
        eyebrow_R.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (p.eyebrowAcc != "")
        {
            eyebrowAcc_L.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image;
            eyebrowAcc_L.SetNativeSize();
            if (data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image2 != null)
                eyebrowAcc_R.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image2;
            else
                eyebrowAcc_R.sprite = data.portraitPartList[p.eyebrow].accessories[p.eyebrowAcc].imageVariations[p.eyebrowAccImageNum].image;
            eyebrowAcc_R.SetNativeSize();
        }
        else
        {
            eyebrowAcc_L.sprite = data.portraitGraphicList[i_blank];
            eyebrowAcc_R.sprite = data.portraitGraphicList[i_blank];
        }

        mouthBuffer.transform.localPosition = new Vector3(p.mouthOffset.x, p.mouthOffset.y);
        mouthBuffer.color = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
        mouth.transform.localPosition = new Vector3(p.mouthOffset.x, p.mouthOffset.y);
        mouth.sprite = data.portraitPartList[p.mouth].imageVariations[p.mouthImageNum].image;
        mouth.SetNativeSize();
        mouth.color = Color.Lerp(new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a), Color.red, 0.1f);
        if (p.mouthAcc != "")
            mouthAcc.sprite = data.portraitPartList[p.mouth].accessories[p.mouthAcc].imageVariations[p.mouthImageNum].image;
        else
            mouthAcc.sprite = data.portraitGraphicList[i_blank];
        mouthAcc.SetNativeSize();

        hair.sprite = data.portraitPartList[p.hair].imageVariations[p.hairImageNum].image;
        hair.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (data.portraitPartList[p.hair].imageVariations[p.hairImageNum].image2 != null)
            hair_back.sprite = data.portraitPartList[p.hair].imageVariations[p.hairImageNum].image2;
        else
            hair_back.sprite = data.portraitGraphicList[i_blank];
        hair_back.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        if (p.hairAcc != "")
        {
            hairAcc.sprite = data.portraitPartList[p.hair].accessories[p.hairAcc].imageVariations[p.hairAccImageNum].image;
            if (data.portraitPartList[p.hair].accessories[p.hairAcc].imageVariations[p.hairAccImageNum].image2 != null)
                hair_backAcc.sprite = data.portraitPartList[p.hair].accessories[p.hairAcc].imageVariations[p.hairAccImageNum].image2;
            else
                hair_backAcc.sprite = data.portraitGraphicList[i_blank];

            if ((GameData.getData().portraitPartList[p.hair].accessories[p.hairAcc].flags & PortraitPartDef.PortraitPartFlags.DYE) != 0)
                hairAcc.color = new Color32(p.hairDyeColor.r, p.hairDyeColor.g, p.hairDyeColor.b, p.hairDyeColor.a);
            else
                hairAcc.color = Color.white;
            if ((GameData.getData().portraitPartList[p.hair].accessories[p.hairAcc].flags & PortraitPartDef.PortraitPartFlags.DYE) != 0)
                hair_backAcc.color = new Color32(p.hairDyeColor.r, p.hairDyeColor.g, p.hairDyeColor.b, p.hairDyeColor.a);
            else
                hair_backAcc.color = Color.white;
        }
        else
        {
            hairAcc.sprite = data.portraitGraphicList[i_blank];
            hair_backAcc.sprite = data.portraitGraphicList[i_blank];
        }

        if (p.hairFacial != "")
        {
            facial_hair_mustache.sprite = data.portraitPartList[p.hairFacial].imageVariations[p.hairFacialImageNum].image;
            facial_hair_mustache.SetNativeSize();
            facial_hair_mustache.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
            if (data.portraitPartList[p.hairFacial].imageVariations[p.hairFacialImageNum].image2 != null)
                facial_hair_beard.sprite = data.portraitPartList[p.hairFacial].imageVariations[p.hairFacialImageNum].image2;
            else
                facial_hair_beard.sprite = data.portraitGraphicList[i_blank];
            facial_hair_beard.color = new Color32(p.hairColor.r, p.hairColor.g, p.hairColor.b, p.hairColor.a);
        }
        else
        {
            facial_hair_mustache.sprite = data.portraitGraphicList[i_blank];
            facial_hair_beard.sprite = data.portraitGraphicList[i_blank];
        }

        if (p.scar != "")
        {
            scars.sprite = data.portraitPartList[p.scar].imageVariations[p.scarLevel].image;
            Color32 scarColor = new Color32(p.skinColor.r, p.skinColor.g, p.skinColor.b, p.skinColor.a);
            float h;
            float s;
            float v;
            Color.RGBToHSV(scarColor, out h, out s, out v);
            s += 0.2f;
            v -= 0.2f;

            scarColor = Color.Lerp(Color.HSVToRGB(h, s, v), Color.red, p.freshScar * 0.1f);
            scars.color = new Color32(scarColor.r, scarColor.g, scarColor.b, (byte)(150 + p.freshScar * 10));
        }
        else
            scars.sprite = data.portraitGraphicList[i_blank];
                
        if (p.forceRegen) p.forceRegen = false;
    }

    public void blackoutPortrait()
    {
        foreach (Image i in GetComponentsInChildren<Image>())
        {
            i.color = Color.black;
        }
    }

    public void dimPortrait()
    {
        foreach (Image i in GetComponentsInChildren<Image>())
        {
            i.color = Color.Lerp(i.color, Color.black, 0.5f);
        }
    }
}
