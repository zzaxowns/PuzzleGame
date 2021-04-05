using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BlockControl.cs
public class Block
{ // 블록에 관한 정보를 다룬다.
    public static float COLLISION_SIZE = 1.0f; // 블록의 충돌 크기.
    public static float VANISH_TIME = 3.0f; // 불 붙고 사라질 때까지의 시간.
    public struct iPosition
    { // 그리드에서의 좌표를 나타내는 구조체.
        public int x; // X 좌표.
        public int y; // Y 좌표.
    }
    public enum COLOR
    { // 블록 색상.
        NONE = -1, // 색 지정 없음.
        PINK = 0, BLUE, YELLOW, GREEN, // 분홍색, 파란색, 노란색, 녹색.
        MAGENTA, ORANGE, GRAY, // 마젠타, 주황색, 그레이.
        NUM, // 컬러가 몇 종류인지 나타낸다(=7).
        FIRST = PINK, LAST = ORANGE, // 초기 컬러(분홍색), 최종 컬러(주황색).
        NORMAL_COLOR_NUM = GRAY, // 보통 컬러(회색 이외의 색)의 수.
    };
    public enum DIR4
    { // 상하좌우 네 방향.
        NONE = -1, // 방향지정 없음.
        RIGHT, LEFT, UP, DOWN, // 우. 좌, 상, 하.
        NUM, // 방향이 몇 종류 있는지 나타낸다(=4).
    };
    public static int BLOCK_NUM_X = 9; // 블록을 배치할 수 있는 X방향 최대수.
    public static int BLOCK_NUM_Y = 9; // 블록을 배치할 수 있는 Y방향 최대수.

    public enum STEP
    {
        // 상태 정보 없음, 대기 중, 잡혀 있음, 떨어진 순간, 슬라이드 중, 소멸 중, 재생성 중, 낙하 중, 크게 슬라이드 중, 상태 종류.
        NONE = -1, IDLE = 0, GRABBED, RELEASED, SLIDE, VACANT, RESPAWN, FALL, LONG_SLIDE, NUM,
    };
}

public class BlockControl : MonoBehaviour
{
    public Block.COLOR color = (Block.COLOR)0; // 블록 색.
    public BlockRoot block_root = null; // 블록의 신.
    public Block.iPosition i_pos;

    public Block.STEP step = Block.STEP.NONE; // 지금 상태.
    public Block.STEP next_step = Block.STEP.NONE; // 다음 상태.
    private Vector3 position_offset_initial = Vector3.zero; // 교체 전 위치.
    public Vector3 position_offset = Vector3.zero; // 교체 후 위치.

    public float vanish_timer = -1.0f; // 블록이 사라질 때까지의 시간.
    public Block.DIR4 slide_dir = Block.DIR4.NONE; // 슬라이드된 방향.
    public float step_timer = 0.0f; // 블록이 교체된 때의 이동시간 등.

    // Start is called before the first frame update
    void Start()
    {
        this.setColor(this.color);
        this.next_step = Block.STEP.IDLE;
    } // 색칠을 한다.
      // 인수 color의 색으로 블록을 칠한다.


    // Update is called once per frame
    void Update()
    {
        Vector3 mouse_position; // 마우스 위치.
        this.block_root.unprojectMousePosition(out mouse_position, Input.mousePosition); // 마우스 위치 획득.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y); // 획득한 마우스 위치를 X와 Y만으로 한다.

        this.step_timer += Time.deltaTime;
        float slide_time = 0.2f;
        if (this.next_step == Block.STEP.NONE)
        { // '상태 정보 없음'의 경우.
            switch (this.step)
            {
                case Block.STEP.SLIDE:
                    if (this.step_timer >= slide_time)
                    { // 슬라이드 중인 블록이 소멸되면 VACANT(사라진) 상태로 이행.
                        if (this.vanish_timer == 0.0f)
                        {
                            this.next_step = Block.STEP.VACANT;
                        }
                        else
                        { // vanish_timer가 0이 아니면 IDLE(대기) 상태로 이행.
                            this.next_step = Block.STEP.IDLE;
                        }
                    }
                    break;
            }
        }

        while (this.next_step != Block.STEP.NONE)
        { // '다음 블록' 상태가 '정보 없음' 이외인 동안. ＝> '다음 블록' 상태가 변경된 경우.
            this.step = this.next_step;
            this.next_step = Block.STEP.NONE;
            switch (this.step)
            {
                case Block.STEP.IDLE: // '대기' 상태.
                    this.position_offset = Vector3.zero;
                    this.transform.localScale = Vector3.one * 1.0f; break; // 블록 표시 크기를 보통 크기로 한다.
                case Block.STEP.GRABBED: // '잡힌' 상태.
                    this.transform.localScale = Vector3.one * 1.2f; break; // 블록 표시 크기를 크게 한다.
                case Block.STEP.RELEASED: // '떨어져 있는' 상태.
                    this.position_offset = Vector3.zero;
                    this.transform.localScale = Vector3.one * 1.0f; break;
                case Block.STEP.VACANT:
                    this.position_offset = Vector3.zero; break;
            }
            this.step_timer = 0.0f;
        }

        switch (this.step){
            case Block.STEP.GRABBED: // 잡힌 상태.
                this.slide_dir = this.calcSlideDir(mouse_position_xy); break; // 잡힌 상태일 때는 항상 슬라이드 방향을 체크.
            case Block.STEP.SLIDE: // 슬라이드(교체) 중.
                float rate = this.step_timer / slide_time; // 블록을 서서히 이동하는 처리.
                rate = Mathf.Min(rate, 1.0f);
                rate = Mathf.Sin(rate * Mathf.PI / 2.0f);
                this.position_offset = Vector3.Lerp(this.position_offset_initial, Vector3.zero, rate); break;
        }
        // 블록 표시 크기를 보통 사이즈로 한다.
        // 그리드 좌표를 실제 좌표(씬의 좌표)로 변환하고.
        Vector3 position = BlockRoot.calcBlockPosition(this.i_pos) + this.position_offset; // position_offset을 추가한다.
        this.transform.position = position; // 실제 위치를 새로운 위치로 변경한다.
    }

    public void setColor(Block.COLOR color)
    {
        this.color = color; // 이번에 지정된 색을 멤버 변수에 보관한다.
        Color color_value; // Color 클래스는 색을 나타낸다.


        switch (this.color)
        { // 칠할 색에 따라서 갈라진다.
            default:
            case Block.COLOR.PINK:
                color_value = new Color(1.0f, 0.5f, 0.5f); break;
            case Block.COLOR.BLUE:
                color_value = Color.blue; break;
            case Block.COLOR.YELLOW:
                color_value = Color.yellow; break;
            case Block.COLOR.GREEN:
                color_value = Color.green; break;
            case Block.COLOR.MAGENTA:
                color_value = Color.magenta; break;
            case Block.COLOR.ORANGE:
                color_value = new Color(1.0f, 0.46f, 0.0f); break;
        }
        this.GetComponent<Renderer>().material.color = color_value; // 이 게임 오브젝트의 색상을 변경한다.
    }

    public void beginGrab()
    { // 잡혔을 때 호출한다.
        this.next_step = Block.STEP.GRABBED;
    }
    public void endGrab()
    { // 놓았을 때 호출한다.
        this.next_step = Block.STEP.IDLE;
    }
    public bool isGrabbable()
    { // 잡을 수 있는 상태 인지 판단한다.
        bool is_grabbable = false;
        switch (this.step)
        {
            case Block.STEP.IDLE: // '대기' 상태일 때만.
                is_grabbable = true; // true(잡을 수 있다)를 반환한다.
                break;
        }
        return (is_grabbable);
    }

    public bool isContainedPosition(Vector2 position)
    { // 지정된 마우스 좌표가 자신과 겹치는지 반환한다.
        bool ret = false;
        Vector3 center = this.transform.position;
        float h = Block.COLLISION_SIZE / 2.0f;
        do
        {
            if (position.x < center.x - h || center.x + h < position.x) { break; } // X 좌표가 자신과 겹치지 않으면 루프를 빠져 나간다.
            if (position.y < center.y - h || center.y + h < position.y) { break; } // Y 좌표가 자신과 겹치지 않으면 루프를 빠져 나간다.
            ret = true; // X 좌표, Y 좌표 모두 겹쳐 있으면 true(겹쳐 있다)를 반환한다.
        } while (false);
        return (ret);
    }

    public Block.DIR4 calcSlideDir(Vector2 mouse_position)
    { // 마우스 위치를 바탕으로 슬라이드된 방향을 구한다.
        Block.DIR4 dir = Block.DIR4.NONE;
        Vector2 v = mouse_position - new Vector2(this.transform.position.x, this.transform.position.y); // 지정된 mouse_position과 현재 위치의 차를 나타냄.
        if (v.magnitude > 0.1f)
        { // 벡터의 크기가 0.1보다 크면. 그보다 작으면 슬라이드하지 않은 걸로 간주한다.
            if (v.y > v.x)
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.UP;
                }
                else { dir = Block.DIR4.LEFT; }
            }
            else
            {
                if (v.y > -v.x)
                {
                    dir = Block.DIR4.RIGHT;
                }
                else { dir = Block.DIR4.DOWN; }
            }
        }
        return (dir);
    }

    public float calcDirOffset(Vector2 position, Block.DIR4 dir)
    { // 현재 위치와 슬라이드할 곳의 거리가 어느 정도인가 반환한다.
        float offset = 0.0f;
        Vector2 v = position - new Vector2(this.transform.position.x, this.transform.position.y); // 지정된 위치와 블록의 현재 위치의 차를 나타내는 벡터.
        switch (dir)
        { // 지정된 방향에 따라 갈라진다.
            case Block.DIR4.RIGHT: offset = v.x; break;
            case Block.DIR4.LEFT: offset = -v.x; break;
            case Block.DIR4.UP: offset = v.y; break;
            case Block.DIR4.DOWN: offset = -v.y; break;
        }
        return (offset);
    }

    public void beginSlide(Vector3 offset)
    { // 이동 시작을 알리는 메서드
        this.position_offset_initial = offset;
        this.position_offset = this.position_offset_initial;
        this.next_step = Block.STEP.SLIDE; // 상태를 SLIDE로 변경.
    }
}
