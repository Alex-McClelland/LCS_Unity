using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.Components.World;
using LCS.Engine.Components.Location;
using LCS.Engine.Components.Item;
using LCS.Engine.Factories;
using LCS.Engine.Data;

public class ShopScreenImpl : MonoBehaviour, LCS.Engine.UI.ShopUI
{
    public UIControllerImpl uiController;

    public GameObject buyView;
    public GameObject sellView;
    public GameObject modeButtons;
    public Transform cartContent;
    public Transform departmentButtoContainer;
    public Transform buyItemButtonContainer;
    public Transform sellItemButtonContainer;

    public Text t_shopTitle;
    public Text t_cash;
    public Text t_totalCost;

    public Button quicksellButton;
    public Button checkoutButton;

    public Button p_MenuButton;
    public ItemButton p_ItemButton;

    public Entity currentLocation;
    public LiberalCrimeSquad.Squad currentSquad;

    private LCS.Engine.UI.ShopActions actions;
    private List<GameObject> departmentButtons;
    private List<GameObject> buyItemButtons;
    private Dictionary<string, ItemButton> sellItemButtons;
    private Dictionary<string, ItemButton> cartItemButtons;

    public enum ShopMode
    {
        BUY,
        SELL
    }

    private ShopMode mode;

    void Awake()
    {
        departmentButtons = new List<GameObject>();
        buyItemButtons = new List<GameObject>();
        sellItemButtons = new Dictionary<string, ItemButton>();
        cartItemButtons = new Dictionary<string, ItemButton>();
    }

	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void init(LCS.Engine.UI.ShopActions actions)
    {
        this.actions = actions;
    }

    public void show()
    {
        uiController.addCurrentScreen(this);
        gameObject.SetActive(true);
    }

    public void hide()
    {
        gameObject.SetActive(false);
    }

    public void close()
    {
        hide();
        uiController.removeCurrentScreen(this);
    }

    public void refresh()
    {
        if((currentLocation.getComponent<Shop>().getFlags() & LocationDef.ShopFlag.VEHICLES) != 0)
        {
            //Can't quicksell cars
            quicksellButton.gameObject.SetActive(false);
        }
        else
        {
            quicksellButton.gameObject.SetActive(true);
        }

        t_cash.text = "$" + MasterController.lcs.Money;

        foreach (ItemButton g in cartItemButtons.Values)
        {
            Destroy(g.gameObject);
        }
        cartItemButtons.Clear();

        if (mode == ShopMode.BUY)
        {
            foreach(Entity e in currentLocation.getComponent<Shop>().buyCart)
            {
                if (!cartItemButtons.ContainsKey(e.def))
                {
                    ItemButton cartButton = Instantiate(p_ItemButton);
                    cartButton.transform.SetParent(cartContent, false);
                    cartButton.setItem(e);
                    Entity targetItem = e;
                    cartButton.button.onClick.AddListener(() => 
                    {
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        {
                            actions.removeAllSimilarItemsFromBuyCart(currentLocation, targetItem);
                        }
                        else
                        {
                            actions.removeItemFromBuyCart(currentLocation, targetItem); 
                        }
                        refresh();
                    });
                    cartItemButtons[e.def] = cartButton;
                }
                else
                {
                    cartItemButtons[e.def].changeCount(1);
                }
            }

            t_totalCost.text = "TOTAL: $" + currentLocation.getComponent<Shop>().getTotalBuyValue();
            if (currentLocation.getComponent<Shop>().getTotalBuyValue() > MasterController.lcs.Money)
                checkoutButton.interactable = false;
            else
                checkoutButton.interactable = true;
        }
        else if(mode == ShopMode.SELL)
        {
            buildSellList();

            foreach (Entity e in currentLocation.getComponent<Shop>().sellCart)
            {
                string itemName = e.def;
                if (e.hasComponent<Armor>()) itemName += e.getComponent<Armor>().quality;

                if (!cartItemButtons.ContainsKey(itemName))
                {
                    ItemButton cartButton = Instantiate(p_ItemButton);
                    cartButton.transform.SetParent(cartContent, false);
                    cartButton.setItem(e);
                    Entity targetItem = e;
                    cartButton.button.onClick.AddListener(() => {
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        {
                            actions.removeAllSimilarItemsFromSellCart(currentLocation, targetItem);
                        }
                        else
                        {
                            actions.removeItemFromSellCart(currentLocation, targetItem);
                        }
                        refresh();
                    });
                    cartItemButtons[itemName] = cartButton;
                }
                else
                {
                    cartItemButtons[itemName].changeCount(1);
                }
            }

            t_totalCost.text = "TOTAL: $" + currentLocation.getComponent<Shop>().getTotalSellValue();
            checkoutButton.interactable = true;
        }
    }

    public void startShopping(Entity location, LiberalCrimeSquad.Squad squad)
    {
        show();
        uiController.squadUI.displaySquad(squad);
        currentLocation = location;
        currentSquad = squad;
        setMode("BUY");

        if ((location.getComponent<Shop>().getFlags() & LocationDef.ShopFlag.SELL_ITEMS) != 0)
        {
            modeButtons.SetActive(true);
            buildSellList();
        }
        else
            modeButtons.SetActive(false);

        t_cash.text = "$" + MasterController.lcs.Money;
        t_shopTitle.text = location.getComponent<SiteBase>().getCurrentName();
        t_totalCost.text = "TOTAL: $0";

        foreach(LocationDef.ShopDepartmentDef department in location.getComponent<Shop>().getDepartments())
        {
            Button depButton = Instantiate(p_MenuButton);
            depButton.transform.SetParent(departmentButtoContainer, false);
            depButton.GetComponentInChildren<Text>().text = department.name;
            depButton.onClick.AddListener(() => { showDepartment(department.name); });
            departmentButtons.Add(depButton.gameObject);
        }
    }

    public void endShopping()
    {
        foreach(GameObject g in departmentButtons)
        {
            Destroy(g);
        }
        departmentButtons.Clear();

        foreach(GameObject g in buyItemButtons)
        {
            Destroy(g);
        }
        buyItemButtons.Clear();

        foreach(ItemButton g in sellItemButtons.Values)
        {
            Destroy(g.gameObject);
        }
        sellItemButtons.Clear();

        foreach(ItemButton g in cartItemButtons.Values)
        {
            Destroy(g.gameObject);
        }
        cartItemButtons.Clear();

        actions.finishShopping(currentLocation);
    }

    private void showDepartment(string depName)
    {
        foreach (GameObject g in buyItemButtons)
        {
            Destroy(g);
        }
        buyItemButtons.Clear();

        foreach (LocationDef.ShopDepartmentDef department in currentLocation.getComponent<Shop>().getDepartments())
        {
            if (department.name != depName) continue;

            foreach(LocationDef.ShopItemDef item in department.items)
            {
                if ((currentLocation.getComponent<Shop>().getFlags() & LocationDef.ShopFlag.LEGAL_ONLY) != 0)
                {
                    if (item.item.components.ContainsKey("weapon") &&
                        ((ItemDef.WeaponDef)item.item.components["weapon"]).legality < (int)MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment)
                        continue;
                    if (item.item.components.ContainsKey("clip") &&
                        ((ItemDef.ClipDef)item.item.components["clip"]).legality < (int)MasterController.government.laws[Constants.LAW_GUN_CONTROL].alignment)
                        continue;
                }

                Button buyItemButton = Instantiate(p_MenuButton);
                buyItemButton.transform.SetParent(buyItemButtonContainer, false);
                buyItemButton.GetComponentInChildren<Text>().text = item.text + " ($" + currentLocation.getComponent<Shop>().itemPrices[item.item.type] + ")";
                buyItemButton.onClick.AddListener(() => { actions.addItemToBuyCart(currentLocation, ItemFactory.create(item.item.type)); refresh(); });
                buyItemButtons.Add(buyItemButton.gameObject); 
            }
        }
    }

    private void buildSellList()
    {
        foreach (ItemButton g in sellItemButtons.Values)
        {
            Destroy(g.gameObject);
        }
        sellItemButtons.Clear();

        //TODO: This is an incredibly inefficient way to handle this, fix up later
        foreach (Entity e in currentSquad.homeBase.getComponent<SafeHouse>().getInventory())
        {
            if (!e.hasComponent<Loot>()) continue;
            if (currentLocation.getComponent<Shop>().sellCart.Contains(e)) continue;
            if (e.getComponent<ItemBase>().targetBase != null) continue;

            //Shops won't buy bloody or damaged armor
            if (e.hasComponent<Armor>())
            {
                if (e.getComponent<Armor>().damaged || e.getComponent<Armor>().bloody)
                    continue;
            }

            if((currentLocation.getComponent<Shop>().getFlags() & LocationDef.ShopFlag.VEHICLES) != 0)
            {
                //Car dealerships don't care about buying random junk
                if (!e.hasComponent<Vehicle>())
                    continue;
            }
            //Only car dealerships will buy cars
            else if(e.hasComponent<Vehicle>())
            {
                continue;
            }

            string itemName = e.def;
            if (e.hasComponent<Armor>()) itemName += e.getComponent<Armor>().quality;

            if (!sellItemButtons.ContainsKey(itemName))
            {
                ItemButton cartButton = Instantiate(p_ItemButton);
                cartButton.transform.SetParent(sellItemButtonContainer, false);
                cartButton.setItem(e);
                Entity targetItem = e;
                cartButton.button.onClick.AddListener(() => 
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        actions.addAllSimilarItemsToSellCart(currentLocation, targetItem, currentSquad.homeBase);
                    }
                    else
                    {
                        actions.addItemToSellCart(currentLocation, targetItem);
                    }
                    refresh();
                });
                sellItemButtons[itemName] = cartButton;
            }
            else
            {
                sellItemButtons[itemName].changeCount(1);
            }
        }
    }

    public void setMode(string shopMode)
    {
        if(shopMode == "BUY")
        {
            mode = ShopMode.BUY;
            buyView.SetActive(true);
            sellView.SetActive(false);
            checkoutButton.GetComponentInChildren<Text>().text = "Buy (⏎)";
            checkoutButton.onClick.RemoveAllListeners();
            checkoutButton.onClick.AddListener(() => { actions.buy(currentLocation); refresh(); });
        }
        else if(shopMode == "SELL")
        {
            mode = ShopMode.SELL;
            buyView.SetActive(false);
            sellView.SetActive(true);
            checkoutButton.GetComponentInChildren<Text>().text = "Sell (⏎)";
            checkoutButton.onClick.RemoveAllListeners();
            checkoutButton.onClick.AddListener(() => { actions.sell(currentLocation); refresh(); });
        }

        refresh();
    }

    public void quickSell()
    {
        foreach (Entity e in currentSquad.homeBase.getComponent<SafeHouse>().getInventory())
        {
            if (!e.hasComponent<Loot>()) continue;
            if (currentLocation.getComponent<Shop>().sellCart.Contains(e)) continue;

            if ((e.getComponent<Loot>().getFlags() & ItemDef.LootFlags.QUICK_FENCE) != 0)
                actions.addItemToSellCart(currentLocation, e);
        }

        refresh();
    }
}
