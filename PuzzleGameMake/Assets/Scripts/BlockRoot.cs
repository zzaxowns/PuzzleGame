using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRoot : MonoBehaviour
{
    public GameObject BlockPrefab = null; // 만들어낼 블록의 프리팹.
    public BlockControl[,] blocks; // 그리드.

    private GameObject main_camera = null; // 메인 카메라.
    private BlockControl grabbed_block = null; // 잡은 블록.

    public void fallBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)
    { // 낙하했을 때 위아래 블록을 교체한다.
      // block0과 block1의 색, 크기, 사라질 때까지 걸리는 시간, 표시, 비표시, 상태를 기록.
        Block.COLOR color0 = block0.color;
        Block.COLOR color1 = block1.color;
        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;
        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;
        bool visible0 = block0.isVisible();
        bool visible1 = block1.isVisible();
        Block.STEP step0 = block0.step;
        Block.STEP step1 = block1.step;
        // block0과 block1의 각종 속성을 교체한다.
        block0.setColor(color1);
        block1.setColor(color0);
        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;
        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;
        block0.setVisible(visible1);
        block1.setVisible(visible0);
        block0.step = step1;
        block1.step = step0;
        block0.beginFall(block1);
    }

    private bool is_has_sliding_block_in_column(int x)
    { // 지정된 그리드 좌표의 열(세로 줄)에 슬라이드 중인 블록이 하나라도 있으면, true를 반환한다.
        bool ret = false;
        for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
        {
            if (this.blocks[x, y].isSliding())
            { // 슬라이드 중인 블록이 있으면,
                ret = true; // true를 반환한다. 
                break;
            }
        }
        return (ret);
    }

    private ScoreCounter score_counter = null; // 점수 카운터 ScoreCounter.
    protected bool is_vanishing_prev = false; // 앞에서 발화했는가?

    public TextAsset levelData = null; // 레벨 데이터의 텍스트를 저장.
    public LevelControl level_control; // LevelControl를 저장.

    // Start is called before the first frame update
    void Start()
    {
        this.main_camera = GameObject.FindGameObjectWithTag("MainCamera");

        this.score_counter = this.gameObject.GetComponent<ScoreCounter>();
    } // 카메라로부터 마우스 커서를 통과하는 광선을 쏘기 위해서 필요


    // Update is called once per frame
    void Update()
    {
        Vector3 mouse_position; // 마우스 위치.
        this.unprojectMousePosition(out mouse_position, Input.mousePosition); // 마우스 위치를 가져온다.
        Vector2 mouse_position_xy = new Vector2(mouse_position.x, mouse_position.y); // 가져온 마우스 위치를 하나의 Vector2로 모은다.

        if (this.grabbed_block == null)
        {
            Debug.Log("들어가기 전");

            if (is_has_falling_block())
            {

                // 잡은 블록이 비었으면.
                // if(!this.is_has_falling_block()) { // 나중에 주석 해제
                if (Input.GetMouseButtonDown(0))
                { // 마우스 버튼이 눌렸으면
                    foreach (BlockControl block in this.blocks)
                    { // blocks 배열의 모든 요소를 차례로 처리한다.
                        if (!block.isGrabbable())
                        { // 블록을 잡을 수 없다면.
                            continue;
                        } // 루프의 처음으로 점프한다.
                        if (!block.isContainedPosition(mouse_position_xy))
                        { // 마우스 위치가 블록 영역 안이 아니면.
                            continue;
                        } // 루프의 처음으로 점프한다.
                        this.grabbed_block = block; // 처리 중인 블록을 grabbed_block에 등록한다.
                        this.grabbed_block.beginGrab(); break;
                    } // 잡았을 때의 처리를 실행한다.
                }
            }// }
        }
        else
        {
            do
            {
                BlockControl swap_target = this.getNextBlock(grabbed_block, grabbed_block.slide_dir); // 슬라이드할 곳의 블록을 가져온다.
                if (swap_target == null)
                { // 슬라이드할 곳 블록이 비어 있으면.
                    break;
                } // 루프 탈출.
                if (!swap_target.isGrabbable())
                { // 슬라이드할 곳의 블록이 잡을 수 있는 상태가 아니라면.
                    break;
                } // 루프 탈출.
                  // 현재 위치에서 슬라이드 위치까지의 거리를 얻는다.
                float offset = this.grabbed_block.calcDirOffset(mouse_position_xy, this.grabbed_block.slide_dir);
                if (offset < Block.COLLISION_SIZE / 2.0f)
                { // 수리 거리가 블록 크기의 절반보다 작다면.
                    break;
                } // 루프 탈출.
                this.swapBlock(grabbed_block, grabbed_block.slide_dir, swap_target); // 블록을 교체한다.
                this.grabbed_block = null; // 지금은 블록을 잡고 있지 않다.
            } while (false);

            // 블록을 잡았을 때.
            if (!Input.GetMouseButton(0))
            { // 마우스 버튼이 눌려져 있지 않으면.
                this.grabbed_block.endGrab(); // 블록을 놨을 때의 처리를 실행.
                this.grabbed_block = null;
            } // grabbed_block을 비우게 설정.
        }

        // 낙하 중 또는 슬라이드 중이면.
        if (this.is_has_falling_block() || this.is_has_sliding_block())
        {
            // 아무것도 하지 않는다.
            // 낙하 중도 슬라이드 중도 아니면.
        }
        else
        {
            int ignite_count = 0; // 불붙은 개수.
                                  // 그리드 안의 모든 블록에 대해서 처리.
            foreach (BlockControl block in this.blocks)
            {
                if (!block.isIdle())
                { // 대기 중이면 루프의 처음으로 점프하고.
                    continue; // 다음 블록을 처리한다.
                }
                // 세로 또는 가로에 같은 색 블록이 세 개 이상 나열했다면.
                if (this.checkConnection(block))
                {
                    ignite_count++; // 불붙은 개수를 증가.
                }
            }
            if (ignite_count > 0)
            {
                if (!this.is_vanishing_prev)
                {
                    this.score_counter.clearIgniteCount(); // 연속 점화가 아니라면, 점화 횟수를 리셋.
                }
                this.score_counter.addIgniteCount(ignite_count);// 점화 횟수를 증가.
                this.score_counter.updateTotalScore(); // 합계 점수 갱신.

                // 불붙은 개수가 0보다 크면 ＝ 한 군데라도 맞춰진 곳이 있으면.
                int block_count = 0; // 불붙는 중인 블록 수(다음 장에서 사용한다).
                                     // 그리드 내의 모든 블록에 대해서 처리.
                foreach (BlockControl block in this.blocks)
                {
                    if (block.isVanishing())
                    { // 타는 중이면.
                        block.rewindVanishTimer(); // 다시 점화!.
                        block_count++;
                    }
                }
            }
        }

        bool is_vanishing = this.is_has_vanishing_block(); // 하나라도 연소 중인 블록이 있는가?.

        do
        {
            if (is_vanishing) { break; } // 연소 중인 블록이 있다면, 낙하 처리를 실행하지 않는다.
            if (this.is_has_sliding_block()) { break; } // 교체 중인 블록이 있다면, 낙하 처리를 실행하지 않는다.
            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
            {
                if (this.is_has_sliding_block_in_column(x))
                { // 열에 교체 중인 블록이 있다면 그 열은 처리하지 않고 다음 열로 진행.
                    continue;
                }
                for (int y = 0; y < Block.BLOCK_NUM_Y - 1; y++)
                {// 그 열에 있는 블록을 위에서부터 검사한다.
                    if (!this.blocks[x, y].isVacant())
                    { // 지정 블록이 비표시라면 다음 블록으로.
                        continue;
                    }
                    for (int y1 = y + 1; y1 < Block.BLOCK_NUM_Y; y1++)
                    { // 지정 블록 아래에 있는 블록을 검사.
                        if (this.blocks[x, y1].isVacant()) { continue; } // 아래에 있는 블록이 비표시라면 다음 블록으로.
                        this.fallBlock(this.blocks[x, y], Block.DIR4.UP, this.blocks[x, y1]); // 블록을 교체한다.
                        break;
                    }
                }
            }
            for (int x = 0; x < Block.BLOCK_NUM_X; x++)
            { // 보충처리.
                int fall_start_y = Block.BLOCK_NUM_Y;
                for (int y = 0; y < Block.BLOCK_NUM_Y; y++)
                {
                    if (!this.blocks[x, y].isVacant()) { continue; } // 비표시 블록이 아니라면 다음 블록으로.
                    this.blocks[x, y].beginRespawn(fall_start_y); // 블록 부활.
                    fall_start_y++;
                }
            }
        } while (false);

        this.is_vanishing_prev = is_vanishing;
    }

    // 지정된 그리드 좌표로 씬에서의 좌표를 구한다.
    public static Vector3 calcBlockPosition(Block.iPosition i_pos)
    {
         // 배치할 왼쪽 위 구석 위치를 초기값으로 설정한다.
         Vector3 position = new Vector3(-(Block.BLOCK_NUM_X / 2.0f - 0.5f), -(Block.BLOCK_NUM_Y / 2.0f - 0.5f), 0.0f);
        // 초깃값 + 그리드 좌표 × 블록 크기.
         position.x += (float)i_pos.x * Block.COLLISION_SIZE;
        position.y += (float)i_pos.y * Block.COLLISION_SIZE;
         return (position); // 씬에서의 좌표를 반환한다.
    }

    // 블록을 만들어 내고 가로 9칸, 세로 9칸에 배치한다.
    public void initialSetUp()
        {
            this.blocks = new BlockControl[Block.BLOCK_NUM_X, Block.BLOCK_NUM_Y]; // 그리드의 크기를 9×9로 한다.
            int color_index = 0; // 블록의 색 번호.
            Block.COLOR color = Block.COLOR.FIRST;

        for (int y = 0; y < Block.BLOCK_NUM_Y; y++) { // 처음~마지막행
                for (int x = 0; x < Block.BLOCK_NUM_X; x++)
                { // 왼쪽~오른쪽
                 // BlockPrefab의 인스턴스를 씬에 만든다.
                     GameObject game_object = Instantiate(this.BlockPrefab) as GameObject;
                     BlockControl block = game_object.GetComponent<BlockControl>(); // 블록의 BlockControl 클래스를 가져온다.
                     this.blocks[x, y] = block; // 블록을 그리드에 저장한다.
                     block.i_pos.x = x; // 블록의 위치 정보(그리드 좌표)를 설정한다.
                     block.i_pos.y = y;
                     block.block_root = this; // 각 BlockControl이 연계할 GameRoot는 자신이라고 설정한다.

                    Vector3 position = BlockRoot.calcBlockPosition(block.i_pos); // 그리드 좌표를 실제 위치(씬의 좌표)로 변환
                    block.transform.position = position; // 씬의 블록 위치를 이동한다.

                    //block.setColor((Block.COLOR)color_index); // 블록의 색을 변경한다.
                    // 블록의 이름을 설정(후술)한다. 나중에 블록 정보 확인때 필요.

                    // 현재 출현 확률을 바탕으로 색을 결정한다.
                    color = this.selectBlockColor();
                    block.setColor(color);

                    block.name = "block(" + block.i_pos.x.ToString() + "," + block.i_pos.y.ToString() + ")";
                    // 전체 색 중에서 임의로 하나의 색을 선택한다.
                    color_index = Random.Range(0, (int)Block.COLOR.NORMAL_COLOR_NUM);
                }
        }
    }

    // ref와 out 둘 다 값을 복사하지 않고, 참조를 통해 매개 변수를 직접 변경함, ref는 초기화가 필요하나, out은 불필요함.
    // 때문에, out으로 받아온 매개 변수는 함수 안에서 대입이 필요함
    public bool unprojectMousePosition(out Vector3 world_position, Vector3 mouse_position)
    {
        bool ret;
        // 판을 작성한다. 이 판은 카메라에 대해서 뒤로 향해서(Vector3.back).
        // 블록의 절반 크기만큼 앞에 둔다.
        Plane plane = new Plane(Vector3.back, new Vector3(0.0f, 0.0f, -Block.COLLISION_SIZE / 2.0f));
        // 카메라와 마우스를 통과하는 빛을 만든다.
        Ray ray = this.main_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);
        float depth;
        // 광선(ray)이 판(plane)에 닿았다면,
        if (plane.Raycast(ray, out depth))
        { // depth에 정보가 기록된다.
          // 인수 world_position을 마우스 위치로 덮어쓴다.
            world_position = ray.origin + ray.direction * depth;
            ret = true;
        }
        else
        { // 닿지 않았다면.
          // 인수 world_position을 0인 벡터로 덮어쓴다.
            world_position = Vector3.zero;
            ret = false;
        }
        return (ret); // 카메라를 통과하는 광선이 블록에 닿았는지를 반환
    }

    public BlockControl getNextBlock(BlockControl block, Block.DIR4 dir)
    { // 인수로 지정된 블록과 방향으로 블록이 슬라이드할 곳에 어느 블록이 있는지 반환.
        BlockControl next_block = null; // 슬라이드할 곳의 블록을 여기에 저장.
        switch (dir)
        {
            case Block.DIR4.RIGHT:
                if (block.i_pos.x < Block.BLOCK_NUM_X - 1)
                { // 그리드 안이라면.
                    next_block = this.blocks[block.i_pos.x + 1, block.i_pos.y];
                }
                break;
            case Block.DIR4.LEFT:
                if (block.i_pos.x > 0)
                { // 그리드 안이라면.
                    next_block = this.blocks[block.i_pos.x - 1, block.i_pos.y];
                }
                break;
            case Block.DIR4.UP:
                if (block.i_pos.y < Block.BLOCK_NUM_Y - 1)
                { // 그리드 안이라면.
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y + 1];
                }
                break;
            case Block.DIR4.DOWN:
                if (block.i_pos.y > 0)
                { // 그리드 안이라면.
                    next_block = this.blocks[block.i_pos.x, block.i_pos.y - 1];
                }
                break;
        }
        return (next_block);
    }

    public static Vector3 getDirVector(Block.DIR4 dir)
    { // 인수로 지정된 방향으로 현재 블록에서 지정 방향으로 이동하는 양 반환.
        Vector3 v = Vector3.zero;
        switch (dir)
        {
            case Block.DIR4.RIGHT: v = Vector3.right; break; // 오른쪽으로 1단위 이동.
            case Block.DIR4.LEFT: v = Vector3.left; break; // 왼쪽으로 1단위 이동.
            case Block.DIR4.UP: v = Vector3.up; break; // 위로 1단위 이동.
            case Block.DIR4.DOWN: v = Vector3.down; break; // 아래로 1단위 이동.
        }
        v *= Block.COLLISION_SIZE; // 블록의 크기를 곱한다.
        return (v);
    }

    public static Block.DIR4 getOppositDir(Block.DIR4 dir)
    { // 블록을 서로 교체하기 위해 인수로 지정된 방향의 반대 방향을 반환.
        Block.DIR4 opposit = dir;
        switch (dir)
        {
            case Block.DIR4.RIGHT: opposit = Block.DIR4.LEFT; break;
            case Block.DIR4.LEFT: opposit = Block.DIR4.RIGHT; break;
            case Block.DIR4.UP: opposit = Block.DIR4.DOWN; break;
            case Block.DIR4.DOWN: opposit = Block.DIR4.UP; break;
        }
        return (opposit);
    }
    public void swapBlock(BlockControl block0, Block.DIR4 dir, BlockControl block1)
    { // 실제로 블록을 교체.
      // 각각의 블록 색을 기억해 둔다.
        Block.COLOR color0 = block0.color;
        Block.COLOR color1 = block1.color;
        // 각각의 블록의 확대율을 기억해 둔다.
        Vector3 scale0 = block0.transform.localScale;
        Vector3 scale1 = block1.transform.localScale;
        // 각각의 블록의 '사라지는 시간'을 기억해 둔다.
        float vanish_timer0 = block0.vanish_timer;
        float vanish_timer1 = block1.vanish_timer;
        // 각각의 블록의 이동할 곳을 구한다.
        Vector3 offset0 = BlockRoot.getDirVector(dir);
        Vector3 offset1 = BlockRoot.getDirVector(BlockRoot.getOppositDir(dir));
        // 색을 교체한다.
        block0.setColor(color1);
        block1.setColor(color0);
        // 확대율을 교체한다.
        block0.transform.localScale = scale1;
        block1.transform.localScale = scale0;
        // '사라지는 시간'을 교체한다.
        block0.vanish_timer = vanish_timer1;
        block1.vanish_timer = vanish_timer0;
        block0.beginSlide(offset0); // 원래 블록 이동을 시작한다.
        block1.beginSlide(offset1); // 이동할 위치의 블록 이동을 시작한다.
    }

    // 인수로 받은 블록이 세 개의 블록 안에 들어가는 지 파악하는 메서드
    public bool checkConnection(BlockControl start)
    {
        bool ret = false;
        int normal_block_num = 0;
        if (!start.isVanishing())
        { // 인수인 블록이 불붙은 다음이 아니면.
            normal_block_num = 1;
        }
        int rx = start.i_pos.x; // 그리드 좌표를 기억해 둔다.
        int lx = start.i_pos.x;
        for (int x = lx - 1; x > 0; x--)
        { // 블록의 왼쪽을 검사.
            BlockControl next_block = this.blocks[x, start.i_pos.y];
            if (next_block.color != start.color) { break; } // 색이 다르면, 루프를 빠져나간다.
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; } // 낙하 중이면, 루프를 빠져나간다.
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; } // 슬라이드 중이면, 루프를 빠져나간다.
            if (!next_block.isVanishing())
            { // 불붙은 상태가 아니면
                normal_block_num++;
            } // 검사용 카운터를 증가.
            lx = x;
        }
        for (int x = rx + 1; x < Block.BLOCK_NUM_X; x++)
        { // 블록의 오른쪽을 검사.
            BlockControl next_block = this.blocks[x, start.i_pos.y];
            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }
            if (!next_block.isVanishing()) { normal_block_num++; }
            rx = x;
        }
        do
        {
            if (rx - lx + 1 < 3) { break; } // 오른쪽 블록의 그리드 번호 - 왼쪽 블록의 그리드 번호 + 중앙 블록(1)을 더한 수가 3 미만이면, 루프 탈출.
            if (normal_block_num == 0) { break; } // 불붙지 않은 블록이 하나도 없으면, 루프 탈출.
            for (int x = lx; x < rx + 1; x++)
            { // 나열된 같은 색 블록을 불붙은 상태로.
                this.blocks[x, start.i_pos.y].toVanishing();
                ret = true;
            }
        } while (false);

        normal_block_num = 0;

        if (!start.isVanishing())
        {
            normal_block_num = 1;
        }

        int uy = start.i_pos.y;
        int dy = start.i_pos.y;

        for (int y = dy - 1; y > 0; y--)
        { // 블록의 위쪽을 검사.
            BlockControl next_block = this.blocks[start.i_pos.x, y];
            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }
            if (!next_block.isVanishing()) { normal_block_num++; }
            dy = y;
        }

        for (int y = uy + 1; y < Block.BLOCK_NUM_Y; y++)
        { // 블록의 아래쪽을 검사.
            BlockControl next_block = this.blocks[start.i_pos.x, y];
            if (next_block.color != start.color) { break; }
            if (next_block.step == Block.STEP.FALL || next_block.next_step == Block.STEP.FALL) { break; }
            if (next_block.step == Block.STEP.SLIDE || next_block.next_step == Block.STEP.SLIDE) { break; }
            if (!next_block.isVanishing()) { normal_block_num++; }
            uy = y;
        }

        do
        {
            if (uy - dy + 1 < 3) { break; }
            if (normal_block_num == 0) { break; }
            for (int y = dy; y < uy + 1; y++)
            {
                this.blocks[start.i_pos.x, y].toVanishing();
                ret = true;
            }
        } while (false);
        return (ret);
    }

    // 불붙는 중인 블록이 하나라도 있으면 true를 반환한다.
    private bool is_has_vanishing_block()
    {
        bool ret = false;
        foreach(BlockControl block in this.blocks) {
            if(block.vanish_timer > 0.0f) {
                ret = true;
                break;
            }
        }
        return(ret);
    }
    // 슬라이드 중인 블록이 하나라도 있으면 true를 반환한다.
    private bool is_has_sliding_block()
    {
        bool ret = false;
        foreach (BlockControl block in this.blocks)
        {
            if(block.step == Block.STEP.SLIDE) {
                ret = true;
                break;
            }
        }

       
        return ret;
    }
    // 낙하 중인 블록이 하나라도 있으면 true를 반환한다.
    private bool is_has_falling_block()
    {
        bool ret = false;

 
        foreach (BlockControl block in this.blocks)
        {
            if(block.step == Block.STEP.FALL) {
                ret = true;
                break;
            }
        }



        return ret;
    } 
    

    public void create()
    { // 레벨 데이터의 초기화, 로드, 패턴 설정까지 시행
        this.level_control = new LevelControl();
        this.level_control.initialize(); // 레벨 데이터 초기화.
        this.level_control.loadLevelData(this.levelData); // 데이터 읽기.
        this.level_control.selectLevel(); // 레벨 선택.
    }

    public Block.COLOR selectBlockColor()
    { // 현재 패턴의 출현 확률을 바탕으로 색을 산출해서 반환
        Block.COLOR color = Block.COLOR.FIRST;
        // 이번 레벨의 레벨 데이터를 가져온다.
        LevelData level_data = this.level_control.getCurrentLevelData();
        float rand = Random.Range(0.0f, 1.0f); // 0.0~1.0 사이의 난수.
        float sum = 0.0f; // 출현 확률의 합계.
        int i = 0;
        // 블록의 종류 전체를 처리하는 루프.
        for (i = 0; i < level_data.probability.Length - 1; i++)
        {
            if (level_data.probability[i] == 0.0f)
            {
                continue; // 출현 확률이 0이면 루프의 처음으로 점프.
            }
            sum += level_data.probability[i]; // 출현 확률을 더한다.
            if (rand < sum)
            { // 합계가 난숫값을 웃돌면.
                break; // 루프를 빠져나온다.
            }
        }
        color = (Block.COLOR)i; // i번째 색을 반환한다.
        return (color);
    }
}
