using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillBar : MonoBehaviour
{
    public Image fill;
    private float cdTime = 5f, currentFill = 1f;
    private float currentAmount;
    public bool isCD;
    private float timer = 0f;

    void Update()
    {
        // Check if the fill is not empty already
        if (currentFill > 0)
        {
            UpdateBar();
        }
        else isCD = false;
    }
    public void InitData(int maxValue)
    {
        cdTime = maxValue;
        currentAmount = maxValue;
        fill.fillAmount = 0;
        isCD = false;
    }
    public void StartCD()
    {
        if (isCD == false)
        {
            isCD = true;
            timer = 0f;
            currentFill = 1f; // Reset to full
        }
        
    }

    private void UpdateBar()
    {

        timer += Time.deltaTime;
        // Calculate the fill based on the elapsed time
        currentFill = 1 - (timer / cdTime);
        fill.fillAmount = currentFill;
    }

}
