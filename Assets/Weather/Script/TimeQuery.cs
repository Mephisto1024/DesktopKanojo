using UnityEngine;
using System;
using TMPro;
public class TimeQuery : MonoBehaviour
{
    
    public GameObject textWidget;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        textWidget.GetComponent<TMP_Text>().SetText(DateTime.Now.ToLongTimeString());
    }
}
