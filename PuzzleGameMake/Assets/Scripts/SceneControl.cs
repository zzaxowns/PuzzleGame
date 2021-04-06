﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneControl : MonoBehaviour
{

    private BlockRoot block_root = null;

    private ScoreCounter score_counter = null;
    public enum STEP
    {
        NONE = -1, PLAY = 0, CLEAR, NUM,
    }; // 상태 정보 없음, 플레이 중, 클리어, 상태의 종류(= 2).
    public STEP step = STEP.NONE; // 현재 상태.
    public STEP next_step = STEP.NONE; // 다음 상태.
    public float step_timer = 0.0f; // 경과 시간.
    private float clear_time = 0.0f; // 클리어 시간.

    public GUIStyle guistyle; // 폰트 스타일.

    // Start is called before the first frame update
    void Start()
    {
        // BlockRoot 스크립트를 가져온다.
        this.block_root = this.gameObject.GetComponent<BlockRoot>();

        this.block_root.create();

        // BlockRoot 스크립트의 initialSetUp()을 호출한다.
        this.block_root.initialSetUp();

        this.score_counter = this.gameObject.GetComponent<ScoreCounter>(); // ScoreCounter 가져오기
        this.next_step = STEP.PLAY; // 다음 상태를 '플레이 중'으로.
        this.guistyle.fontSize = 24; // 폰트 크기를 24로.
    }

    // Update is called once per frame
    void Update()
    {
        this.step_timer += Time.deltaTime;
        if (this.next_step == STEP.NONE)
        {// 상태 변화 대기 -----.
            switch (this.step)
            {
                case STEP.PLAY:
                    if (this.score_counter.isGameClear()) { this.next_step = STEP.CLEAR; } // 클리어 조건을 만족하면, 클리어 상태로 이행.
                    break;
            }
        }
        while (this.next_step != STEP.NONE)
        { // 상태가 변화했다면 ------.
            this.step = this.next_step;
            this.next_step = STEP.NONE;
            switch (this.step)
            {
                case STEP.CLEAR:
                    this.block_root.enabled = false; // block_root를 정지.
                    this.clear_time = this.step_timer; // 경과 시간을 클리어 시간으로 설정.
                    break;
            }
            this.step_timer = 0.0f;
        }
    }

    void OnGUI()
    {
        switch (this.step)
        {
            case STEP.PLAY:
                GUI.color = Color.black;
                // 경과 시간을 표시.
                GUI.Label(new Rect(40.0f, 10.0f, 200.0f, 20.0f), "시간" + Mathf.CeilToInt(this.step_timer).ToString() + "초", guistyle);
                GUI.color = Color.white;
                break;
            case STEP.CLEAR:
                GUI.color = Color.black;
                // 「☆클리어-！☆」라는 문자열을 표시.
                GUI.Label(new Rect(Screen.width / 2.0f - 80.0f, 20.0f, 200.0f, 20.0f), "☆클리어-!☆", guistyle);
                // 클리어 시간을 표시.
                GUI.Label(new Rect(Screen.width / 2.0f - 80.0f, 40.0f, 200.0f, 20.0f), "클리어 시간" + Mathf.CeilToInt(this.clear_time).ToString()
                + "초", guistyle);
                GUI.color = Color.white;
                break;
        }
    }
}
