using UnityEngine;
using System.Collections;

public class GameControl : MonoBehaviour
{
    enum STATE
    {
        NONE = -1,
        PLAY = 0,
        CLEAR,
        NUM,
    }

    private STATE state = STATE.PLAY;
    private STATE next_state = STATE.NONE;

    private float state_timer = 0.0f;

    public GameObject pazzlePrefab = null;
    public PazzleControl pazzle_control = null;

    public GameObject retry_button = null;
    public GameObject complete_image = null;
    
    void Start()
    {
        this.pazzle_control = (Instantiate(this.pazzlePrefab) as GameObject).GetComponent<PazzleControl>();
    }
    
    void Update()
    {
        this.state_timer += Time.deltaTime;

        switch (this.state)
        {
            case STATE.PLAY:
                {
                    if (this.pazzle_control.isCleared())
                    {
                        this.next_state = STATE.CLEAR;
                    }
                }
                break;
            case STATE.CLEAR:
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
                    }
                }
                break;
        }

        if(this.next_state != STATE.NONE)
        {
            switch (this.next_state)
            {
                case STATE.CLEAR:
                    {
                        this.retry_button.SetActive(false);
                        this.complete_image.SetActive(true);
                    }
                    break;
            }

            this.state = this.next_state;
            this.next_state = STATE.NONE;
            this.state_timer = 0.0f;
        }
    }

    public void OnRetryButtonPush()
    {
        if (!this.pazzle_control.isCleared())
        {
            this.pazzle_control.beginRetryAction();
        }
    }
}
