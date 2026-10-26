using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인트로 대본. 줄마다 무엇을, 어떤 모양으로, 얼마나 쉬었다가 다음 줄로 갈지 적는다.
/// <c>Create → CoD → Intro Script</c>
///
/// <b>연출은 여기서 고친다.</b> 문구·간격·글리치 위치를 바꾸려고 코드를 열 일이 없게 한다.
/// 새로 만들면 기본 대본이 채워져 있다.
/// </summary>
[CreateAssetMenu(fileName = "IntroScript", menuName = "CoD/Intro Script")]
public class IntroScript : ScriptableObject
{
    public enum LineKind
    {
        /// <summary>프롬프트 뒤에 한 글자씩 타이핑된다. 사람이 친 명령.</summary>
        Command,

        /// <summary>한 번에 뜬다. 프로그램의 출력.</summary>
        Output,

        /// <summary>노란 출력.</summary>
        Warning,

        /// <summary>빨간 출력.</summary>
        Error,

        /// <summary>진행 막대가 0% 에서 progressTo 까지 찬다. 걸리는 시간은 holdSeconds.</summary>
        Progress,

        /// <summary>화면을 비우고 holdSeconds 동안 아무것도 없다. 암전.</summary>
        Blackout
    }

    [System.Serializable]
    public class Line
    {
        public LineKind kind = LineKind.Output;

        [Tooltip("Command 는 프롬프트 뒤에 붙는다. Progress 는 막대 앞에 붙는 이름.")]
        public string text = "";

        [Tooltip("Command 의 앞에 붙는 프롬프트. 비우면 대본의 기본 프롬프트를 쓴다.")]
        public string prompt = "";

        [Tooltip("이 줄이 끝난 뒤 다음 줄까지 쉬는 시간(초).\n" +
                 "Progress 는 막대가 차는 시간, Blackout 은 어둠이 이어지는 시간.")]
        [Min(0f)] public float holdSeconds = 0.3f;

        [Tooltip("이 줄이 뜰 때 화면을 흔들고 글리치 소리를 낸다.")]
        public bool glitch;

        [Tooltip("Progress 막대가 어디까지 찰지(0~100).")]
        [Range(0, 100)] public int progressTo = 100;

        public Line() { }

        public Line(LineKind kind, string text, float hold, bool glitch = false,
                    string prompt = "", int progressTo = 100)
        {
            this.kind = kind;
            this.text = text;
            this.holdSeconds = hold;
            this.glitch = glitch;
            this.prompt = prompt;
            this.progressTo = progressTo;
        }
    }

    [Header("타이핑")]
    [Tooltip("Command 줄의 초당 글자 수.")]
    [Min(1f)] public float charsPerSecond = 28f;

    [Tooltip("Command 줄의 기본 프롬프트.")]
    public string defaultPrompt = "~/my-game $ ";

    [Header("대본")]
    public List<Line> lines = new()
    {
        // 1막 — 금요일 저녁. 평온하다
        new(LineKind.Command, "git log --oneline -1", 0.2f),
        new(LineKind.Output,  "a3f9e21 release candidate. 드디어 출시", 0.6f),
        new(LineKind.Command, "git fetch origin pull/404/head", 0.2f),
        new(LineKind.Output,  "> PR #404  perf-boost   by n0t_a_b0t", 0.3f),
        new(LineKind.Warning, "> WARNING: this PR modifies build scripts", 0.9f),
        new(LineKind.Command, "git merge perf-boost      # 금 18:02, 퇴근 전에 하나만", 0.2f),
        new(LineKind.Output,  "Merge made by the 'ort' strategy.", 0.8f),

        // 2막 — 빌드. 무너진다
        new(LineKind.Command, "./build.sh --release", 0.1f),
        new(LineKind.Progress, "compiling", 1.4f, progressTo: 61),
        new(LineKind.Warning, "> unknown process 'lock' started", 0.5f, glitch: true),
        new(LineKind.Error,   "> encrypting assets ... 2,138 files", 0.5f, glitch: true),
        new(LineKind.Error,   "> YOUR BUILD HAS BEEN LOCKED", 0.7f, glitch: true),
        new(LineKind.Error,   "FATAL: CPU temperature 142°C", 0.3f, glitch: true),
        new(LineKind.Error,   "FATAL: hardware failure", 0.4f, glitch: true),
        new(LineKind.Blackout, "", 2.0f),

        // 3막 — 백업 터미널. 다시 시작한다
        new(LineKind.Output,  "usb0: backup terminal detected", 0.8f),
        new(LineKind.Command, "", 0.6f, prompt: "bash-5.2$ "),
        new(LineKind.Warning, "> 41 new issues:  \"게임이 안 켜져요 (libforge 업데이트 후)\"", 1.2f),
        new(LineKind.Command, "attach --debugger ./infected_build", 0.3f, prompt: "bash-5.2$ "),
        new(LineKind.Output,  "> attaching debugger : PLAYER_01", 1.2f)
    };
}
