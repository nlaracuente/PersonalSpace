using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles playing sound on over and button click
/// </summary>
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    public void OnPointerEnter(PointerEventData ped)
    {
        AudioManager.instance.PlaySound(AudioName.ButtonHover);
    }

    public void OnPointerDown(PointerEventData ped)
    {
        AudioManager.instance.PlaySound(AudioName.ButtonClick);
    }
}
