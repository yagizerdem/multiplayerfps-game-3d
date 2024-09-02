using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public static GameUIController instance;
    public Slider heatSlider;
    public TextMeshProUGUI overHeatLable;
    private Stopwatch timer;
    public TextMeshProUGUI healtAmountLable;
    public TextMeshProUGUI remainigTimeLable;
    private void Awake()
    {
        instance = this;
        timer = new Stopwatch();
        overHeatLable.gameObject.SetActive(false);
    }
    private void Update()
    {
        ManageOverheatLable();
    }

    public void SetHeatSliderValues(float min , float max)
    {
        heatSlider.minValue = min;
        heatSlider.maxValue = max;
    }
    public void UpdateHeatSliderValue(float heatAmount)
    {
        this.heatSlider.value = heatAmount;
    }
    // public api for interact with overheat lable 
    public void ShowOverHeatLable()
    {
        overHeatLable.gameObject.SetActive(true);
        timer.Restart();
    }
    private void ManageOverheatLable()
    {
        if (overHeatLable.gameObject.activeSelf)
        {
            TimeSpan span = timer.Elapsed;
            if (span.TotalSeconds >= (double)2)
            {
                timer.Stop();
                overHeatLable.gameObject.SetActive(false);
            }
        }
    }
    
    public void UpdateHealtAmountLable(float healtAmount)
    {
        string output = $"{healtAmount}/100";
        this.healtAmountLable.text = output;
    }

    public void UpdateRemainigTimeLable(int m , int s)
    {
        this.remainigTimeLable.text = $"{m}:{s}";
    }
}
