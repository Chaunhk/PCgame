using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SkillIconControl : MonoBehaviour
{
    public SkillBar skill;
    public SkillBar ulti;
    
    void Update()
    {
        SkillControl();
    }
    private void SkillControl()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            
            skill.StartCD();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ulti.StartCD();
        }
    }
}
