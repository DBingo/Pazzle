using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshCollider))]
public class PieceControl : MonoBehaviour
{
    private GameObject game_camera;
    public PazzleControl pazzle_control = null;

    private static bool IS_ENABLE_GRAB_OFFSET = true;

    private const float HEIGHT_OFFSET_BASE = 0.1f;
    private const float SNAP_SPEED_MIN = 0.01f * 60.0f;
    private const float SNAP_SPEED_MAX = 0.8f * 60.0f;

    private Vector3 grab_offset = Vector3.zero;
    private bool is_now_dragging = false;

    public Vector3 finished_position;
    public Vector3 start_position;

    public float height_offset = 0.0f;

    static float SNAP_DISTANCE = 0.5f;

    private Color origin_color;

    enum STATE
    {
        NONE = -1,
        IDLE = 0,
        DRAGING,
        FINISHED,
        RESTART,
        SNAPPING,
        NUM,
    }

    private STATE state = STATE.NONE;
    private STATE next_state = STATE.NONE;

    private Vector3 snap_target;
    private STATE next_state_snap;

    private void Awake()
    {
        this.finished_position = this.transform.position;
        this.start_position = this.finished_position;
        this.origin_color = this.GetComponent<Renderer>().material.color;
    }

    void Start()
    {
        this.game_camera = GameObject.FindGameObjectWithTag("MainCamera");
    }
    
    void Update()
    {
        PazzleControl.DrawBounds(this.GetBounds(Vector3.zero));


        Color color = this.origin_color;

        switch (this.state)
        {
            case STATE.NONE:
                {
                    this.next_state = STATE.RESTART;
                }
                break;
            case STATE.IDLE:
                {
                    if (this.is_now_dragging)
                    {
                        this.next_state = STATE.DRAGING;
                    }
                }
                break;
            case STATE.DRAGING:
                {
                    // 鼠标放开时
                    if (!this.is_now_dragging)
                    {
                        // 在吸附范围内
                        if (this.is_in_snap_range())
                        {
                            this.next_state = STATE.SNAPPING;
                            this.snap_target = this.finished_position;
                            this.next_state_snap = STATE.FINISHED;
                        }
                        // 在吸附范围外
                        else
                        {
                            this.next_state = STATE.IDLE;
                        }
                    }
                }
                break;
            case STATE.SNAPPING:
                {
                    if((this.transform.position - this.snap_target).magnitude < 0.0001f)
                    {
                        this.next_state = this.next_state_snap;
                    }
                }
                break;
        }

        if(this.next_state != STATE.NONE)
        {
            switch (this.next_state)
            {
                //case STATE.IDLE:
                //    {
                //        // this.SetHeightOffset(this.height_offset);
                //    }
                //    break;
                case STATE.RESTART:
                    {
                        this.transform.position = this.start_position;
                        // this.SetHeightOffset(this.height_offset);
                        this.next_state = STATE.IDLE;
                    }
                    break;
                case STATE.DRAGING:
                    {
                        this.begin_dragging();
                        this.pazzle_control.PickPiece(this);
                    }
                    break;
                case STATE.FINISHED:
                    {
                        this.transform.position = this.finished_position;
                        this.pazzle_control.FinishPiece(this);
                    }
                    break;
            }

            this.state = this.next_state;
            this.next_state = STATE.NONE;
        }

        this.transform.localScale = Vector3.one;

        switch (this.state)
        {
            case STATE.DRAGING:
                {
                    this.do_dragging();

                    if (this.is_in_snap_range())
                    {
                        color = Color.Lerp(color, Color.white, .5f);   
                    }

                    this.transform.localScale = Vector3.one * 1.1f;
                }
                break;

            case STATE.SNAPPING:
                {
                    Vector3 next_position, distance, move;
                    distance = this.snap_target - this.transform.position;

                    distance *= 0.25f * (60.0f * Time.deltaTime);

                    next_position = this.transform.position + distance;
                    move = next_position - this.transform.position;

                    float snap_speed_min = PieceControl.SNAP_SPEED_MIN * Time.deltaTime;
                    float snap_speed_max = PieceControl.SNAP_SPEED_MAX * Time.deltaTime;

                    if(move.magnitude < snap_speed_min)
                    {
                        this.transform.position = this.snap_target;
                    }
                    else
                    {
                        if(move.magnitude > snap_speed_max)
                        {
                            move *= snap_speed_max / move.magnitude;
                            next_position = this.transform.position + move;
                        }
                        this.transform.position = next_position;
                    }
                }
                break;
        }

        this.GetComponent<Renderer>().material.color = color;

    }

    private void begin_dragging()
    {
        do
        {
            Vector3 world_position;

            if(!this.unproject_mouse_position(out world_position, Input.mousePosition))
            {
                break;
            }
            if (PieceControl.IS_ENABLE_GRAB_OFFSET) {
                this.grab_offset = this.transform.position - world_position;
            }

        } while (false);
    }

    private void do_dragging()
    {
        do
        {
            Vector3 world_position;

            if (!this.unproject_mouse_position(out world_position, Input.mousePosition))
            {
                break;
            }

            this.transform.position = world_position + this.grab_offset;

        } while (false);
    }

    public bool unproject_mouse_position(out Vector3 world_position, Vector3 mouse_position)
    {
        bool ret;
        float depth;

        Plane plane = new Plane(Vector3.up, new Vector3(0.0f, this.transform.position.y, 0.0f));

        Ray ray = this.game_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);

        if(plane.Raycast(ray, out depth))
        {
            world_position = ray.origin + ray.direction * depth;  // 可以试试 ray.GetPoint(enter);
            ret = true;
        } else
        {
            world_position = Vector3.zero;
            ret = false;
        }

        return ret;
    }

    public void SetHeightOffset(float height_offset)
    {
        Vector3 position = this.transform.position;

        this.height_offset = 0.0f;

        if(this.state != STATE.FINISHED || this.next_state != STATE.FINISHED)
        {
            this.height_offset = height_offset;
            position.y = this.finished_position.y + PieceControl.HEIGHT_OFFSET_BASE;
            position.y += this.height_offset;

            this.transform.position = position;
        }
    }

    private bool is_in_snap_range()
    {
        bool ret = false;

        if(Vector3.Distance(this.transform.position, this.finished_position) < PieceControl.SNAP_DISTANCE)
        {
            ret = true;
        }

        return ret;
    }

    private void OnMouseDown()
    {
        this.is_now_dragging = true;
    }

    private void OnMouseUp()
    {
        this.is_now_dragging = false;
    }

    public void Restart()
    {
        this.next_state = STATE.RESTART;
    }

    public Bounds GetBounds(Vector3 center)
    {
        // 由于 Mesh 不是 Component ，无法执行 GetComponent<Mesh>()
        Bounds bounds = this.GetComponent<MeshFilter>().mesh.bounds;

        bounds.center = center;

        return bounds;
    }
}
