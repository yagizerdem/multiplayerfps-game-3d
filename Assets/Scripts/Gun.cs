using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun 
{
    public float damagePerShot;
    public float heatPerShot;
    public bool isAutomatic;
    public float coolDownTime;
    public string gunName;
    public Gun()
    {
        
    }
    public Gun(float damagePerShot , float heatPerShot , bool isAutomatic , float coolDownTime, string gunName)
    {
        this.damagePerShot = damagePerShot;
        this.heatPerShot = heatPerShot;
        this.isAutomatic = isAutomatic;
        this.gunName = gunName;
        this.coolDownTime = coolDownTime;
    }
    public static IEnumerable<Gun> CreateGuns()
    {
        yield return new Gun(35f, 18f , false , 0.3f , "pistol");
        yield return new Gun(6f, 3f, true, 0.04f, "machinegun");
        yield return new Gun(140f, 80f, false, 1f, "rifle");
    }
}
