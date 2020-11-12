using UnityEngine;
using System.Collections;

public class PazzleControl : MonoBehaviour
{
    private int piece_num;
    private int piece_finished_num;

    private PieceControl[] all_pieces;
    private PieceControl[] active_pieces;

    enum STATE
    {
        NONE = -1,
        PLAY = 0,
        CLEAR,
        NUM,
    }

    private STATE state = STATE.NONE;
    private STATE next_state = STATE.NONE;

    private float state_timer = 0.0f;
    private float state_timer_prev = 0.0f;

    private Bounds shuffle_zone;
    private float pazzle_rotation = 37.0f;
    private int shuffle_grid_num = 1;

    private bool is_disp_cleared = false;

    // Use this for initialization
    void Start()
    {
        this.piece_num = 0;

        for(int i = 0; i < this.transform.childCount; i++)
        {
            GameObject piece = this.transform.GetChild(i).gameObject;

            if (!this.is_piece_object(piece))
            {
                continue;
            }
            this.piece_num++;
        }

        this.all_pieces = new PieceControl[this.piece_num];
        this.active_pieces = new PieceControl[this.piece_num];

        for(int i = 0, n = 0; i < this.transform.childCount; i++)
        {
            GameObject piece = this.transform.GetChild(i).gameObject;

            if (!this.is_piece_object(piece))
            {
                continue;
            }

            piece.AddComponent<PieceControl>();
            piece.GetComponent<PieceControl>().pazzle_control = this;

            this.all_pieces[n++] = piece.GetComponent<PieceControl>();
        }

        this.piece_finished_num = 0;

        this.calc_shuffle_zone();

        this.is_disp_cleared = false;
    }
    
    void Update()
    {
        this.state_timer_prev = this.state_timer;
        this.state_timer += Time.deltaTime;

        switch (this.state)
        {
            case STATE.NONE:
                {
                    this.next_state = STATE.PLAY;
                }
                break;
            case STATE.PLAY:
                {
                    if(this.piece_finished_num >= this.piece_num)
                    {
                        this.next_state = STATE.CLEAR;
                    }
                }
                break;
        }

        if(this.next_state != STATE.NONE)
        {
            switch (this.next_state)
            {
                case STATE.PLAY:
                    {
                        for(int i = 0; i < this.all_pieces.Length; i++)
                        {
                            this.active_pieces[i] = this.all_pieces[i];
                        }

                        this.piece_finished_num = 0;

                        this.shuffle_pieces();

                        foreach(PieceControl piece in this.active_pieces)
                        {
                            piece.Restart();
                        }
                        
                    }
                    break;
                case STATE.CLEAR:
                    {

                    }
                    break;
            }

            this.state = this.next_state;
            this.next_state = STATE.NONE;
            this.state_timer = 0.0f;
        }

        switch (this.state)
        {
            case STATE.CLEAR:
                {
                    this.is_disp_cleared = true;
                }
                break;
        }

        PazzleControl.DrawBounds(this.shuffle_zone);
    }

    // ---------------------------------------------------------------------------------------- //

    private static float SHUFFLE_ZONE_OFFSET_X = -5.0f;
    private static float SHUFFLE_ZONE_OFFSET_Y = 1.0f;
    private static float SHUFFLE_ZONE_SCALE = 1.1f;

    private void calc_shuffle_zone()
    {
        Vector3 center;
        center = Vector3.zero;

        // 这里是取所有碎片的平均中心点
        foreach(PieceControl piece in this.all_pieces)
        {
            center += piece.finished_position;
        }
        center /= (float)this.all_pieces.Length;

        // 偏移出拼图区
        center.x += SHUFFLE_ZONE_OFFSET_X;
        center.z += SHUFFLE_ZONE_OFFSET_Y;

        // 根据碎片数量，计算容器框单边的格子数，例如: 10-16片 则分散在 4*4 格子
        this.shuffle_grid_num = Mathf.CeilToInt(Mathf.Sqrt((float)this.all_pieces.Length));

        // 取最大的碎片的格子大小
        Bounds piece_bounds_max = new Bounds(Vector3.zero, Vector3.zero);
        foreach(PieceControl piece in this.all_pieces)
        {
            Bounds bounds = piece.GetBounds(Vector3.zero);
            piece_bounds_max.Encapsulate(bounds);
        }
        // 稍微大一小圈，后面随机小偏移，就不会有那么方正的痕迹
        piece_bounds_max.size *= SHUFFLE_ZONE_SCALE;

        // 所有碎片的洗牌区域范围
        this.shuffle_zone = new Bounds(center, piece_bounds_max.size * this.shuffle_grid_num);
    }

    private void shuffle_pieces()
    {
#if true
        int[] piece_index = new int[this.shuffle_grid_num * this.shuffle_grid_num];

        // 10个碎片会有 4*4 格子，空的格子标记 -1
        for (int i = 0; i < piece_index.Length; i++)
        {
            if (i < this.all_pieces.Length)
            {
                piece_index[i] = i;
            } else
            {
                piece_index[i] = -1;
            }
        }

        // 随机交换两个格子位置
        for (int i = 0; i < piece_index.Length - 1; i++)
        {
            int j = Random.Range(i + 1, piece_index.Length);

            int temp = piece_index[j];
            piece_index[j] = piece_index[i];
            piece_index[i] = temp;
        }

        // 标记数字的格子填充该数字对应的碎片
        Vector3 pitch;
        pitch = this.shuffle_zone.size / (float)this.shuffle_grid_num;
        for(int i = 0; i < piece_index.Length; i++)
        {
            if(piece_index[i] < 0)
            {
                continue;
            }

            PieceControl piece = this.all_pieces[piece_index[i]];

            // 根据格子在总格子的索引，结合中心点位置计算出格子坐标，把碎片坐标设置为那个格子坐标
            Vector3 position = piece.finished_position;

            int ix = i % this.shuffle_grid_num;
            int iz = i / this.shuffle_grid_num;

            position.x = ix * pitch.x;
            position.z = iz * pitch.z;

            // 相对左上角第一个格子的【中心位置】计算，所以多了个 0.5
            position.x += this.shuffle_zone.center.x - pitch.x * (this.shuffle_grid_num / 2.0f - 0.5f);
            position.z += this.shuffle_zone.center.z - pitch.z * (this.shuffle_grid_num / 2.0f - 0.5f);

            position.y = piece.finished_position.y;

            piece.start_position = position;
        }

        // 碎片在自己的小格内做小范围偏移
        Vector3 offset_cycle = pitch / 2.0f;
        Vector3 offset_add = pitch / 5.0f;
        Vector3 offset = Vector3.zero;
        for(int i = 0; i < piece_index.Length; i++)
        {
            if(piece_index[i] < 0)
            {
                continue;
            }
            PieceControl piece = this.all_pieces[piece_index[i]];
            Vector3 position = piece.start_position;
            position.x += offset.x;
            position.z += offset.z;
            piece.start_position = position;

            offset.x += offset_add.x;
            if(offset.x > offset_cycle.x / 2.0f)
            {
                offset.x -= offset_cycle.x;
            }
            offset.z += offset_add.z;
            if(offset.z > offset_cycle.z / 2.0f)
            {
                offset.z -= offset_cycle.z;
            }
        }

        // shuffle_zone 只是一个 bound 所以没办法旋转整个 shuffle_zone 来旋转所有碎片，而是每个碎片单独相对 shuffle_zone 的中心轴作旋转
        foreach(PieceControl piece in this.all_pieces)
        {
            Vector3 position = piece.start_position;
            position -= this.shuffle_zone.center;
            position = Quaternion.AngleAxis(this.pazzle_rotation, Vector3.up) * position;
            position += this.shuffle_zone.center;
            piece.start_position = position;
        }
        // 这里作用是按重置按钮的时候，整体旋转角再变化
        this.pazzle_rotation += 90;

#else
        // 简单的使用随机数来决定坐标的情况
        foreach(PieceControl piece in this.all_pieces)
        {
            Vector3 position;
            Bounds piece_bounds = piece.GetBounds(Vector3.zero);

            position.x = Random.Range(this.shuffle_zone.min.x - piece_bounds.min.x, this.shuffle_zone.max.x - piece_bounds.max.x);
            position.z = Random.Range(this.shuffle_zone.min.x - piece_bounds.min.z, this.shuffle_zone.max.z - piece_bounds.max.z);

            position.y = piece.finished_position.y;
            piece.start_position = position;
        }
#endif
    }

    // ---------------------------------------------------------------------------------------- //

    public void beginRetryAction()
    {
        this.next_state = STATE.PLAY;
    }

    private bool is_piece_object(GameObject game_object)
    {
        bool is_piece = false;
        do
        {
            if (game_object.name.Contains("base"))
            {
                continue;
            }
            is_piece = true;
        } while (false);

        return is_piece;
    }

    public bool isCleared()
    {
        return (this.state == STATE.CLEAR);
    }

    public bool isDispCleared()
    {
        return this.is_disp_cleared;
    }

    // 将被点击的碎片移动到数组的头部，根据数组设置渲染优先级
    public void PickPiece(PieceControl piece)
    {
        int i, j;

        for(i = 0; i < this.active_pieces.Length; i++)
        {
            if(this.active_pieces[i] == null)
            {
                continue;
            }

            if(this.active_pieces[i].name == piece.name)
            {
                for(j = i; j > 0; j--)
                {
                    this.active_pieces[j] = this.active_pieces[j - 1];
                }

                this.active_pieces[0] = piece;

                break;
            }
        }
        this.set_height_offset_to_pieces();
    }

    private void set_height_offset_to_pieces()
    {
        float offset = 0.01f;
        int n = 0;

        foreach(PieceControl piece in this.active_pieces)
        {
            if(piece == null)
            {
                continue;
            }

            piece.GetComponent<Renderer>().material.renderQueue = this.GetDrawPriorityPiece(n);

            offset -= 0.01f / this.piece_num;
            piece.SetHeightOffset(offset);
            n++;
        }
    }

    private int GetDrawPriorityPiece(int priority_in_pieces)
    {
        int priority;

        priority = this.GetDrawPriorityRetryButton() + 1;
        priority += this.piece_num - 1 - priority_in_pieces;

        return priority;
    }

    private int GetDrawPriorityRetryButton()
    {
        int priority;
        priority = this.GetDrawPriorityFinishedPiece() + 1;
        return priority;
    }

    private int GetDrawPriorityFinishedPiece()
    {
        int priority;
        priority = this.GetDrawPriorityBase() + 1;
        return priority;
    }

    private int GetDrawPriorityBase()
    {
        return (0);
    }

    // 碎片被放置到正解位置时的处理
    public void FinishPiece(PieceControl piece)
    {
        int i, j;
        piece.GetComponent<Renderer>().material.renderQueue = this.GetDrawPriorityFinishedPiece();

        for (i = 0; i < this.active_pieces.Length; i++)
        {
            if (this.active_pieces[i] == null)
            {
                continue;
            }

            if (this.active_pieces[i].name == piece.name)
            {
                for (j = i; j < this.active_pieces.Length - 1; j++)
                {
                    this.active_pieces[j] = this.active_pieces[j + 1];
                }

                this.active_pieces[this.active_pieces.Length - 1] = null;

                this.piece_finished_num++;

                break;
            }
        }
    }

    // ---------------------------------------------------------------------------------------- //
    // 开发时辅助画洗牌区
    public static void DrawBounds(Bounds bounds)
    {
        Vector3[] square = new Vector3[4];

        square[0] = new Vector3(bounds.min.x, 0.0f, bounds.min.z);
        square[1] = new Vector3(bounds.max.x, 0.0f, bounds.min.z);
        square[2] = new Vector3(bounds.max.x, 0.0f, bounds.max.z);
        square[3] = new Vector3(bounds.min.x, 0.0f, bounds.max.z);

        for(int i = 0; i < 4; i++)
        {
            Debug.DrawLine(square[i], square[(i + 1) % 4], Color.white, 0.0f, false);
        }
    }
}
