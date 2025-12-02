using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class Volume : MonoBehaviour
{
    [SerializeField] Slider volumeSlider;
    [SerializeField] GameObject panel;

    private bool Shown = false;

    void Start()
    {
        if(!PlayerPrefs.HasKey("volumeLevel"))
        {
            PlayerPrefs.SetFloat("volumeLevel", 1f);
            Load();
        }
        else
        {
            Load();
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (Shown == false)
            {
                panel.SetActive(!panel.activeSelf);
                Shown = true;
                Time.timeScale = 0f;

            }
            else
            {
                panel.SetActive(!panel.activeSelf);
                Shown = false;
                Time.timeScale = 1f;
            }
        }
    }

    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
        Save();
    }

    void Load()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("volumeLevel");
    }

    void Save()
    {
        PlayerPrefs.SetFloat("volumeLevel", volumeSlider.value);
    }
}
