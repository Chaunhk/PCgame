using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralMenuController : MonoBehaviour
{
    public GameObject menu;

    [SerializeField] private GameObject _btnNewGame;
    [SerializeField] private GameObject _btnContinue;
    [SerializeField] private GameObject _btnOption;
    [SerializeField] private GameObject _btnCredit;
    [SerializeField] private GameObject _btnHighScore;
    [SerializeField] private GameObject _btnQuit;

    public void OnClickNewGame()
    {
        SceneManager.LoadScene("TDgameplay");
    }
    private void OnClickContinue()
    {
        //Load save file
        SceneManager.LoadScene("TDgameplay");
    }
    private void OnClickCredit()
    {
        SceneManager.LoadScene("Credit");
    }
    private void OnClickOption()
    {
        SceneManager.LoadScene("Option");
    }
    private void OnClickHighScore()
    {
        SceneManager.LoadScene("HighScore");
    }
    private void OnClickQuit()
    {
    }
}
