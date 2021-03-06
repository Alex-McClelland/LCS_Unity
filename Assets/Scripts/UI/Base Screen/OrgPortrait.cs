using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Creature;

public class OrgPortrait : MonoBehaviour {

    public LiberalManagementBoard liberalManagementBoard { get; set; }
    public Entity character { get; set; }

    public PortraitImage i_Portrait;
    public Image i_Overlay;
    public Image i_ActivityIcon;
    public Text t_Name;
    public Transform stringRoot;

    public RectTransform line;
    public RectTransform pin;
    public OrgPortrait parent;
    public OrgOverlays overlays;

    public List<Sprite> activityIcons;
    private Dictionary<string, Sprite> activityIconDictionary;

    void Awake()
    {
        activityIconDictionary = new Dictionary<string, Sprite>();

        foreach (Sprite s in activityIcons)
        {
            activityIconDictionary.Add(s.name, s);
        }
    }

	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {

        trackPins();
        if (parent != null) connectString();
        character.getComponent<Liberal>().managerPosX = transform.localPosition.x;
        character.getComponent<Liberal>().managerPosY = transform.localPosition.y;
    }

    public void addParent(OrgPortrait parent, RectTransform line, RectTransform pin)
    {
        this.parent = parent;
        this.line = line;
        this.pin = pin;

        this.line.transform.localPosition = transform.localPosition + stringRoot.localPosition;
        this.pin.transform.localPosition = transform.localPosition + stringRoot.localPosition;

        updateStringColors();
        connectString(); 
    }

    public void updateStringColors()
    {
        switch (character.getComponent<Liberal>().recruitType)
        {
            case Liberal.RecruitType.ENLIGHTENED:
                line.GetComponent<Image>().color = Color.yellow;
                break;
            case Liberal.RecruitType.LOVE_SLAVE:
                line.GetComponent<Image>().color = new Color32(255, 100, 200, 255);
                break;
            default:
                line.GetComponent<Image>().color = Color.white;
                break;
        }
    }

    public void setParent(OrgPortrait parent)
    {
        this.parent = parent;
        updateStringColors();
    }

    public void setActivityIcon()
    {
        string activity = character.getComponent<Liberal>().dailyActivity.type;

        if (activityIconDictionary.ContainsKey(activity) && 
            (character.getComponent<Liberal>().status == Liberal.Status.ACTIVE || 
            character.getComponent<Liberal>().status == Liberal.Status.SLEEPER))
        {
            i_ActivityIcon.gameObject.SetActive(true);
            i_ActivityIcon.sprite = activityIconDictionary[activity];
        }
        else
        {
            i_ActivityIcon.gameObject.SetActive(false);
            i_ActivityIcon.sprite = null;
        }        

        switch (character.getComponent<Liberal>().status)
        {
            default:
                i_Overlay.gameObject.SetActive(false);
                break;
            case Liberal.Status.HOSPITAL:
                i_Overlay.gameObject.SetActive(true);
                i_Overlay.sprite = overlays.i_hospital;
                break;
            case Liberal.Status.JAIL_COURT:
            case Liberal.Status.JAIL_POLICE_CUSTODY:
            case Liberal.Status.JAIL_PRISON:
                i_Overlay.gameObject.SetActive(true);
                i_Overlay.sprite = overlays.i_jail;
                break;
            case Liberal.Status.SLEEPER:
                i_Portrait.blackoutPortrait();
                break;
            case Liberal.Status.AWAY:
                i_Portrait.dimPortrait();
                break;
        }
    }

    public void select()
    {
        if (!GetComponent<ClickAndDrag>().dragging)
        {
            liberalManagementBoard.select(character);
            if(character.getComponent<Liberal>().status == Liberal.Status.ACTIVE)
                setActivityIcon();
        }
    }

    private void connectString()
    {
        line.transform.localPosition = transform.localPosition + stringRoot.localPosition;
        line.sizeDelta = new Vector2(Vector3.Distance(transform.localPosition, parent.transform.localPosition), line.sizeDelta.y);
        line.localEulerAngles = new Vector3(0, 0, Mathf.Atan2((parent.transform.localPosition.y - transform.localPosition.y), (parent.transform.localPosition.x - transform.localPosition.x)) * Mathf.Rad2Deg);
    }

    private void trackPins()
    {
        pin.transform.localPosition = transform.localPosition + stringRoot.localPosition;
    } 
}
