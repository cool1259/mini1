using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Cinemachine;
public class playerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject Gun;
    public Rigidbody2D rb;
    public Animator ani;

    public SpriteRenderer sr;
    public PhotonView PV;
    public Text NickNameText;
    public Image HealthImage;
    public Image DashCoolTimeImage;
    public Button Attack_Btn;
    public Button Dash_Btn;
    public Text Dash_Text;
    bool isGround;
    Vector3 curPos;

    const float origin_speed = 270f;
    public float move_speed = origin_speed;
    public float aim_speed = 100f;
    public float dash_speed = 12f;
    float aim_max_dis = 2.0f;
    bool isDashing = false;
    public bool isdeath = false;
    public int dashh_count = 2;
    GameObject aim;


    private const float origin_attack_cooltiime = 0.25f;
    public float attack_coolTime = 0f;


    public const float origin_hitCoolTime = 0.35f;
    public float hitCoolTime = 0f;

    // 오디오 소스 생성해서 추가
    public AudioSource bgm;
    public AudioSource DashSound;
    public AudioSource ShotSound;

    public SpriteRenderer gun_sr;
    public Animator gun_ani;

    public GameObject aim_joystick;
    // Start is called before the first frame update
    void Awake()
    {


        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName.ToString() : PV.Owner.NickName.ToString();
        NickNameText.color = PV.IsMine ? Color.green : Color.red;


        ShotSound = GameObject.Find("SoundManager").transform.Find("shotSound").GetComponent<AudioSource>();
        DashSound = GameObject.Find("SoundManager").transform.Find("dashSound").GetComponent<AudioSource>();



        if (PV.IsMine)
        {
            // 2D 카메라
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;

            GameManager.Instance.myplayer = gameObject;


            GameObject.Find("Canvas").transform.Find("Move_Joystick").gameObject.SetActive(true);
            GameObject.Find("Canvas").transform.Find("Move_Joystick").gameObject.GetComponent<MoveJoyStick>().MyPlayer = gameObject;


            aim_joystick = GameObject.Find("Canvas").transform.Find("Aim_Joystick").gameObject;
            GameObject.Find("Canvas").transform.Find("Aim_Joystick").gameObject.GetComponent<AimJoystickScript>().MyPlayer = gameObject;


            Attack_Btn = GameObject.Find("Canvas").transform.Find("Aim_Joystick").transform.Find("attack_BTN").gameObject.GetComponent<Button>();
            Attack_Btn.onClick.AddListener(Attack);


            GameObject.Find("Canvas").transform.Find("DashButton").gameObject.SetActive(true);
            Dash_Btn = GameObject.Find("Canvas").transform.Find("DashButton").gameObject.GetComponent<Button>();
            Dash_Btn.onClick.AddListener(Dash);

            DashCoolTimeImage = GameObject.Find("Canvas").transform.Find("DashCoolTime").gameObject.GetComponent<Image>();
            Dash_Text = GameObject.Find("Canvas").transform.Find("DashButton").transform.Find("Text").GetComponent<Text>();

            aim = this.transform.Find("aim").gameObject;
            aim.SetActive(true);

            bgm = GameObject.Find("SoundManager").transform.Find("bgm").GetComponent<AudioSource>();
            bgm.Play();


            GameObject.Find("ObjectPoolParent").transform.GetChild(0).gameObject.SetActive(true);

            // gun_sr = Gun.GetComponent<SpriteRenderer>();
            // gun_ani = Gun.GetComponent<Animator>();
        }
    }

    void Start()
    {

    }
    float h;
    float v;
    // Update is called once per frame
    void Update()
    {
        if (PV.IsMine)
        {
            if (isdeath)
                return;

            // h = Input.GetAxisRaw("Horizontal");
            // v = Input.GetAxis("Vertical");

            //rb.velocity = new Vector2(move_speed* h, move_speed * v );



            //if (h != 0)
            //    PV.RPC("FlipXRPC", RpcTarget.AllBuffered, h); // 재접속시 filpX를 동기화해주기 위해서 AllBuffered


            HitCoolTimeFunc();
            attackCoolTimeFunc();
            animation_running();
            //aimMove();
            gun_lookat();
            DashControl();

            // 스페이스 총알 발사
            if (Input.GetKeyDown(KeyCode.Space)) Attack();


            if (Input.GetKeyDown(KeyCode.R)) Dash();

            if (Input.GetKeyDown(KeyCode.T)) Debug.Log(PhotonNetwork.MasterClient.NickName);
            if (Input.GetKeyDown(KeyCode.Y)) GameManager.Instance.OnEndGame();

            //중간에들어오는애때문에
            if (GameManager.Instance.gameState == GameManager.Instance.state_gaming)
                aim_joystick.SetActive(true);
            else
                aim_joystick.SetActive(false);
        }

        // IsMine이 아닌 것들은 부드럽게 위치 동기화
        else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos; //위치가 동기화 위치랑 너무 멀어지면 동기화 위치로 만듬
        else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10); //그게 아니면 위치를 동기화 받은위치로 보간시킴
    }

    //[PunRPC]
    //void FlipXRPC(float axis) => sr.flipX = axis < 0; //스프라이트 랜더러의 flipX에 axis가 -1이면 true 아니면 false넣어줌

    [PunRPC]
    void colorRPC(float color) => GetComponent<SpriteRenderer>().color = color == 1f ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.1f, 0.1f, 0.5f);

    [PunRPC]
    void SoundRPC(float sound)
    {
        if (ShotSound == null || DashSound == null) return;
        if (sound == 1f)
            ShotSound.Play();
        else
            DashSound.Play();
    }



    public void HitCoolTimeFunc()
    {
        if (hitCoolTime > 0f)
        {
            hitCoolTime -= Time.deltaTime;

            if (hitCoolTime <= 0)
            {
                hitCoolTime = 0;
                PV.RPC("colorRPC", RpcTarget.AllBuffered, 1f);
            }
        }
    }

    public void Hit(string enemyName)
    {
        if (isDashing)
            return;

        PV.RPC("colorRPC", RpcTarget.AllBuffered, 0f);
        hitCoolTime = origin_hitCoolTime;


        HealthImage.fillAmount -= 0.1f;
        if (HealthImage.fillAmount <= 0 && !isdeath)
        {
            isdeath = true;
            rb.velocity = Vector2.zero;
            GameManager.Instance.killReport(enemyName, PhotonNetwork.NickName);
            ani.SetTrigger("death");

        }
    }


    //death애니메이션끝에이벤트달아놈
    public void death()
    {
        if (PV.IsMine)
        {
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered); // AllBuffered로 해야 제대로 사라져 복제버그가 안 생긴다
        }
    }
    [PunRPC]
    void DestroyRPC() => Destroy(gameObject);




    public void Attack()
    {
        if (isdeath) return;


        if (attack_coolTime > 0f)
            return;

        if (!ShotSound.isPlaying)
            PV.RPC("SoundRPC", RpcTarget.AllBuffered, 1f);

        attack_coolTime = origin_attack_cooltiime;


        PV.RPC("ShootRPC", RpcTarget.AllBuffered, PhotonNetwork.NickName);


        //진짜 PhotonNetwork.Instantiate("Bullet", Gun.transform.position + new Vector3(sr.flipX ? -0.4f : 0.4f, -0.11f, 0), Gun.GetComponent<Transform>().rotation);
        //가짜 // .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, sr.flipX ? -1 : 1);

        gun_ani.SetBool("isShot", true);
        ani.SetTrigger("shot");
    }

    [PunRPC]
    void ShootRPC(string name)
    {
        var bullet = ObjectPool.GetObject(name);

        bullet.transform.position = Gun.transform.position + new Vector3(sr.flipX ? -0.4f : 0.4f, -0.11f, 0);
        bullet.transform.rotation = Gun.GetComponent<Transform>().rotation; 
    }

    private void attackCoolTimeFunc()
    {
        if (attack_coolTime > 0)
        {
            attack_coolTime -= Time.deltaTime;

            if (attack_coolTime <= 0)
            {
                attack_coolTime = 0;
                gun_ani.SetBool("isShot", false);
            }
        }
       
    }



    private void gun_lookat()
    {
        Vector2 direction = new Vector2(
                Gun.transform.position.x - aim.transform.position.x,
                Gun.transform.position.y - aim.transform.position.y
            );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // if (!sr.flipX)
        Quaternion angleAxis = Quaternion.AngleAxis(angle -180.0f, Vector3.forward);
        //else
        //    angleAxis = Quaternion.AngleAxis(angle, Vector3.forward);
        Quaternion rotation = Quaternion.Slerp(Gun.transform.rotation, angleAxis, 20.0f * Time.deltaTime);
        if(rotation.z>0.7)
            PV.RPC("GunLookAtRPC", RpcTarget.AllBuffered, rotation);

        PV.RPC("FlipRPC", RpcTarget.AllBuffered, direction.x);
        PV.RPC("GunLookAtRPC", RpcTarget.AllBuffered, rotation);
        

       // Debug.Log(rotation);

    }

    [PunRPC]
    void GunLookAtRPC(Quaternion rotation)
    {
        Gun.transform.rotation = rotation;
    }



    [PunRPC]
    void FlipRPC(float x) { gun_sr.flipY = x > 0f; sr.flipX = gun_sr.flipY;}


    [PunRPC]
    void DashRPC()
    {
        move_speed = 800f;
    }

    public void Dash()
    {

        if (isdeath) return;

        if (dashh_count <= 0) return;


        PV.RPC("SoundRPC", RpcTarget.AllBuffered, 0f);
        dashh_count--;
        Dash_Text.text = "대쉬" + dashh_count;
        DashCoolTimeImage.fillAmount = 1.0f;
        isDashing = true;
        ani.SetTrigger("Dash");
        PV.RPC("DashRPC", RpcTarget.All); //All모든사람들한테 
    }
    public void DashInit() //대쉬쿨초기화
    {
        dashh_count = 2;
        Dash_Text.text = "대쉬" + dashh_count;
        DashCoolTimeImage.fillAmount =0f;
        move_speed = origin_speed;
        isDashing = false; 
    }

    private void DashControl()
    {
        if (move_speed > origin_speed)
        {
           
            move_speed -= Time.deltaTime * 800;

            if (move_speed <= origin_speed)
                move_speed = origin_speed;
        }
        else
            isDashing = false;

        

        if (DashCoolTimeImage.fillAmount > 0f)
        {
            DashCoolTimeImage.fillAmount -= Time.deltaTime;
            if (DashCoolTimeImage.fillAmount <= 0)
            {
                if (dashh_count == 0)
                {
                    dashh_count = 1;
                    Dash_Text.text = "대쉬" + dashh_count;
                    DashCoolTimeImage.fillAmount = 1.0f;
                }
                else if (dashh_count == 1)
                {
                    dashh_count = 2;
                    Dash_Text.text = "대쉬" + dashh_count;
                    DashCoolTimeImage.fillAmount = 0f;
                }
            }
        }
    }

    private void animation_running()
    {
        if (rb.velocity != Vector2.zero)
        {
            ani.SetBool("walk", true);

        }
        else ani.SetBool("walk", false);
    }


    public void Move(Vector2 inputDirection)
    {
        if (isdeath) return;
        rb.velocity = inputDirection* move_speed * 1/60; 
        // 여기서 만약에 30fps면 델타t = 1/30 addforce
        // 근데 60fps면 델타t = 1/60
        
        // addforce와 비교
        // addforce(5)는 매 프레임마다 실행시 매 프레임마다 5만큼 '힘'을 더해준다고
        // 그러면 30fps보다 60fps에서 더 많은 '힘'을 더해줬기때문에
        // 그땐 델타t를 곱해주면 fps별로 노말라이즈 하는 것임!

        // 하지만! 속도는 매 순간의 값을 지정해주는 것이기 때문에
        // 프레임이 달라서 아무리 값을 지정해주는 횟수가 다르더라도


        //if (rb.velocity.x != 0)
         //   PV.RPC("FlipXRPC", RpcTarget.AllBuffered, rb.velocity.x); // 재접속시 filpX를 동기화해주기 위해서 AllBuffered
    }


    //에임--------------------------------------------

    float disX;
    float disY;
    public int range_=3;
    public void aimMove(Vector2 inputDirection)
    {
        //private void controlJoyStickLever(PointerEventData eventData)
        //{
        //    var inputPos = eventData.position - rectTransform.anchoredPosition;
        //    var inputVector = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;
        //    lever.anchoredPosition = inputVector;
        //    inputDirection = inputVector / leverRange;
        //}


        //var TargetPos = aim.transform.position + new Vector3(Time.deltaTime * aim_speed * inputDirection.x, Time.deltaTime * aim_speed * inputDirection.y, 0);

        //var distance = TargetPos - transform.position; //목표지점- 캐릭위치
        //var destVector = distance.magnitude < range_ ? distance : distance.normalized * range_; //그 위치가 range벗어나면 정규화
        //aim.transform.position = destVector;
         
        aim.transform.position = Vector3.Lerp(aim.transform.position, transform.position+ new Vector3(inputDirection.x *4f, inputDirection.y*4f, 0f), Time.deltaTime*aim_speed);
        //disX = transform.position.x - aim.transform.position.x;
        //disY = transform.position.y - aim.transform.position.y;

        //Debug.Log(inputDirection);
        //if (inputDirection.x<-0.5f)
        //{

        //    aim_Left();
        //}
        //if (inputDirection.x>0.5f)
        //{
        //    aim_Right();
        //}
        //if (inputDirection.y>0.5f)
        //{
        //    aim_Up();
        //}
        //if (inputDirection.y<-0.5f)
        //{
        //    aim_Down();
        //}
    }

    public void aim_Right()
    {
        if (disX > -aim_max_dis)
            aim.transform.Translate(Vector3.right * aim_speed * Time.deltaTime);
        else
            aim.transform.position = new Vector2(transform.position.x + aim_max_dis, aim.transform.position.y);
    }
    public void aim_Left()
    {
        if (disX < aim_max_dis)
            aim.transform.Translate(Vector3.left * aim_speed * Time.deltaTime);
        else
            aim.transform.position = new Vector2(transform.position.x - aim_max_dis, aim.transform.position.y);
    }
    public void aim_Up()
    {
        if (disY > -aim_max_dis)
            aim.transform.Translate(Vector3.up * aim_speed * Time.deltaTime);
        else
            aim.transform.position = new Vector2(aim.transform.position.x, transform.position.y + aim_max_dis);
    }
    public void aim_Down()
    {
        if (disY < aim_max_dis)
            aim.transform.Translate(Vector3.down * aim_speed * Time.deltaTime);
        else
            aim.transform.position = new Vector2(aim.transform.position.x, transform.position.y - aim_max_dis);
    }



    //변수 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //ismine일땐 보냄 위치,체력
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(HealthImage.fillAmount);
        }
        //ismine아니면 받음 위치,체력
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }

}
