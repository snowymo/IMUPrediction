using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Demo : MonoBehaviour
{
    public GameObject cell;
    public void OnPointerClick(PointerEventData pointerData)
    {
        Debug.Log("Clicked.");
        cell.SetActive(true);
    }
    public void OnPointerStart(PointerEventData pointerData)
    {
        Debug.Log("Clicked.");
        cell.SetActive(true);
    }
}
