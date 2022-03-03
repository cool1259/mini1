using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
public class GameManager : MonoBehaviour
{
    public PhotonView PV;
    private static GameManager instance = null;
    Text killLog_;
    Text RangkingLog_;
    Queue<KeyValuePair<string, string>> killQueue = new Queue<KeyValuePair<string, string>>();
    Dictionary<string, int> rankDic = new Dictionary<string, int>();
    //SortedDictionary<int, string> sortedRank = new SortedDictionary<int, string>();

    public readonly  string state_lobby = "lobby";
    public readonly string state_ready = "ready";
    public readonly string state_gaming = "gaming";
    public string gameState { get; private set; }

    public GameObject ResponePanel;
    public Button reGameBtn;
    //방장만함 그후 동기화
    //왜 rankDic도 동기화해줘야되냐면 방장떠나면 다른사람이 방장되서 rankDic써야하니까
    //그리고 custom property는 object 형식이라 박싱언박싱일어나서 안좋은듯
    public GameObject myplayer;

    public void OnReady()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PV.RPC("OnReadyRPC", RpcTarget.AllBuffered);

        }
    }

    [PunRPC]
    public void OnReadyRPC()
    {
        gameState = state_ready;

        //아직도 결과창보고있는애들 강제실행
        if (reGameBtn.IsActive())
            reGameBtn.onClick.Invoke();

        myplayer.GetComponent<playerScript>().isdeath = true; //못움직이게
        myplayer.GetComponent<Transform>().position = new Vector3(Random.Range(-10f, 10f), Random.Range(-5f, 5f), 0);
        myplayer.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        myplayer.GetComponent<Animator>().SetBool("walk", false);
        myplayer.GetComponent<playerScript>().DashInit();
        // myplayer.GetComponent<playerScript>().aim_joystick.SetActive(true);

        //아 게임시작 버튼도 비활성화


        StartCoroutine(ready_Coroutine());
        
    }

    IEnumerator ready_Coroutine()
    {
        killLog_.fontSize = 200;

        killLog_.text = "3";
        yield return new WaitForSeconds(1f);
        killLog_.text = "2";
        yield return new WaitForSeconds(1f);
        killLog_.text = "1";
        yield return new WaitForSeconds(1f);
        killLog_.text = "게임 시작!";

        myplayer.GetComponent<playerScript>().isdeath = false; //이제 움직임
        gameState = state_gaming;
        yield return new WaitForSeconds(1f);
        killLog_.text = "";
        killLog_.fontSize = 100;
    }



    public void OnEndGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            

            PV.RPC("OnEndGameRPC", RpcTarget.AllBuffered);
            
            //마스터는 마지막으로 rankdic 초기화
            for(int i=0;i<rankDic.Count;i++)
            {
                rankDic[rankDic.Keys.ToList()[i]] = 0;
            }
            UpdateRank();
        }
    }
    [PunRPC]
    public void OnEndGameRPC()
    {
        ResponePanel.SetActive(false);
 

        gameState = state_lobby;

        myplayer.GetComponent<playerScript>().aim_joystick.SetActive(false);
       myplayer.GetComponent<playerScript>().PV.RPC("DestroyRPC", RpcTarget.AllBuffered);

        result.SetActive(true);
        result_text.text = "경기 결과\n" + RangkingLog_.text;

        
      

        //버튼누르면다시캐릭터스폰되게함
    }




    public void UserEntrance(string nickName)
    {
    

        if (PhotonNetwork.IsMasterClient)
        {
           if(!rankDic.ContainsKey(nickName)) 
                rankDic.Add(nickName, 0);
            UpdateRank();
        }
    }


    //아 방장 떠날떄만 해주면 되네
    [PunRPC]
    public void UpdateRankDicRPC(Dictionary<string, int> _rankDic, string gs) { rankDic = _rankDic; gameState = gs; }

    public void recordRankDic() =>PV.RPC("UpdateRankDicRPC", RpcTarget.AllBuffered, rankDic, gameState); //방장떠날때 대비 이것도 동기화해줘야됨

    //만약 게임이 끝나면
    //왼쪽 킬리스트 가운데 기록 보여줌 우승 누구누구하고
    //공격버튼없어짐
    //밑에 다시하기 있음
    //다시하면 다시 로비상태로감
    //게임상태는 로비상태가됨

  



    public void UserLeft(string nickName)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (rankDic.ContainsKey(nickName))
                rankDic.Remove(nickName);
            UpdateRank();
        }
    }

    float origin_cool = 1f;
    float record_coolTime = 1f;

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if(record_coolTime>0f) //쿨타임마다 딕셔너리 동기화 (딕셔너리 복사작업을 프레임마다 하는건 애바인듯)
            {
                record_coolTime -= Time.deltaTime;
                if(record_coolTime < 0f)
                {
                    recordRankDic(); //동기화해주고
                    record_coolTime = origin_cool; //쿨타임초기화  
                }
            }


            if(gameState !=state_gaming && gameState != state_ready && myplayer!=null)
            {
                startBTN.SetActive(true);    
            }
            else
            {
                startBTN.SetActive(false);
            }
        }
    }


    public void UpdateRank()
    {
        if (!PhotonNetwork.IsMasterClient) return;


        var sortedDic = rankDic.OrderByDescending(num => num.Value); //벨류값으로 내림차순
        string rank_str="";
      
        foreach (var r in sortedDic)
        {
      
            rank_str += r.Key + " : " + r.Value + "킬\n";
            
        }
        PV.RPC("updateRankTextRPC", RpcTarget.AllBuffered, rank_str);
       
    }
    [PunRPC]
    public void updateRankTextRPC(string rank_str) => RangkingLog_.text = rank_str;



   

    void Awake()
    {
        if (null == instance)
        {
            //이 클래스 인스턴스가 탄생했을 때 전역변수 instance에 게임매니저 인스턴스가 담겨있지 않다면, 자신을 넣어준다.
            instance = this;

            //씬 전환이 되더라도 파괴되지 않게 한다.
            //gameObject만으로도 이 스크립트가 컴포넌트로서 붙어있는 Hierarchy상의 게임오브젝트라는 뜻이지만, 
            //나는 헷갈림 방지를 위해 this를 붙여주기도 한다.
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            //만약 씬 이동이 되었는데 그 씬에도 Hierarchy에 GameMgr이 존재할 수도 있다.
            //그럴 경우엔 이전 씬에서 사용하던 인스턴스를 계속 사용해주는 경우가 많은 것 같다.
            //그래서 이미 전역변수인 instance에 인스턴스가 존재한다면 자신(새로운 씬의 GameMgr)을 삭제해준다.
            Destroy(this.gameObject);
        }
    }

    //게임 매니저 인스턴스에 접근할 수 있는 프로퍼티. static이므로 다른 클래스에서 맘껏 호출할 수 있다.
    public static GameManager Instance
    {
        get
        {
            if (null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    //private static GameManager instance = null;
    //public static GameManager Instance
    //{
    //    get { return instance; }
    //}
    //private void Awake()
    //{
    //    if (instance == null)
    //        instance = this;
    //    else if (instance != this)
    //        Destroy(gameObject);
    //}
    GameObject startBTN;
    GameObject result;
    Text result_text;
    private void Start()
    {
        killLog_ = GameObject.Find("Canvas").transform.Find("KillLog").gameObject.GetComponent<Text>();
        RangkingLog_ = GameObject.Find("Canvas").transform.Find("RankingLog").gameObject.GetComponent<Text>();
        startBTN = GameObject.Find("Canvas").transform.Find("gameStartBTN").gameObject;
        startBTN.GetComponent<Button>().onClick.AddListener(OnReady);
       
        result = GameObject.Find("Canvas").transform.Find("gameEndPanel").gameObject;
        result_text = GameObject.Find("Canvas").transform.Find("gameEndPanel").transform.Find("result_text").gameObject.GetComponent<Text>();


        if (PhotonNetwork.IsMasterClient)
        {
            gameState = state_lobby;

            
        }
        
    }


   

    [PunRPC]
    void killLogRPC(float num, string a, string b)
    {
       killLog_.text= num==1f ? a + "님이 " + b + "님을 처치했습니다"  :  "";

    }

    [PunRPC]
    void killWrite(string killer)
    {
        if (killer == PhotonNetwork.LocalPlayer.NickName)
        {
            myplayer.GetComponent<playerScript>().HealthImage.fillAmount += 0.3f;
            if (myplayer.GetComponent<playerScript>().HealthImage.fillAmount > 1f)
                myplayer.GetComponent<playerScript>().HealthImage.fillAmount = 1f;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            rankDic[killer]++;
            UpdateRank();

            if (rankDic[killer] >= 5)
                OnEndGame();
        }
    }

    public void killReport(string a, string b)
    {
        PV.RPC("killWrite", RpcTarget.AllBuffered, a); //마스터가 rank업데이트해야함
       
   

        //대기중없으면 바로 있으면 큐에집어넣음(줄섬)
        if (killLog_.text == "") killLogStartFunc(a, b);
        else killQueue.Enqueue(new KeyValuePair<string, string>(a, b));
        


    }

    private void killLogStartFunc(string a, string b)
    {

        PV.RPC("killLogRPC", RpcTarget.AllBuffered,1f, a, b);
        StartCoroutine(hide());
    }
  
    IEnumerator hide()
    {
        yield return new WaitForSeconds(3f);
        PV.RPC("killLogRPC", RpcTarget.AllBuffered, 0f,"","");

        if (killQueue.Count > 0) //끝났는데 대기하는 애 있으면 출력
        {
            KeyValuePair<string, string> pair = killQueue.Dequeue();
            killLogStartFunc(pair.Key, pair.Value);
          
        }
    }
}