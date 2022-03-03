using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class BulletScript : MonoBehaviour
{
    public PhotonView PV;
    int dir;
    string name_; //오브젝트풀써야되서만듬

    [SerializeField]
    float bullet_speed = 12f;


    public void naming(string name)
    {
        name_ = name;
    }

    private void OnEnable()
    {
        name_ = "";
        Invoke("inactive", 2f); //시작때는 그냥 각자 자기가 처리해주면됨(rpc안하고) //밑에상호작용때는 rpc로 삭제필요
     
    }


    void inactive() => ObjectPool.ReturnObject(gameObject);


    void Update() => transform.Translate(Vector3.right * bullet_speed * Time.deltaTime);


    [PunRPC]
    public void ReturnObjectRPC() 
    { 
        ObjectPool.ReturnObject(gameObject); 
    }




    void OnTriggerEnter2D(Collider2D col) // col을 RPC의 매개변수로 넘겨줄 수 없다
    {
        if (col.tag == "Ground") PV.RPC("ReturnObjectRPC", RpcTarget.AllBuffered);

        //총알이 초기화상태아니고 내꼐 아니고 충돌대상은 player고 걔가 나면
        //맞는쪽입장인듯 (느린쪽이란게 이거 동기화되야하니까 억울함방지)
        if (name_!="" &&name_ !=PhotonNetwork.NickName && col.tag == "Player" && col.GetComponent<PhotonView>().IsMine) // 느린쪽에 맞춰서 Hit판정
        {

            col.GetComponent<playerScript>().Hit(name_);
            PV.RPC("ReturnObjectRPC", RpcTarget.AllBuffered);
        }
    }


    //[PunRPC]
    //void DirRPC(int dir) => this.dir = dir;

    // [PunRPC]
    //void DestroyRPC() => Destroy(gameObject);


    //  [PunRPC]
    //void DestroyRPC() => DestroyBullet();
}
