using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleSceneControl : MonoBehaviour
{
    enum STATE
    {
        NONE = -1,
        WAIT = 0,
        PLAY_JINGLE,

        NUM,
    }

    private STATE state = STATE.WAIT;
    private STATE next_state = STATE.NONE;

    private float state_timer = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.state_timer += Time.deltaTime;

        switch (this.state)
        {
            case STATE.WAIT:
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        this.next_state = STATE.PLAY_JINGLE;
                    }
                }
                break;

            case STATE.PLAY_JINGLE:
                {
                    if(this.state_timer > 1.0f)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene0");
                    }
                }
                break;
        }

        if(this.next_state != STATE.NONE)
        {
            switch (this.next_state)
            {
                case STATE.PLAY_JINGLE:
                    {
                        // 播放开始游戏的音乐
                    }
                    break;
            }

            this.state = this.next_state;
            this.next_state = STATE.NONE;
            this.state_timer = 0.0f;
        }

        switch (this.state)
        {
            case STATE.WAIT:
                {

                }
                break;
        }
    }
}
