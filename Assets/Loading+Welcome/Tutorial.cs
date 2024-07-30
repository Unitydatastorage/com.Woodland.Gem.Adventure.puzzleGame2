using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    private int tut;
    public GameObject window;
    void Start()
    {
        Loads();
        if(tut == 0){
            window.SetActive(true);
        }
    }
    void Loads()
    {
        tut = PlayerPrefs.GetInt("Tw", 0);
    }
    public void TutorialDone(){
        tut = 1;
        PlayerPrefs.SetInt("Tw", tut);
        PlayerPrefs.Save();
    }
}
