using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using LCS.Engine;
using LCS.Engine.UI;
using LCS.Engine.Components.World;

public class FinanceView : MonoBehaviour, Finances
{
    public FinancialMonthDisplay p_FinancialMonthDisplay;
    public Transform reportContainer;
    public Text t_Total;

    public UIControllerImpl uiController;
    public List<FinancialMonthDisplay> months;

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

        int total = 0;

        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        culture.NumberFormat.CurrencyNegativePattern = 1;

        while (months.Count < lcs.financials.Count)
        {
            FinancialMonthDisplay month = Instantiate(p_FinancialMonthDisplay, reportContainer);
            month.transform.SetAsFirstSibling();
            months.Add(month);
        }

        for (int i = 0; i < lcs.financials.Count; i++)
        {
            DateTime modMonth = MasterController.GetMC().currentDate.AddMonths(-i);

            months[i].t_Date.text = modMonth.ToString("MMMM yyyy");
            months[i].t_Income.text = lcs.financials[i].income.ToString("C00", culture);
            months[i].t_Expenses.text = lcs.financials[i].expenses.ToString("C00", culture);
            months[i].t_Net.text = (lcs.financials[i].income - lcs.financials[i].expenses).ToString("C00", culture);

            if (lcs.financials[i].income - lcs.financials[i].expenses < 0)
                months[i].t_Net.color = Color.red;
            else
                months[i].t_Net.color = Color.white;

            total += lcs.financials[i].income - lcs.financials[i].expenses;
        }

        t_Total.text = total.ToString("C00", culture);

        if (total < 0)
            t_Total.color = Color.red;
        else
            t_Total.color = Color.white;
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
