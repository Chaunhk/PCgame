using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class SkillIconControl : MonoBehaviour
{
    public void SkillCooldown(SkillBar skill)
    {
            if (skill.isCD == false)
            skill.StartCD();
    }
    public bool CheckCoolDown(SkillBar skill)
    {
        return skill.isCD;
    }
}
