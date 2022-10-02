using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;

public class FinanceView : MonoBehaviour, Finances
{
    public UIControllerImpl uiController;

    public Text t_currentIncome;
    public Text t_currentExpenses;
    public Text t_currentTotal;
    public Text t_lastIncome;
    public Text t_lastExpenses;    
    public Text t_lastTotal;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void show()
    {
        gameObject.SetActive(true);
        uiController.addCurrentScreen(this);

        LiberalCrimeSquad lcs = MasterController.GetMC().worldState.getComponent<LiberalCrimeSquad>();

        t_currentIncome.text = lcs.monthlyIncome.ToString("C00");
        t_currentExpenses.text = lcs.monthlyExpenses.ToString("C00");
        t_lastIncome.text = lcs.lastMonthIncome.ToString("C00");
        t_lastExpenses.text = lcs.lastMonthExpenses.ToString("C00");

        if(lcs.monthlyIncome - lcs.monthlyExpenses < 0)
        {
            t_currentTotal.color = Color.red;
        }
        else
        {
            t_currentTotal.color = Color.white;
        }

        if (lcs.lastMonthIncome - lcs.lastMonthExpenses < 0)
        {
            t_lastTotal.color = Color.red;
        }
        else
        {
            t_lastTotal.color = Color.white;
        }

        t_currentTotal.text = (lcs.monthlyIncome - lcs.monthlyExpenses).ToString("C00");
        t_lastTotal.text = (lcs.lastMonthIncome - lcs.lastMonthExpenses).ToString("C00");
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

    }

    public void back()
    {
        close();
        uiController.baseMode.show();
    }
}
