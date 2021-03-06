using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.Item;

public class PopupGuardian : MonoBehaviour {

    public UIControllerImpl uiController;
    public GameObject dimmer;
    public Transform buttonContent;

    public Button p_MenuButton;

    public bool popupOpen;

    private List<GameObject> generatedButtons;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void doPopup(List<Entity> items)
    {
        generatedButtons = new List<GameObject>();

        dimmer.SetActive(true);
        popupOpen = true;
        gameObject.SetActive(true);

        foreach(Entity item in items)
        {
            Button itemButton = Instantiate(p_MenuButton);
            itemButton.transform.SetParent(buttonContent, false);
            itemButton.GetComponentInChildren<Text>().text = item.getComponent<ItemBase>().getName();
            itemButton.onClick.AddListener(() =>
            {
                MasterController.news.publishSpecialEdition(item);
                close();
            });

            generatedButtons.Add(itemButton.gameObject);
        }
    }

    public void close()
    {
        foreach(GameObject o in generatedButtons)
        {
            Destroy(o);
        }

        generatedButtons.Clear();

        dimmer.SetActive(false);
        gameObject.SetActive(false);
        popupOpen = false;

        MasterController.GetMC().doNextAction();
    }
}
