using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;

public class MartyrScreenImpl : MonoBehaviour, MartyrScreen {

    public UIControllerImpl uiController;
    public Transform listContent;

    public MemorialUI p_Memorial;

    private List<GameObject> generatedObjects;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void show()
    {
        generatedObjects = new List<GameObject>();

        foreach(LiberalCrimeSquad.Memorial m in MasterController.lcs.liberalMartyrs)
        {
            MemorialUI memorial = Instantiate(p_Memorial);
            memorial.transform.SetParent(listContent, false);
            memorial.init(uiController, m);

            generatedObjects.Add(memorial.gameObject);
        }

        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void close()
    {
        hide();
        foreach (GameObject o in generatedObjects)
        {
            Destroy(o);
        }
        uiController.removeCurrentScreen(this);
    }

    public void refresh()
    {

    }

    public void back()
    {
        close();
        uiController.baseMode.show();
    }
}
