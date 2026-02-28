using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GeneralBar : MonoBehaviour
{
    public Image bar;
    public Image fill;
    private int maxAmount, currentAmount, decAmount;
    public bool isDec;
    void FixedUpdate()
    {
        if (!isDec)
        {
            return;
        }

        if (fill.fillAmount > (float)currentAmount / (float)maxAmount)
        {
            DecAction();
        }
        else
        {
            isDec = false;
        }
    }
    public void InitData(int maxValue)
    {
        maxAmount = maxValue;
        currentAmount = maxValue;
        fill.fillAmount = 1;
        bar.fillAmount = 1;
    }
    public void ResetData(int maxValue)
    {
        maxAmount = maxValue;
        currentAmount = 0;
        //fill.fillAmount = 0;
        bar.fillAmount = 0;
    }
    public void Decrease(int val)
    {
        DecreaseValue(maxAmount, currentAmount - val, val);
    }
    public void Increase(int val){
        IncreaseValue(maxAmount, currentAmount + val, val);
    }
    private void DecreaseValue(int maxAmountTm, int currentAmountTm, int decAmountTm)
    {
        maxAmount = maxAmountTm;
        currentAmount = currentAmountTm;
        decAmount = decAmountTm;
        isDec = true;
        UpdateBar();
    }
    private void IncreaseValue(int maxAmountTm, int currentAmountTm, int decAmountTm){
        maxAmount = maxAmountTm;
        currentAmount = currentAmountTm;
        UpdateBar();
        fill.fillAmount = bar.fillAmount;
    }
    private void UpdateBar()
    {
        bar.fillAmount = (float)currentAmount / (float)maxAmount;
    }
    private void DecAction()
    {
        fill.fillAmount -= Time.deltaTime / 5;
    }
}
