using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCS.Engine;
using LCS.Engine.Components.Location;

public class BaseStorageView : MonoBehaviour {

    private enum SelectedView
    {
        NONE,
        CORPSE,
        HOSTAGE
    }

    public SafeHouseView safehouseController;

    public CorpseView p_CorpseView;
    public Transform content;

    private List<GameObject> generatedObjects;
    private Entity selectedSafeHouse;
    private SelectedView selectedView;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void showCorpseList(Entity safeHouse)
    {
        if (generatedObjects == null) generatedObjects = new List<GameObject>();
        foreach(GameObject o in generatedObjects)
        {
            Destroy(o);                
        }

        selectedSafeHouse = safeHouse;
        selectedView = SelectedView.CORPSE;

        generatedObjects.Clear();

        foreach(Entity e in selectedSafeHouse.getComponent<SafeHouse>().getBodies())
        {
            CorpseView corpse = Instantiate(p_CorpseView);
            generatedObjects.Add(corpse.gameObject);
            corpse.transform.SetParent(content, false);
            corpse.safehouseController = safehouseController;

            corpse.displayCharacter(e);
        }

        gameObject.SetActive(true);
    }

    public void showHostageList(Entity safeHouse)
    {
        if (generatedObjects == null) generatedObjects = new List<GameObject>();
        foreach (GameObject o in generatedObjects)
        {
            Destroy(o);
        }

        selectedSafeHouse = safeHouse;
        selectedView = SelectedView.HOSTAGE;

        generatedObjects.Clear();

        foreach (Entity e in selectedSafeHouse.getComponent<SafeHouse>().getHostages())
        {
            CorpseView corpse = Instantiate(p_CorpseView);
            generatedObjects.Add(corpse.gameObject);
            corpse.transform.SetParent(content, false);
            corpse.safehouseController = safehouseController;

            corpse.displayCharacter(e);
        }

        gameObject.SetActive(true);
    }

    public void hideAll()
    {
        if (generatedObjects == null) generatedObjects = new List<GameObject>();
        foreach (GameObject o in generatedObjects)
        {
            Destroy(o);
        }

        generatedObjects.Clear();
        selectedView = SelectedView.NONE;

        gameObject.SetActive(false);
    }

    public void refresh()
    {
        switch (selectedView)
        {
            case SelectedView.CORPSE:
                showCorpseList(selectedSafeHouse);
                break;
        }
    }
}
